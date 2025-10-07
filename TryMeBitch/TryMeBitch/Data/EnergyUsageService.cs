using System;
using TryMeBitch.Models;
using Microsoft.EntityFrameworkCore;

namespace TryMeBitch.Data
{
    public class EnergyUsageService
    {
        private readonly MRTDbContext _db;
        public EnergyUsageService(MRTDbContext db) => _db = db;

        /// <summary>
        /// Returns per-bay average, min, max over the past time window.
        /// </summary>
        public async Task<List<BayEnergyStat>> GetBayStatsAsync(TimeSpan window)
        {
            var cutoff = DateTime.UtcNow - window;
            return await _db.DepotEnergySlots
                .Where(x => x.Timestamp >= cutoff)
                .GroupBy(x => x.BayId)
                .Select(g => new BayEnergyStat
                {
                    BayId = g.Key,
                    AvgWatts = g.Average(x => x.Watts),
                    MinWatts = g.Min(x => x.Watts),
                    MaxWatts = g.Max(x => x.Watts)
                })
                .ToListAsync();
        }

        /// <summary>
        /// Flags any readings above a given threshold as alerts.
        /// </summary>
        public async Task<List<DepotEnergySlot>> DetectHighUsageAsync(double threshold)
            => await _db.DepotEnergySlots
                        .Where(x => x.Watts >= threshold)
                        .OrderByDescending(x => x.Timestamp)
                        .Take(20)
                        .ToListAsync();
    
}
}
