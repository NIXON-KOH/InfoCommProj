 using static TryMeBitch.Models.Blockchain;
using System.Security.Claims;
using System.Text.Json;
using TryMeBitch.Data;
using TryMeBitch.Models;

public class IoTHubListenerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IoTHubListenerService> _logger;

    public IoTHubListenerService(IServiceProvider serviceProvider, ILogger<IoTHubListenerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IoT Hub Listener started.");

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
                _logger.LogError(ex, "Error in IoT Hub Listener");
            }
        }

        _logger.LogInformation("IoT Hub Listener stopping.");
    }

    HashSet<string> SavedTimeStamps = new HashSet<string>();

    public async Task ReadHub(CancellationToken cancellationToken)
    {
        string[] connstring = new string[]
        {
            "-c", "Endpoint=sb://ihsuprodsgres021dednamespace.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=MRMonIa107HtJM0ccTZl6CfNMzfTH6Y4/AIoTD0fGAk=;EntityPath=iothub-ehub-it3681-00-57167206-863f4b9aec"
        };

        List<string> gantrytoken = await ReadD2cMessages.ReadFromIoTHub(connstring);

        List<string> Payloads = new List<string>();

        foreach (string readtoken in gantrytoken)
        {
            try
            {
                string mySecretKey = "thisisasecretkeythatisatleast16byteslong"; //Should be in ENV
                string myIssuer = "Device01";
                string myAudience = "StationTerminal";
                JwtHelper jwtHelper = new JwtHelper(mySecretKey, myIssuer, myAudience);
                ClaimsPrincipal principalFromJson = jwtHelper.ValidateToken(readtoken);

                var payload = principalFromJson.Claims.FirstOrDefault(c => c.Type.StartsWith("FootTraffic"));
                if (payload != null && !SavedTimeStamps.Contains(payload.Type))
                {
                    SavedTimeStamps.Add(payload.Type);
                    Payloads.Add(payload.Value);
                }
            }
            catch (Exception ex)
            {
               
            }
        }

        Dictionary<Guid, double> tempcard = new Dictionary<Guid, double>();

        foreach (string claim in Payloads)
        {
            List<Dictionary<string, object>> block = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(claim);

            foreach (Dictionary<string, object> detail in block)
            {
                var tempblock = new BlockchainEvent
                {
                    Id = Guid.Parse(((JsonElement)detail["Id"]).GetString()),
                    CardId = Guid.Parse(((JsonElement)detail["CardId"]).GetString()),
                    EventType = ((JsonElement)detail["EventType"]).GetString(),
                    FareCharged = Math.Round(((JsonElement)detail["FareCharged"]).GetDouble(), 2),
                    NewBalance = Math.Round(((JsonElement)detail["NewBalance"]).GetDouble(), 2),
                    Station = detail.TryGetValue("Station", out var stationElement) &&
                              stationElement is JsonElement stationJson &&
                              stationJson.ValueKind != JsonValueKind.Null &&
                              stationJson.ValueKind != JsonValueKind.Undefined
                        ? stationJson.GetString()
                        : "None",
                    Timestamp = ((JsonElement)detail["Timestamp"]).GetDateTime(),
                    Hash = ((JsonElement)detail["Hash"]).GetString(),
                    PrevHash = ((JsonElement)detail["PrevHash"]).GetString()
                };

                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<MRTDbContext>();

                    db.Blockchain.Add(tempblock);

                    await db.SaveChangesAsync(cancellationToken);
                }

                tempcard[tempblock.CardId] = tempblock.NewBalance;
            }
        }
    }
}
