using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Data;
using TryMeBitch.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Emgu.CV.Ocl;

namespace TryMeBitch.Controllers
{
    [Authorize(Roles = "Engineer,Administrator")]

    public class TelemetryController : Controller
    {
        private readonly MRTDbContext _db;
        private readonly EnergyUsageService _energySvc;
        private readonly LoadMonitoringService _loadSvc;

        public TelemetryController(
            MRTDbContext db,
            EnergyUsageService energySvc,
            LoadMonitoringService loadSvc)
        {
            _db = db;
            _energySvc = energySvc;
            _loadSvc = loadSvc;

        }

        // GET: /Telemetry/Index
        public async Task<IActionResult> Index()
        {
            var gps = await _db.TrainLocations
                             .OrderByDescending(t => t.Timestamp)
                             .Take(50)
                             .ToListAsync();
            var power = await _db.PowerUsages
                             .OrderByDescending(p => p.Timestamp)
                             .Take(50)
                             .ToListAsync();
            var load = await _db.LoadWeights
                             .OrderByDescending(l => l.Timestamp)
                             .Take(50)
                             .ToListAsync();
            var slot = await _db.DepotEnergySlots
                             .OrderByDescending(s => s.Timestamp)
                             .Take(50)
                             .ToListAsync();

            var vm = new TelemetryViewModel
            {
                TrainLocations = gps,
                PowerUsages = power,
                LoadWeights = load,
                DepotEnergySlots = slot
            };

            return View(vm);
        }

        // GET: /Telemetry/DepotEnergyMonitoring
        [HttpGet]
        public async Task<IActionResult> DepotEnergyMonitoring(string bayId = "Bay-01")
        {
            var cutoff = DateTime.Now - TimeSpan.FromHours(1);
            var allReadings = await _db.DepotEnergySlots
       .Where(x => x.Timestamp >= cutoff)
       .ToListAsync();

            // 1. pull raw readings
            var readings = await _db.DepotEnergySlots
        .Where(x => x.Timestamp >= cutoff && x.BayId == bayId)
        .ToListAsync();
            // … same as before …
            ViewData["SelectedBay"] = bayId;

            // 2. compute per-bay stats
            var stats = readings
                .GroupBy(x => x.BayId)
                .Select(g => new BayEnergyStat
                {
                    BayId = g.Key,
                    AvgWatts = g.Average(x => x.Watts),
                    MinWatts = g.Min(x => x.Watts),
                    MaxWatts = g.Max(x => x.Watts)
                })
                .ToList();

            // 3. group into minute‐buckets & average
            var grouped = readings
                .GroupBy(r => new DateTime(r.Timestamp.Year,
                                           r.Timestamp.Month,
                                           r.Timestamp.Day,
                                           r.Timestamp.Hour,
                                           r.Timestamp.Minute,
                                           0))
                .OrderBy(g => g.Key)
                .Select(g => new TimeSeriesPoint
                {
                    Time = g.Key,
                    Value = g.Average(r => r.Watts)
                })
                .ToList();

            // 4. dynamic threshold: e.g. 120% of overall avg
            var overallAvg = stats.Any() ? stats.Average(s => s.AvgWatts) : 0;
            var threshold = overallAvg * 1.2;

            // rawAlerts = all readings >= threshold, ordered by timestamp desc
            var rawAlerts = readings
                .Where(r => r.Watts >= threshold)
                .OrderByDescending(r => r.Timestamp)
                .ToList();

            // now group by BayId + minute, pick the max‐Watts entry in each group,
            // then sort by BayId and Timestamp asc
            var alerts = (await _energySvc.DetectHighUsageAsync(threshold))
              .Where(a => a.BayId == bayId)              // only the selected bay
              .OrderByDescending(a => a.Timestamp)       // newest first
              .Take(10)                                  // final cap
              .ToList();
            var lastPerBay = allReadings
      .GroupBy(r => r.BayId)
      .Select(g => g.OrderByDescending(r => r.Timestamp).First().Watts);
            double currentUsageOverall = lastPerBay.Any()
                ? lastPerBay.Average()
                : 0;

            // 8) peak overall = max single reading across all bays
            double peakOverall = allReadings.Any()
                ? allReadings.Max(r => r.Watts)
                : 0;

            // 9) avg overall = average of all readings across all bays
            double avgOverall = allReadings.Any()
                ? allReadings.Average(r => r.Watts)
                : 0;
            var vm = new EnergyMonitoringViewModel
            {
                Stats = stats,
                GroupedReadings = grouped,
                ThresholdValue = threshold,
                Alerts = alerts,
                CurrentUsageOverall = currentUsageOverall,
                PeakOverall = peakOverall,
                AvgOverall = avgOverall
            };


            return View(vm);
        }

        //public async Task<IActionResult> LoadMonitoring(string trainId = "All")
        //{
        //    var window = TimeSpan.FromHours(1);

        //    // 1) get all carriage stats, then filter if needed
        //    var stats = await _loadSvc.GetCarriageStatsAsync(window);
        //    if (!string.Equals(trainId, "All", StringComparison.OrdinalIgnoreCase))
        //    {
        //        stats = stats.Where(s => s.CarriageId == trainId).ToList();
        //    }

        //    // 2) pull time series already filtered by trainId
        //    var series = await _loadSvc.GetLoadTimeSeriesAsync(window, trainId);

        //    // 3) build the dropdown
        //    var trainList = new[] { "All", "Train1", "Train2", "Train3", "Train4", "Train5" };
        //    ViewData["TrainFilter"] = new SelectList(trainList, trainId);

        //    // 4) assemble VM
        //    var vm = new LoadMonitoringViewModel
        //    {
        //        CarriageStats = stats,
        //        LoadSeries = series
        //    };
        //    // 5) also populate current load
        //    foreach (var s in vm.CarriageStats)
        //    {
        //        s.CurrentLoad = await _loadSvc.GetCurrentLoadAsync(s.CarriageId);
        //    }

        //    return View(vm);
        //}
        public async Task<IActionResult> LoadMonitoring(string trainId = "All")
        {
            var window = TimeSpan.FromHours(1);
            var cutoff = DateTime.Now - window;
            /* 1️⃣  dynamic train list ------------------------------------------ */
            var trainIds = await _db.Joel_Train
                                    .Where(t => t.IsActive)
                                    .Select(t => t.TrainId)
                                    .OrderBy(id => id)
                                    .ToListAsync();

            trainIds.Insert(0, "All");                              // prepend "All"
            ViewData["TrainFilter"] = new SelectList(trainIds, trainId);
            /* ----------------------------------------------------------------- */

            /* 2️⃣  stats: filter only if a specific train was chosen ----------- */
            var stats = await _loadSvc.GetCarriageStatsAsync(window);
            if (!string.Equals(trainId, "All", StringComparison.OrdinalIgnoreCase))
            {
                stats = stats.Where(s => s.CarriageId == trainId).ToList();
            }

            /* 3️⃣  time-series already has trainId param ----------------------- */
            var series = (await _loadSvc.GetLoadTimeSeriesAsync(window, trainId))
                .Where(p => p.Time >= cutoff)      // ≤――― here
                .OrderBy(p => p.Time)              // make sure they’re in order
                .ToList();

            /* 4️⃣  view-model + current load ---------------------------------- */
            var vm = new LoadMonitoringViewModel
            {
                CarriageStats = stats,
                LoadSeries = series
            };
            foreach (var s in vm.CarriageStats)
                s.CurrentLoad = await _loadSvc.GetCurrentLoadAsync(s.CarriageId);

            return View(vm);
        }


    }
}
