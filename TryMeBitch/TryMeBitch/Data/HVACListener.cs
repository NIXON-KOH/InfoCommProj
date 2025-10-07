using System.Security.Claims;
using System.Text.Json;
using TryMeBitch.Models;
using static TryMeBitch.Models.Blockchain;

namespace TryMeBitch.Data
{
    public class HVACListener : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HVACListener> _logger;
        public HVACListener(IServiceProvider serviceProvider, ILogger<HVACListener> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("HVAC Listener started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Await your async method properly
                    await ReadHub(stoppingToken);
                    await Task.Delay(800, stoppingToken); // Adjust polling interval
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in HVAC Listener");
                }
            }
            _logger.LogInformation("HVAC Listener stopping.");
        }
        HashSet<string> SavedTimeStamps = new HashSet<string>();

        public async Task ReadHub(CancellationToken cancellationToken)
        {
            string[] connstring = new string[]
            {
            "-c", "xxx"
            };

            List<string> gantrytoken = await ReadD2cMessages.ReadFromIoTHub(connstring);
            List<string> Payloads = new List<string>();
            foreach (string readtoken in gantrytoken)
            {
                try
                {
                    string mySecretKey = "SuperDuperSecretKeyThatsAtleast16Bytes"; //Should be in ENV
                    string myIssuer = "Device01";
                    string myAudience = "StationTerminal";
                    JwtHelper jwtHelper = new JwtHelper(mySecretKey, myIssuer, myAudience);
                    ClaimsPrincipal principalFromJson = jwtHelper.ValidateToken(readtoken);

                    var payload = principalFromJson.Claims.FirstOrDefault(c => c.Type.StartsWith("HVAC_"));
                    if (payload != null && !SavedTimeStamps.Contains(payload.Type))
                    {
                        SavedTimeStamps.Add(payload.Type);
                        Payloads.Add(payload.Value);
                    }
                }
                catch
                {
                   
                }
            }
           

            foreach (string claim in Payloads)
            {
                

                var detail = JsonSerializer.Deserialize<Dictionary<string, object>>(claim);

                var tempblock = new HVAC
                    {
                        Id = Guid.Parse(((JsonElement)detail["Id"]).GetString()),
                        timestamp = DateTime.Parse(((JsonElement)detail["timestamp"]).GetString()),
                        temperature = ((JsonElement)detail["temperature"]).GetDouble(),
                        humidity = ((JsonElement)detail["humidity"]).GetDouble(),
                        psi = ((JsonElement)detail["psi"]).GetDouble(),
                        GasDetection = ((JsonElement)detail["GasDetection"]).GetDouble()
                    };

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<MRTDbContext>();

                        db.HVAC.Add(tempblock);

                        await db.SaveChangesAsync(cancellationToken);
                    }

                 
            }
        }
    
    }
}
