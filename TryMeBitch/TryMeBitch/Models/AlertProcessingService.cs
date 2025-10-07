using TryMeBitch.Data;
using TryMeBitch.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Mail;

namespace TryMeBitch.Services
{
    public class AlertProcessingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SmtpEmailSender _email;
        private readonly ILogger<AlertProcessingService> _log;
        private const string AlertEmail = "Alert Email Here";

        public AlertProcessingService(
            IServiceScopeFactory scope,
            SmtpEmailSender email,
            ILogger<AlertProcessingService> log)
        {
            _scopeFactory = scope;
            _email = email;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _log.LogInformation("AlertProcessingService started.");

            while (!ct.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MRTDbContext>();

                var oneHourAgoUtc = DateTime.Now.AddHours(-1);

                // 1) Per-bay stats (last hour)
                var bayStats = await db.DepotEnergySlots
                    .AsNoTracking()
                    .Where(x => x.Timestamp >= oneHourAgoUtc)
                    .GroupBy(x => x.BayId)
                    .Select(g => new
                    {
                        BayId = g.Key,
                        Avg = g.Average(x => x.Watts),
                        Latest = g.OrderByDescending(x => x.Timestamp).First().Watts
                    })
                    .ToListAsync(ct);

                // 2) Per-train load (last hour)
                var loadStats = await db.LoadWeights
                    .AsNoTracking()
                    .Where(x => x.Timestamp >= oneHourAgoUtc)
                    .GroupBy(x => x.TrainId)
                    .Select(g => new
                    {
                        TrainId = g.Key,
                        AvgLoad = g.Average(x => x.Kilograms),
                        Latest = g.OrderByDescending(x => x.Timestamp).First().Kilograms
                    })
                    .ToListAsync(ct);

                // 3) Rules
                var rules = await db.AlertDefinitions.AsNoTracking().ToListAsync(ct);

                foreach (var rule in rules)
                {
                    if (rule.Type == AlertType.BayPower)
                    {
                        foreach (var b in bayStats)
                        {
                            var cutoffValue = b.Avg * (1 + rule.Threshold / 100.0);
                            if (b.Latest >= cutoffValue)
                            {
                                await FireAlertAsync(db, rule, b.BayId, b.Latest - b.Avg);
                            }
                        }
                    }
                    else if (rule.Type == AlertType.WheelMaintenance)
                    {
                        // ☆ NEW: take the LAST 16 rows for the selected train (rule.TargetId)
                        var latestRows = await db.WheelScans
                            .AsNoTracking()
                            .Where(w => w.TrainId == rule.TargetId)
                            .OrderByDescending(w => w.Timestamp)
                            .Take(16)
                            .ToListAsync(ct);

                        if (latestRows.Count == 0)
                            continue;

                        var rowsHtml = string.Concat(latestRows.Select(h =>
                            $"<tr><td>{h.WheelPosition}</td>" +
                            $"<td>{h.Distance:F1}</td>" +
                            $"<td>{h.Timestamp:HH:mm:ss}</td></tr>"));

                        var body = $@"
<h3>{rule.MessageTemplate}</h3>
<p>Below are the last <strong>16</strong> wheel-scan readings for {rule.TargetId}:</p>
<table border='1' cellpadding='4' cellspacing='0'>
  <tr><th>Wheel</th><th>Distance&nbsp;(mm)</th><th>Time&nbsp;(NOW)</th></tr>
  {rowsHtml}
</table>
";

                        // Use a simple metric for ObservedValue (e.g., most recent distance)
                        var observed = latestRows.First().Distance;

                        await FireAlertAsync(db, rule, rule.TargetId, observed, body);
                    }
                    else // CapacityUtilization
                    {
                        const double capacity = 3000.0;
                        foreach (var t in loadStats)
                        {
                            var pct = t.Latest / capacity * 100.0;
                            if (pct >= rule.Threshold)
                            {
                                await FireAlertAsync(db, rule, t.TrainId, pct - rule.Threshold);
                            }
                        }
                    }
                }

                await db.SaveChangesAsync(ct);
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }
        }

        private async Task FireAlertAsync(
            MRTDbContext db,
            AlertDefinition rule,
            string device,
            double overValue,
            string? customBody = null)
        {
            var nowUtc = DateTime.Now;

            // rate-limit (30 minutes)
            var trackedRule = await db.AlertDefinitions.FirstAsync(a => a.Id == rule.Id);
            if (trackedRule.LastFiredAt.HasValue &&
                (nowUtc - trackedRule.LastFiredAt.Value) < TimeSpan.FromMinutes(30))
            {
                return;
            }

            var subject = $"[{trackedRule.Role}] Alert: {trackedRule.Name} @ {DateTime.Now:HH:mm:ss}";
            var body = customBody ?? string.Format(
                trackedRule.MessageTemplate ?? "{0} breached threshold with {1:F0}",
                device, overValue);

            try
            {
                await _email.SendEmailAsync(AlertEmail, subject, body); // should send as HTML
            }
            catch (SmtpException ex)
            {
                // log and continue
                Console.WriteLine(ex);
            }

            // log history + update LastFiredAt
            db.AlertHistories.Add(new AlertHistory
            {
                DefinitionId = trackedRule.Id,
                FiredAt = nowUtc,
                RecipientEmail = AlertEmail,
                ObservedValue = overValue,
                MessageSent = body
            });
            trackedRule.LastFiredAt = nowUtc;

            await db.SaveChangesAsync();
        }
    }
}
