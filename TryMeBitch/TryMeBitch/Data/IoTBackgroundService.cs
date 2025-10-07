using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using TryMeBitch.Models;       
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;



namespace TryMeBitch.Data;

public sealed class IoTBackgroundService : BackgroundService
{
    private readonly AlertEmailConfig _alertConfig;

    private readonly IConfiguration _cfg;
    private readonly IServiceProvider _sp;
    private readonly ILogger<IoTBackgroundService> _log;
    private readonly TelegramAlertConfig _teleConfig;


    public IoTBackgroundService(IConfiguration cfg,
                                IServiceProvider sp,
                                ILogger<IoTBackgroundService> log)
    {
        _cfg = cfg;
        _sp = sp;
        _log = log;

        _alertConfig = _cfg.GetSection("AlertEmail").Get<AlertEmailConfig>();
        _teleConfig = _cfg.GetSection("TelegramBot").Get<TelegramAlertConfig>();

    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        string ehConn = "xxxx";
        string myId = "IT3681-GP02-DEV04";
        string cg = EventHubConsumerClient.DefaultConsumerGroupName;

        await using var consumer = new EventHubConsumerClient(cg, ehConn);

        await foreach (PartitionEvent ev in consumer.ReadEventsAsync(stop))
        {
            if (!ev.Data.SystemProperties.TryGetValue("iothub-connection-device-id", out var id) ||
                !myId.Equals(id?.ToString(), StringComparison.OrdinalIgnoreCase))
                continue;  // skip other teams’ traffic

            var json = Encoding.UTF8.GetString(ev.Data.Body.ToArray());
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MRTDbContext>();

            // --- Detect type by field name and insert to correct table ---

            // Brake Pressure
            if (json.Contains("pressure"))
            {
                var dto = JsonSerializer.Deserialize<BrakePressureDto>(json, options);
                if (dto is null || string.IsNullOrEmpty(dto.Status)) continue;
                bool brakeExists = db.Joel_BrakePressureLogs.Any(log =>
                log.TrainId == dto.TrainId &&
                log.Pressure == dto.Pressure &&
                log.Status == dto.Status &&
                log.Timestamp == dto.Timestamp.DateTime);

                            if (!brakeExists)
                            {
                                db.Joel_BrakePressureLogs.Add(new BrakePressureLog
                                {
                                    TrainId = dto.TrainId,
                                    Pressure = dto.Pressure,
                                    Status = dto.Status,
                                    Timestamp = dto.Timestamp.DateTime
                                });
                                await db.SaveChangesAsync(stop);
                                // Send alert if Fault
                                if (dto.Status.Equals("Fault", StringComparison.OrdinalIgnoreCase))
                                {
                                    SendAlertEmail(
                                        $"[ALERT] Fault Detected: Brake Pressure ({dto.TrainId})",
                                        $"A FAULT was detected on Train {dto.TrainId} at {dto.Timestamp.LocalDateTime}.\n" +
                                        $"Pressure: {dto.Pressure}"
                                    );
                                }
                                if (dto.Status.Equals("Fault", StringComparison.OrdinalIgnoreCase))
                                {
                                    SendAlertEmail(
                                        $"[ALERT] Fault Detected: Brake Pressure ({dto.TrainId})",
                                        $"A FAULT was detected on Train {dto.TrainId} at {dto.Timestamp.LocalDateTime}.\nPressure: {dto.Pressure}"
                                    );
                                    await SendTelegramAlert(
                                        $"🚨 FAULT: Brake Pressure on Train {dto.TrainId}\nTime: {dto.Timestamp.LocalDateTime}\nPressure: {dto.Pressure}", stop);
                                }

                }

                continue;

                
            }

            // Cabin Temp
            if (json.Contains("temperature"))
            {
                var dto = JsonSerializer.Deserialize<CabinTempDto>(json, options);
                if (dto is null || string.IsNullOrEmpty(dto.Status)) continue;

                // Check for duplicate before inserting
                bool exists = db.Joel_CabinTempLogs.Any(log =>
                    log.TrainId == dto.TrainId &&
                    log.Temperature == dto.Temperature &&
                    log.Status == dto.Status &&
                    log.Timestamp == dto.Timestamp.DateTime
                );
                if (exists) continue; // Skip if already exists

                db.Joel_CabinTempLogs.Add(new CabinTempLog
                {
                    TrainId = dto.TrainId,
                    Temperature = dto.Temperature,
                    Status = dto.Status,
                    Timestamp = dto.Timestamp.DateTime
                });
                await db.SaveChangesAsync(stop);


                if (dto.Status.Equals("Fault", StringComparison.OrdinalIgnoreCase))
                {
                    SendAlertEmail(
                        $"[ALERT] Fault Detected: Cabin Temp ({dto.TrainId})",
                        $"A FAULT was detected on Train {dto.TrainId} at {dto.Timestamp.LocalDateTime}.\n" +
                        $"Temperature: {dto.Temperature}°C"
                    );
                }
                if (dto.Status.Equals("Fault", StringComparison.OrdinalIgnoreCase))
                {
                    SendAlertEmail(
                        $"[ALERT] Fault Detected: Cabin Temp ({dto.TrainId})",
                        $"A FAULT was detected on Train {dto.TrainId} at {dto.Timestamp.LocalDateTime}.\nTemperature: {dto.Temperature}°C"
                    );
                    await SendTelegramAlert(
                        $"🚨 FAULT: Cabin Temperature on Train {dto.TrainId}\nTime: {dto.Timestamp.LocalDateTime}\nTemperature: {dto.Temperature}°C", stop);
                }


                continue;
            }


            // RFID Entry/Exit
            // RFID Entry/Exit
            if (json.Contains("entryTime"))
            {
                var dto = JsonSerializer.Deserialize<RFIDEntryDto>(json, options);
                if (dto is null || string.IsNullOrEmpty(dto.EntryStatus)) continue;

                // Check for duplicate before inserting
                bool exists = db.Joel_RFIDEntryLogs.Any(log =>
                    log.TrainId == dto.TrainId &&
                    log.EntryTime == dto.EntryTime.DateTime &&
                    (
                        // Both ExitTime must be null, or both non-null and equal
                        (log.ExitTime == null && dto.ExitTime == null) ||
                        (log.ExitTime != null && dto.ExitTime != null && log.ExitTime == dto.ExitTime.Value.DateTime)
                    ) &&
                    log.EntryStatus == dto.EntryStatus
                );

                if (exists) continue; // Skip if already exists

                db.Joel_RFIDEntryLogs.Add(new RFIDEntryLog
                {
                    TrainId = dto.TrainId,
                    EntryTime = dto.EntryTime.DateTime,
                    ExitTime = dto.ExitTime?.DateTime,
                    EntryStatus = dto.EntryStatus
                });
                await db.SaveChangesAsync(stop);
                continue;
            }


        }

    }

    // --- Replace single TelemetryDto with these: ---
    private sealed record BrakePressureDto(
        string TrainId,
        float Pressure,
        string Status,
        DateTimeOffset Timestamp
    );

    private sealed record CabinTempDto(
        string TrainId,
        float Temperature,
        string Status,
        DateTimeOffset Timestamp
    );

    private sealed record RFIDEntryDto(
        string TrainId,
        DateTimeOffset EntryTime,
        DateTimeOffset? ExitTime,
        string EntryStatus
    );

    private void SendAlertEmail(string subject, string body)
    {
        var message = new MailMessage();
        message.From = new MailAddress(_alertConfig.Username);
        message.To.Add(_alertConfig.To);
        message.Subject = subject;
        message.Body = body;

        using (var smtp = new SmtpClient(_alertConfig.SmtpHost, _alertConfig.SmtpPort))
        {
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(_alertConfig.Username, _alertConfig.Password);
            smtp.Send(message);
        }
    }


    private async Task SendTelegramAlert(string message, CancellationToken stop = default)
    {
        if (_teleConfig == null || string.IsNullOrWhiteSpace(_teleConfig.BotToken) || string.IsNullOrWhiteSpace(_teleConfig.ChatId))
            return;

        try
        {
            var botClient = new TelegramBotClient(_teleConfig.BotToken);

            // Accepts string or long. If your ChatId starts with a "-", keep as string.
            var chatId = new ChatId(_teleConfig.ChatId);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                cancellationToken: stop
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Telegram send failed");
        }
    }



}
