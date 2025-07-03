
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Bogus;
using FareSystem;
using Blockchain;
using System.Text.Json;


namespace Read
{
    class Read
    {
        public static List<BlockChain.BlockchainEvent> MerkleTreeLeafs = new List<BlockChain.BlockchainEvent> { };
        public static async Task Man()
        {
            Console.WriteLine("Reading From IoT Hub");
            string[] connstring = new string[]
            {
                    "-c","Endpoint=sb://ihsuprodsgres021dednamespace.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=MRMonIa107HtJM0ccTZl6CfNMzfTH6Y4/AIoTD0fGAk=;EntityPath=iothub-ehub-it3681-00-57167206-863f4b9aec"
            };
            List<string> gantrytoken = await ReadD2cMessages.Program.ReadFromIoTHub(connstring);
            Console.WriteLine("Decoding...");
            List<string> Payloads = new List<string> { };
            //JWT Decode
            foreach (string readtoken in gantrytoken)
            {
                try
                {
                    string mySecretKey = "thisisasecretkeythatisatleast16byteslong"; //Keep this in ENV
                    string myIssuer = "Device01";
                    string myAudience = "StationTerminal";
                    JwtHelper jwtHelper = new JwtHelper(mySecretKey, myIssuer, myAudience);
                    ClaimsPrincipal principalFromJson = jwtHelper.ValidateToken(readtoken);

                    var payload = principalFromJson.Claims.FirstOrDefault(c => c.Type.StartsWith("FootTraffic"));
                    Payloads.Add(payload.Value);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred during validation: {ex.Message}");
                }
            }
            foreach (string claim in Payloads)
            {
                Console.WriteLine(claim);
                List<Dictionary<string, object>> block = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(claim);
                foreach (Dictionary<string, object> detail in block)
                {
                    MerkleTreeLeafs.Add(new BlockChain.BlockchainEvent
                    {
                        Id = Guid.Parse(((JsonElement)detail["Id"]).GetString()),
                        CardId = Guid.Parse(((JsonElement)detail["CardId"]).GetString()),
                        EventType = ((JsonElement)detail["EventType"]).GetString(),
                        FareCharged = ((JsonElement)detail["FareCharged"]).GetDouble(),
                        Station = detail.TryGetValue("Station", out var stationElement) &&
                                          stationElement is JsonElement stationJson &&
                                          stationJson.ValueKind != JsonValueKind.Null &&
                                          stationJson.ValueKind != JsonValueKind.Undefined
                                    ? stationJson.GetString()
                                    : null,
                        Timestamp = ((JsonElement)detail["Timestamp"]).GetDateTime(),
                        Hash = ((JsonElement)detail["Hash"]).GetString(),
                        PrevHash = ((JsonElement)detail["PrevHash"]).GetString()
                    }); 


                }
            }
            BlockChain.BuildMerkleTrees(MerkleTreeLeafs);
            BlockChain.CheckBlockchainIntegrity(MerkleTreeLeafs);


        }
    }
}