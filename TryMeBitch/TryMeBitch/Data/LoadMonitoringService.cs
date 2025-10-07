
using TryMeBitch.Models;
using Microsoft.EntityFrameworkCore;
namespace TryMeBitch.Data
{
    public class LoadMonitoringService
    {
            private readonly MRTDbContext _db;
            public LoadMonitoringService(MRTDbContext db) => _db = db;


            public async Task<double> GetCurrentLoadAsync(string trainId)
            {
                var last = await _db.LoadWeights
                                   .Where(x => x.TrainId == trainId)
                                   .OrderByDescending(x => x.Timestamp)
                                   .Select(x => x.Kilograms)
                                   .FirstOrDefaultAsync();
                return last;
            }

            public async Task<List<CarriageLoadStat>> GetCarriageStatsAsync(TimeSpan window)
            {
                var cutoff = DateTime.UtcNow - window;
                return await _db.LoadWeights
                    .Where(x => x.Timestamp >= cutoff)
                    .GroupBy(x => x.TrainId)
                    .Select(g => new CarriageLoadStat
                    {
                        CarriageId = g.Key,
                        AvgLoad = g.Average(x => x.Kilograms),
                        MaxLoad = g.Max(x => x.Kilograms)
                    })
                    .ToListAsync();
            }

            public async Task<List<TimeSeriesPoint>> GetLoadTimeSeriesAsync(TimeSpan window, string trainId = "All")
            {
                var cutoff = DateTime.UtcNow - window;

                // 1) Fetch the raw readings from the database, applying the train filter if needed
                var rawQuery = _db.LoadWeights
                    .Where(x => x.Timestamp >= cutoff);

                if (!string.Equals(trainId, "All", StringComparison.OrdinalIgnoreCase))
                {
                    rawQuery = rawQuery.Where(x => x.TrainId == trainId);
                }

                // pull into memory once
                var raw = await rawQuery.ToListAsync();

                // 2) Group by minute and average
                var grouped = raw
                    .GroupBy(r => new DateTime(
                        r.Timestamp.Year,
                        r.Timestamp.Month,
                        r.Timestamp.Day,
                        r.Timestamp.Hour,
                        r.Timestamp.Minute,
                        0))
                    .OrderBy(g => g.Key)
                    .Select(g => new TimeSeriesPoint
                    {
                        Time = g.Key,
                        Value = g.Average(r => r.Kilograms)
                    })
                    .ToList();

                return grouped;
            }

        
    }
}
