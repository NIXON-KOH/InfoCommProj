using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Blockchain
{
    public class BlockChain
    {
        public static Dictionary<Guid, string> CardMerkleRoots = new Dictionary<Guid, string>();
        public static Dictionary<Guid, List<BlockchainEvent>> CardEventHistory = new Dictionary<Guid, List<BlockchainEvent>>();

      

        // Rebuild and check individual branches of the Merkle tree
        public static void Check(List<BlockchainEvent> blockchain, List<Card> cards)
        {
            foreach (var card in cards)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[CARD] {card.Id}");
                Console.ResetColor();

                double computedBalance = 0;
                var events = blockchain
                    .Where(e => e.CardId == card.Id)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                foreach (var e in events)
                {
                    switch (e.EventType)
                    {
                        case "TopUp":
                            computedBalance += e.FareCharged;
                            PrintEvent(e, computedBalance);
                            break;

                        case "TapIn":
                        case "TapOut":
                            computedBalance -= e.FareCharged;
                            PrintEvent(e, computedBalance);
                            break;
                    }
                }

                Console.ForegroundColor = (computedBalance == card.Balance) ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"→ Final Balance: ${computedBalance:0.00} | " +
                    $"Recorded: ${card.Balance:0.00} " +
                    $"({(computedBalance == card.Balance ? "✔ VALID" : "✖ INVALID")})\n");
                Console.ResetColor();
            }
        }
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
        public static async Task CheckBlockchainIntegrity(List<BlockchainEvent> blockchain)
        {
            var chains = blockchain.GroupBy(e => e.CardId);

            var tasks = chains.Select(async cardChain =>
            {
                Console.WriteLine($"\nChecking chain for Card {cardChain.Key}");
                string prevHash = "Genesis";
                var orderedEvents = cardChain.OrderBy(e => e.Timestamp).ToList();

                for (int i = 0; i < orderedEvents.Count; i++)
                {
                    var current = orderedEvents[i];
                    string calculatedHash = ComputeHash(current);
                    if (current.Hash != calculatedHash)
                    {
                        // Flagging the invalid event (and re-building only this branch)
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ Invalid event detected at position {i}");
                        Console.WriteLine($"   Stored Hash: {current.Hash}");
                        Console.WriteLine($"   Calculated Hash: {calculatedHash}");
                        Console.ResetColor();

                        // Rebuild the affected Merkle branch
                        Console.WriteLine($"Rebuilding affected Merkle branch...");
                        string newMerkleRoot = await RebuildMerkleBranch(orderedEvents.Take(i + 1).ToList());
                        Console.WriteLine($"New Merkle Root: {newMerkleRoot.Substring(0, 8)}...");

                        // Update the Merkle Root for the card
                        CardMerkleRoots[cardChain.Key] = newMerkleRoot;
                        CardEventHistory[cardChain.Key] = orderedEvents.Take(i + 1).ToList();
                        Console.WriteLine("Checking Again..");

                        // Return early from this task
                        return;
                    }
                    prevHash = current.Hash;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Card chain validated successfully!");
                Console.ResetColor();
            });

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);
        }
        public static async Task<string> RebuildMerkleBranch(List<BlockchainEvent> affectedEvents)
        {
            Console.WriteLine("Starting Merkle rebuild...");
            Console.WriteLine($"Number of events to rebuild: {affectedEvents.Count}");

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

            return hashes[0];
        }

        // Simulate tampering of blockchain events
        public static void TamperBlockchain(List<BlockchainEvent> blockchain)
        {
            var rand = new Random();
            var index = rand.Next(blockchain.Count);
            var tamperedEvent = blockchain[index];

            if (tamperedEvent != null)
            {
                // Modify the event (simulating tampering)
                tamperedEvent.Timestamp = DateTime.Parse("2024-04-23T08:00:00");
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
        static void PrintEvent(BlockchainEvent e, double newBalance)
        {
            Console.WriteLine($"[{e.Timestamp:MM/dd/yy HH:mm:ss}] {e.EventType} - " +
                $"{(e.EventType == "TopUp" ? "+$" + e.FareCharged : "-$" + e.FareCharged)} → " +
                $"Balance: ${newBalance:0.00}");
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

                return $"EventId: {Id}, CardId: {CardId}, Type: {EventType}, Fare: {FareCharged}, New Balance: {NewBalance} Station: {Station ?? "N/A"}, Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}, Hash: {displayHash}, PrevHash: {displayPrevHash}";
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
