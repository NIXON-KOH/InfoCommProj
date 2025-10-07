using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TryMeBitch.Data;
using Microsoft.EntityFrameworkCore;

namespace TryMeBitch.Models
{

    public class Blockchain
    {
        private readonly MRTDbContext _repo;

        public Blockchain(MRTDbContext repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        /// <summary>
        /// A single validation issue discovered by the auditor.
        /// </summary>
        public class ValidationIssue
        {
            public Guid CardId { get; set; }
            public Guid EventId { get; set; }
            public string Issue { get; set; }
            public string Fix { get; set; }
        }

        /// <summary>
        /// Public entry: validate all cards and return a list of issues.
        /// This method loads all events from the database once, then performs in-memory validation.
        /// </summary>
        public List<ValidationIssue> Validate()
        {
            // Load everything once to avoid DbContext usage in parallel loops
            var events = _repo.Blockchain.AsNoTracking().ToList();

            var groupedEvents = events
                .GroupBy(e => e.CardId)
                .ToList();

            var bag = new ConcurrentBag<ValidationIssue>();

            Parallel.ForEach(groupedEvents, group =>
            {
                var cardId = group.Key;
                var ordered = group.OrderBy(e => e.Timestamp).ToList();

                // Validate per-event hashes and previoushash links
                for (int i = 0; i < ordered.Count; i++)
                {
                    var ev = ordered[i];

                    // Recompute the hash for the event
                    var recomputed = ComputeSHA256($"{ev.Id}{ev.CardId}{ev.EventType}{ev.Station}{ev.Timestamp}{ev.Amount}");
                    if (!string.Equals(ev.hash, recomputed, StringComparison.OrdinalIgnoreCase))
                    {
                        bag.Add(new ValidationIssue
                        {
                            CardId = cardId,
                            EventId = ev.Id,
                            Issue = "Event hash mismatch",
                            Fix = "Recompute and update event.hash to the computed value"
                        });
                    }

                    // Previous hash chain check (skip genesis event)
                    if (ev.previoushash == "GENESIS")
                    {
                        var prev = ordered[i - 1];
                        if (!string.Equals(ev.previoushash, prev.hash, StringComparison.OrdinalIgnoreCase))
                        {
                            bag.Add(new ValidationIssue
                            {
                                CardId = cardId,
                                EventId = ev.Id,
                                Issue = "Previous-hash link mismatch",
                                Fix = $"Set previoushash to {prev.hash}"
                            });
                        }
                    }
                }

            });

            return bag.ToList();
        }

        /// <summary>
        /// Fix all card chains in the DB. This is batched but will process card-by-card.
        /// For very large datasets you might want to add paging/streaming.
        /// </summary>
        public void FixAll()
        {
            // Load all events and process grouped in memory to minimize DB round-trips
            var allEvents = _repo.Blockchain.OrderBy(e => e.CardId).ThenBy(e => e.Timestamp).ToList();

            var groups = allEvents.GroupBy(e => e.CardId);

            foreach (var group in groups)
            {
                FixByCardInMemory(group.ToList());
            }

            _repo.SaveChanges();
        }

        /// <summary>
        /// Fix a specific CardId chain by recomputing hashes and previoushash fields in a single pass.
        /// This method expects events ordered by Timestamp.
        /// </summary>
        public void FixByCard(Guid cardId)
        {
            var events = _repo.Blockchain
                .Where(e => e.CardId == cardId)
                .OrderBy(e => e.Timestamp)
                .ToList();

            if (!events.Any()) return;

            FixByCardInMemory(events);
            _repo.SaveChanges();
        }

        /// <summary>
        /// Single-pass in-memory fixer: recompute each event's hash using canonical serialization, and set the next event's previoushash accordingly.
        /// </summary>
        private void FixByCardInMemory(List<BlockChainEvent> events)
        {
            if (events == null || events.Count == 0) return;

            // Recompute hashes in a forward single pass, also fixing previoushash as we go.
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                e.hash = ComputeSHA256($"{e.Id}{e.CardId}{e.EventType}{e.Station}{e.Timestamp}{e.Amount}");

                if (i > 0)
                {
                    e.previoushash = events[i - 1].hash;
                }
                else
                {
                    // Genesis event previoushash convention: empty string (or you may choose a fixed genesis value)
                    e.previoushash = string.Empty;
                }
            }
        }

        /// <summary>
        /// Fix a single event by Id (rebuilds the entire card chain).
        /// </summary>
        public void FixEvent(Guid eventId)
        {
            var ev = _repo.Blockchain.FirstOrDefault(e => e.Id == eventId);
            if (ev == null) return;
            FixByCard(ev.CardId);
        }

        /// <summary>
        /// Tamper a random event (useful for testing). This mutates a few fields but does not recompute the hash
        /// so it will create detectable inconsistencies.
        /// </summary>
        public void TamperRandomEvent()
        {
            var count = _repo.Blockchain.Count();
            if (count == 0) return;

            var idx = new Random().Next(0, count);
            var ev = _repo.Blockchain.Skip(idx).FirstOrDefault();
            if (ev == null) return;

            // Mutate a mix of fields to simulate realistic tampering
            ev.EventType = ev.EventType + "-Tampered";
            ev.Amount = ev.Amount + Math.Round((new Random().NextDouble() - 0.5) * 100.0, 4);

            _repo.SaveChanges();
        }

        private static string ComputeSHA256(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
            return Convert.ToHexString(sha256.ComputeHash(bytes));
        }


        public class BlockChainEvent
        {
            public Guid Id { get; set; }
            public Guid CardId { get; set; }
            public string EventType { get; set; }
            public string Station { get; set; }
            public DateTime Timestamp { get; set; }
            public double Amount { get; set; }
            public string hash { get; set; }
            public string previoushash { get; set; }

        }

    }
}
