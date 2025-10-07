using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs.Consumer;
using CommandLine;
using TryMeBitch.Models;
using Azure.Messaging.EventHubs.Consumer;
using CommandLine;
using Microsoft.Azure.Amqp.Framing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace TryMeBitch.Data
{
    public class IoTHubReceiverService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScopeFactory _scopeFactory;

        Dictionary<string, Guid> stationmaps = new Dictionary<string, Guid>();
        public IoTHubReceiverService(IServiceProvider serviceProvider, IServiceScopeFactory scope)
        {
            _serviceProvider = serviceProvider;
            _scopeFactory = scope;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MRTDbContext>();

            }
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Await your async method properly
                    await ReadHub(stoppingToken);

                    await Task.Delay(500, stoppingToken); // Adjust polling interval
                }
                catch (Exception ex)
                {
                }
            }
        }
        public class BasePayload 
        {
            [JsonPropertyName("MessageType")]
            public string MessageType { get; set; }
        }
        public async Task ReadHub(CancellationToken cancellationToken)
        {
            List<string> TrainData = await ReadDevice2.ReadFromIoTHub(new string[]
            {
                "-c", "xxx"
            });

            for (int i = 0; i < TrainData.Count; i++)
            {
                string data = TrainData[i];
                if (string.IsNullOrWhiteSpace(data)) continue;
                try
                {
                    // Deserialize the JSON data into a dictionary
                    JsonElement payload;

                    try
                    {
                        payload = JsonSerializer.Deserialize<JsonElement>(data);
                    }
                    catch (JsonException ex)
                    {
                        continue;
                    }
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<MRTDbContext>();
                    var ts = DateTime.Now;
                    string? trainId = null;
                    double lat = 0.0, lon = 0.0;


                    if (payload.TryGetProperty("trainId", out var tIdProp))
                    {
                        trainId = tIdProp.GetString();
                    }

                    if (stationmaps.ContainsKey(trainId))
                    {
                        Guid NextStation = stationmaps[trainId];
                        Station stationid = db.stations.Find(NextStation);

                        stationmaps[trainId] = stationid.NextStation;
                        lat = (double)stationid.Lat;
                        lon = (double)stationid.Lon;
                    }
                    else 
                    {
                       
                        Station station = db.stations.OrderBy(s => Guid.NewGuid()).FirstOrDefault();
                        if (station != null)
                        {
                            stationmaps[trainId] = station.NextStation;
                            lat = (double)station.Lat;
                            lon = (double)station.Lon;
                        }
                    }
                    if (trainId != null)
                    {
                        db.TrainLocations.Add(new TrainLocation
                        {
                            Timestamp = ts,
                            TrainId = trainId,
                            Latitude = lat,
                            Longitude = lon
                        });
                    }


                    // Power Usage
                    if (payload.TryGetProperty("powerUsage", out var pw))
                    {
                        db.PowerUsages.Add(new PowerUsage
                        {
                            Timestamp = ts,
                            Source = "Train",
                            Watts = pw.GetDouble()
                        });
                    }

                    // Load Weight (with trainId)
                    if (trainId != null
                && payload.TryGetProperty("loadWeight", out var lw))
                    {
                        db.LoadWeights.Add(new LoadWeight
                        {
                            Timestamp = ts,
                            TrainId = trainId,
                            Kilograms = lw.GetDouble()
                        });
                    }

                    // Depot Slot Energy (with bayId)
                    if (payload.TryGetProperty("bayId", out var b) &&
                        payload.TryGetProperty("bayPowerDraw", out var bp))
                    {
                        db.DepotEnergySlots.Add(new DepotEnergySlot
                        {
                            Timestamp = ts,
                            BayId = b.GetString()!,
                            Watts = bp.GetDouble()
                        });
                    }

                    await db.SaveChangesAsync(cancellationToken);
                    if (payload.TryGetProperty("trainId", out var tIdProp1))
                    {
                        trainId = tIdProp1.GetString();
                    }
                    if (trainId != null
             && payload.TryGetProperty("wheelPosition", out var wPos)
             && payload.TryGetProperty("distance", out var dist))
                    {
                        // Re-use outer trainId instead of declaring a new one
                        var d = dist.GetDouble();
                        var anomaly = Math.Abs(d - 290.0) > 8.0;



                        db.WheelScans.Add(new WheelScan
                        {
                            Timestamp = DateTime.Now,
                            TrainId = trainId,
                            WheelPosition = wPos.GetInt32(),
                            Distance = d,
                            IsAnomaly = anomaly
                        });
                    }
                    await db.SaveChangesAsync(cancellationToken);
                }


                catch (Exception ex)
                {
                    // Handle or log the exception as needed
                    Console.WriteLine($"Error processing data: {ex.Message}");
                }
            }
        }
    }

        public class ReadDevice2
        {
            private static Parameters _parameters;
            private static void Main(string[] args) { }
            public static async Task<List<string>> ReadFromIoTHub(string[] args)
            {
                ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                   .WithParsed(parsedParams =>
                   {
                       _parameters = parsedParams;
                   })
                   .WithNotParsed(errors =>
                   {
                       Environment.Exit(1);
                   });
                // Either the connection string must be supplied, or the set of endpoint, name, and shared access key must be.
                if (string.IsNullOrWhiteSpace(_parameters.EventHubConnectionString)
                    && (string.IsNullOrWhiteSpace(_parameters.EventHubCompatibleEndpoint)
                        || string.IsNullOrWhiteSpace(_parameters.EventHubName)
                        || string.IsNullOrWhiteSpace(_parameters.SharedAccessKey)))
                {

                    Environment.Exit(1);
                }
                // Set up a way for the user to gracefully shutdown
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    cts.Cancel();
                };
                _ = Task.Run(async () =>
                {
                    var idleTimeout = TimeSpan.FromSeconds(3);
                    var lastActivity = DateTime.UtcNow;

                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000); // check every second

                        if (DateTime.UtcNow - lastActivity > idleTimeout)
                        {
                            cts.Cancel(); 
                        }
                    }
                });
                return await ReceiveMessagesFromDeviceAsync(cts.Token);
            }
            private static async Task<List<string>> ReceiveMessagesFromDeviceAsync(CancellationToken ct)
            {
                string connectionString = _parameters.GetEventHubConnectionString();

                await using var consumer = new EventHubConsumerClient(
                    EventHubConsumerClient.DefaultConsumerGroupName,
                    connectionString,
                    _parameters.EventHubName);

                List<string> payload = new List<string>();
                try
                {
                    await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(ct))
                    {
                        if (!(partitionEvent.Data.SystemProperties["iothub-connection-device-id"].ToString().Contains("GP02") && partitionEvent.Data.SystemProperties["iothub-connection-device-id"].ToString().Contains("DEV02"))) { continue; }
                        ;



                        string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());

                        payload.Add(data);

                        TimeSpan idleTimeout = TimeSpan.FromSeconds(5);
                        DateTime lastEventTime = DateTime.UtcNow;
                      
                }

            }
                catch (TaskCanceledException)
                {
                }
                return payload;
            }

    }
    }



