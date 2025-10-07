
using System.Security.Claims;
using System.Text.Json;
using TryMeBitch.Data;
using TryMeBitch.Models;
using System.Threading;
using System.Text;
using System.Security.Cryptography;
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
                _logger.LogError(ex, "Error in IoT Hub Listener");
            }
        }

        _logger.LogInformation("IoT Hub Listener stopping.");
    }

    public class Transaction
    {
        public Guid GuidTransaction { get; set; }
        public string Type { get; set; }
        public string Station { get; set; }
        public DateTime EntryTime { get; set; }
        public Guid CardId { get; set; }
        public double Amount;
        public string hash { get; set; }
        public double amount
        {
            get => Amount;
            set => Amount = Math.Round(value, 2);
        }
        public override string ToString()
        {
            return $"Guid:\t\t{GuidTransaction}\nCardId:\t{CardId}\nType:\t\t{Type}\nStation:\t{Station}\nEntryTime:\t{EntryTime}\nAmount:\t\t{Amount}\nhash:\t\t{hash}\n";
        }

    }
   

    HashSet<string> SavedTimeStamps = new HashSet<string>();

    static string ComputeSha256Hash(string rawData)
    {
        // Create a SHA256
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash - returns byte array
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }


    public async Task ReadHub(CancellationToken cancellationToken)
    {
        string[] connstring = new string[]
        {
        "-c", "xxx"
        };

        List<string> gantryTokens = await ReadD2cMessages.ReadFromIoTHub(connstring);
        List<string> payloads = new List<string>();

        foreach (string readToken in gantryTokens)
        {
            try
            {
                string mySecretKey = "thisisasecretkeythatisatleast16byteslong";
                string myIssuer = "Device01";
                string myAudience = "StationTerminal";

                JwtHelper jwtHelper = new JwtHelper(mySecretKey, myIssuer, myAudience);
                ClaimsPrincipal principal = jwtHelper.ValidateToken(readToken);

                var claim = principal.Claims.FirstOrDefault(c => c.Type.StartsWith("FootTraffic"));

                if (claim != null && !SavedTimeStamps.Contains(claim.Type))
                {
                    SavedTimeStamps.Add(claim.Type);
                    payloads.Add(claim.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"JWT validation failed: {ex.Message}");
            }
        }

        // Map to track previous hash per card
        Dictionary<Guid, string> lastCardHash = new Dictionary<Guid, string>();

        foreach (string claimJson in payloads)
        {
            List<Transaction>? blockData = JsonSerializer.Deserialize<List<Transaction>>(claimJson);

            if (blockData == null)
                continue;
            blockData = blockData.OrderBy(tx => tx.EntryTime).ToList();

            foreach (Transaction tx in blockData)
            {
                // Generate hash
                string prevHash = lastCardHash.ContainsKey(tx.CardId) ? lastCardHash[tx.CardId] : "GENESIS";
                Console.WriteLine("Processing transaction: " + tx.ToString());
                //($"guid:{newGuid} Type:{Type} Station:{Station} Timestamp:{Entry} Amount:{amount}"
                string hashedData = ComputeSha256Hash($"guid:{tx.GuidTransaction} Type:{tx.Type} Station:{tx.Station} Timestamp:{tx.EntryTime} Amount:{tx.amount}");
                if (hashedData == tx.hash.ToString()){
                    _logger.LogInformation($"Hash matches for CardId: {tx.CardId}, Transaction: {tx.GuidTransaction}");
                }
                else
                {
                    _logger.LogWarning($"Hash mismatch for CardId: {tx.CardId}, Transaction: {tx.GuidTransaction}. Expected: {hashedData}, Found: {tx.hash}");
                    continue; // Skip if hash does not match
                }
                Guid id = Guid.NewGuid();
                string hash = ComputeSha256Hash($"{id}{tx.CardId}{tx.Type}{tx.Station}{tx.EntryTime}{tx.Amount}");
                var Event = new TryMeBitch.Models.Blockchain.BlockChainEvent
                {
                    Id = id,
                    CardId = tx.CardId,
                    EventType = tx.Type,
                    Station = tx.Station,
                    Timestamp = tx.EntryTime,
                    Amount = tx.Amount,
                    hash = hash,
                    previoushash = prevHash
                };

                lastCardHash[tx.CardId] = hash;

                // Save to DB
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MRTDbContext>();

                db.Blockchain.Add(Event);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

}
