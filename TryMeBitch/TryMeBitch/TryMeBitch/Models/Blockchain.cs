using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Linq;
using TryMeBitch.Data;

namespace TryMeBitch.Models
{
    public class Blockchain
    {
        private readonly MRTDbContext _repo;
        public Blockchain(MRTDbContext repo)
        {
            _repo = repo;
        }
        public static ConcurrentDictionary<Guid, string> CardMerkleRoots = new ConcurrentDictionary<Guid, string>();
            public static ConcurrentDictionary<Guid, List<BlockchainEvent>> CardEventHistory = new ConcurrentDictionary<Guid, List<BlockchainEvent>>();

            public static void BuildMerkleTrees(List<BlockchainEvent> blockchain)
            {
                var cardsGrouped = blockchain.GroupBy(e => e.CardId).ToList();

                foreach (var group in cardsGrouped)
                {
                    string merkleRoot = BuildMerkleRoot(group.ToList());
                    CardMerkleRoots[group.Key] = merkleRoot;
                    CardEventHistory[group.Key] = group.ToList();
                    Console.WriteLine($"Built Merkle Root for Card {group.Key}: {merkleRoot.Substring(0, 8)}...");
                }
            }

            public async Task<string> RebuildMerkleBranch(List<BlockchainEvent> affectedEvents)
            {
                Console.WriteLine("Starting Merkle rebuild...");
                Console.WriteLine($"Number of events to rebuild: {affectedEvents.Count}");
                Guid cardId = new Guid();
                // Step 2: Compute the hashes for the affected events AND UPDATE THE EVENTS
                foreach (var e in affectedEvents)
                {
                    // Recalculate the hash for each event
                    string hash = ComputeHash(e);
                    Console.WriteLine($"Event {e.CardId} - Data: {e.ToString()}");
                    Console.WriteLine($"Event {e.CardId} - Old Hash: {e.Hash}");
                    Console.WriteLine($"Event {e.CardId} - New Hash: {hash}");

                    // Actually update the event's hash value
                    e.Hash = hash;
               
                    _repo.Blockchain.Update(e);
                    _repo.SaveChangesAsync();
                }

                // Get the updated hashes
                var hashes = affectedEvents.Select(e => e.Hash).ToList();

                // Rest of the Merkle tree building logic...
                int level = 0;
                while (hashes.Count > 1)
                {
                    Console.WriteLine($"Level {level} - Hash count: {hashes.Count}");

                    List<string> newHashes = new List<string>();

                    for (int i = 0; i < hashes.Count; i += 2)
                    {
                        if (i + 1 < hashes.Count)
                        {
                            // Combine two hashes and compute the new hash
                            string combinedHash = hashes[i] + hashes[i + 1];
                            string newHash = ComputeHash(combinedHash);
                            Console.WriteLine($"Combining: {hashes[i].Substring(0, 8)} + {hashes[i + 1].Substring(0, 8)} -> {newHash.Substring(0, 8)}");
                            newHashes.Add(newHash);
                        }
                        else
                        {
                            // If odd number of hashes, just carry over the last one
                            Console.WriteLine($"Odd count, carrying over hash: {hashes[i].Substring(0, 8)}");
                            newHashes.Add(hashes[i]);
                        }
                    }
                    // Update hashes for the next level
                    hashes = newHashes;
                    level++;
                }
                // The final hash should be the Merkle root
                Console.WriteLine($"Final Merkle root: {hashes[0].Substring(0, 8)}");

            // Save the updated events to your data store if needed
            // await SaveUpdatedEvents(affectedEvents);
                CardMerkleRoots[cardId] = hashes[0];
                return hashes[0];
            }

            // Simulate tampering of blockchain events
            public void TamperBlockchain(List<BlockchainEvent> blockchain)
            {
                var rand = new Random();
                var index = rand.Next(blockchain.Count);
                var tamperedEvent = blockchain[index];

                if (tamperedEvent != null)
                {
                // Modify the event (simulating tampering)
                tamperedEvent.Timestamp = DateTime.Parse("2025-04-23T08:00:00");
                _repo.Blockchain.Update(tamperedEvent);
                    
                   Console.WriteLine($"[Tamper] Card : {tamperedEvent.CardId} at Event {tamperedEvent.Id}");
                }
            }




            // Build the Merkle root for the full list of events
            static string BuildMerkleRoot(List<BlockchainEvent> events)
            {
                var hashes = events.Select(e => ComputeHash(e)).ToList();

                while (hashes.Count > 1)
                {
                    List<string> newHashes = new List<string>();

                    for (int i = 0; i < hashes.Count; i += 2)
                    {
                        if (i + 1 < hashes.Count)
                        {
                            newHashes.Add(ComputeHash((hashes[i] + hashes[i + 1])));
                        }
                        else
                        {
                            newHashes.Add(hashes[i]);
                        }
                    }

                    hashes = newHashes;
                }

                return hashes[0];
            }

            // Compute the hash for a blockchain event
            public static string ComputeHash(BlockchainEvent e)
            {
                string stationSafe = e.Station ?? "";
                string raw = $"{e.Id}{e.CardId}{e.EventType}{e.FareCharged}{e.Station}{e.Timestamp:O}{e.PrevHash}";

                using var sha = SHA256.Create();
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return Convert.ToHexString(bytes);
            }
            static string ComputeHash(string concatenatedHashes)
            {
                using var sha = SHA256.Create();
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(concatenatedHashes));
                return Convert.ToHexString(bytes);
            }

            // Print an event in the blockchain
        public static async Task<List<IntegrityIssue>> CheckBlockchainIntegrityAndReport(List<BlockchainEvent> blockchain, List<Card> cards)
        {
            var issues = new List<IntegrityIssue>();
            var chains = blockchain.GroupBy(e => e.CardId);
            var tasks = chains.Select(async cardChain =>
            {
                string prevHash = "Genesis";
                var orderedEvents = cardChain.OrderBy(e => e.Timestamp).ToList();

                for (int i = 0; i < orderedEvents.Count; i++)
                {
                    var current = orderedEvents[i];
                    string calculatedHash = ComputeHash(current);

                    if (current.Hash != calculatedHash)
                    {
                        issues.Add(new IntegrityIssue
                        {
                            CardId = current.CardId,
                            Id = current.Id,
                            Problem = "Hash mismatch detected",
                            SuggestedFix = "Rebuild this Merkle branch"
                        });
                        break;
                    }

                    prevHash = current.Hash;
                }

            });

            await Task.WhenAll(tasks);
            var cardissues = CheckCardBalanceAccuracy(blockchain, cards);
            issues = issues.Concat(cardissues).ToList();
            return issues;
        }
        public static List<IntegrityIssue> CheckCardBalanceAccuracy(List<BlockchainEvent> blockchain, List<Card> cards)
        {
            var issues = new List<IntegrityIssue>();
            var cardEvents = blockchain
                .GroupBy(e => e.CardId)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Timestamp).ToList());

            foreach (var card in cards)
            {
                if (!cardEvents.ContainsKey(card.Id))
                {
                    issues.Add(new IntegrityIssue
                    {
                        CardId = card.Id,
                        Id = Guid.Empty,
                        Problem = "No events found for this card",
                        SuggestedFix = "Check card registration or event logging"
                    });
                    continue;
                }

                var lastEvent = cardEvents[card.Id].Last();
                if (Math.Abs(lastEvent.NewBalance - card.Balance) > 0.01)
                {
                    issues.Add(new IntegrityIssue
                    {
                        CardId = card.Id,
                        Id = lastEvent.Id,
                        Problem = $"Card balance mismatch. Blockchain: {lastEvent.NewBalance}, Card: {card.Balance}",
                        SuggestedFix = "Update stored card balance to match blockchain"
                    });
                }
            }

            return issues;
        }
        public class IntegrityIssue
        {
            public Guid CardId { get; set; }
            public Guid Id { get; set; }
            public string Problem { get; set; }
            public string SuggestedFix { get; set; }
        }

        // Define the BlockchainEvent class
        public class BlockchainEvent
            {
                public Guid Id { get; set; }
                public Guid CardId { get; set; }
                public string EventType { get; set; } // "TopUp", "TapIn", "TapOut"
                public double FareCharged { get; set; } = 0;
                public double NewBalance { get; set; }

                public string Station { get; set; }
                public DateTime Timestamp { get; set; }
                public string Hash { get; set; }
                public string PrevHash { get; set; }
                public override string ToString()
                {
                    // Safely get the first 8 characters of Hash and PrevHash, or the full string if shorter
                    var displayHash = Hash != null && Hash.Length >= 8 ? Hash.Substring(0, 8) + "..." : Hash;
                    var displayPrevHash = PrevHash != null && PrevHash.Length >= 8 ? PrevHash.Substring(0, 8) + "..." : PrevHash;

                    return $"Id: {Id}, CardId: {CardId}, Type: {EventType}, Fare: {FareCharged}, Station: {Station ?? "N/A"}, Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}, Hash: {displayHash}, PrevHash: {displayPrevHash}";
                }
            }

            // Define the Card class
            public class Card
            {
                public Guid Id { get; set; }
                public double Balance { get; set; }
                public override string ToString()
                {
                    return $"CardId: {Id}, Balance: {Balance:C}";
                }
            }
        }
    }

