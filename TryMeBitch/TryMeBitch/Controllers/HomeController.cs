    using System.Diagnostics;
using System.Text.Json;
using Emgu.CV.Ocl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Data;
using TryMeBitch.Models;

namespace TryMeBitch.Controllers
{
    [Authorize(Roles = "Station,Administrator")]

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly MRTDbContext _repo;
        public class StationSearchResult
        {
            public string LineName { get; set; }
            public List<string> Stations { get; set; }
        }

        public HomeController(ILogger<HomeController> logger, MRTDbContext repo)
        {
            _logger = logger;
            _repo = repo;
        }

        public IActionResult Index()
        {


            string basePath = Directory.GetCurrentDirectory();

            string deployPath = Path.Combine(basePath, "Data", "Deploy");
            string processedPath = Path.Combine(basePath, "wwwroot", "Processed");
            string uploadsPath = Path.Combine(basePath, "Data", "Uploads");

            string input1 = Path.Combine(uploadsPath, "Test.mp4");
            string input2 = Path.Combine(uploadsPath, "Test2.mp4");
            string wantedFace = Path.Combine(uploadsPath, "wanted_face.jpg");

            string model = Path.Combine(deployPath, "res10_300x300_ssd_iter_140000.caffemodel");
            string config = Path.Combine(deployPath, "deploy.prototxt.txt");

            string output1 = Path.Combine(processedPath, "output1.mp4");
            string output2 = Path.Combine(processedPath, "output2.mp4");

            // Call your reusable processor (wrapped as a helper)
            var processor = new FaceProcessor(model, config, wantedFace, _repo);
            processor.ProcessVideo(input1, output1);
            processor.ProcessVideo(input2, output2);

            string PlatformInput = Path.Combine(uploadsPath, "K4.mp4");
            string PlatformInputMeta= Path.Combine(uploadsPath, "K4_metadata.json");

            string TrackInput = Path.Combine(uploadsPath, "K6.mp4");
            string TrackInputMeta = Path.Combine(uploadsPath, "K6_metadata.json");

            string PlatformOutput = Path.Combine(processedPath, "Platform.mp4");
            string TrackOutput = Path.Combine(processedPath, "Track.mp4");
            var heatmap = new FrameDiffHeatmapWithAreas(_repo);
            heatmap.PlatformProcess(PlatformInput, PlatformOutput, PlatformInputMeta, ffmpeg: "ffmpeg.exe");
            heatmap.PlatformProcess(TrackInput, TrackOutput, TrackInputMeta, ffmpeg: "ffmpeg.exe");

            return View();
        }

        public static List<object> PredictNext30Minutes(double prob)
        {
            double GlobalTrafficAtHour(double hour)
            {
                double baseTraffic = 10;
                double morning = 30 * Math.Exp(-Math.Pow(hour - 8.0, 2) / (2 * 1.0 * 1.0));
                double lunch = 20 * Math.Exp(-Math.Pow(hour - 13.0, 2) / (2 * 0.6 * 0.6));
                double evening = 40 * Math.Exp(-Math.Pow(hour - 18.5, 2) / (2 * 1.2 * 1.2));
                return baseTraffic + morning + lunch + evening;
            }

            var forecastValues = new List<object>();
            DateTime incrementTime = DateTime.Now;
            for (int i = 1; i <= 20; i++)
            {
                DateTime futureTime = incrementTime.AddMinutes(i * 5);
                double hour = futureTime.TimeOfDay.TotalHours;

                double globalTraffic = GlobalTrafficAtHour(hour);
                double stationPrediction = globalTraffic * prob;

                // Convert timestamp to same string format ("yyyy-MM-ddTHH:mm:ss")
                string xVal = futureTime.ToString("yyyy-MM-ddTHH:mm:ss");
                forecastValues.Add(new
                {
                    x = xVal,
                    y = stationPrediction
                });
            }

            return forecastValues;
        }


        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<StationSearchResult>());

            var results = await _repo.stations
                .Where(s => EF.Functions.Like(s.StationName, $"%{query}%"))
                .GroupBy(s => s.StationLine)
                .Select(g => new StationSearchResult
                {
                    LineName = g.Key,
                    Stations = g.Select(s => s.StationName).ToList()
                })
                .ToListAsync();

            return Json(results ?? new List<StationSearchResult>());
        }
        [HttpPost]
        [Route("Home/Sabotage")]
        public async Task<IActionResult> Sabotage(string station )
        {
            var thresholds = _repo.Threshold.FirstOrDefaultAsync();
            var sabotagedHVAC = new HVAC
            {
                Id = Guid.NewGuid(),
                timestamp = DateTime.Now,
                temperature = thresholds.Result.temperature + 1,
                humidity = thresholds.Result.humidity + 1,
                psi = thresholds.Result.psi + 1,
                GasDetection = thresholds.Result.GasDetection + 1
            };
            _repo.HVAC.Add(sabotagedHVAC);
            _repo.SaveChanges();

            return await Data(station);

        }
        public double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;
            return d;
        }

        public double ToRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }
        [HttpGet]
        [Route("Home/Data")]
        public async Task<IActionResult> Data(string station)
        {
            var today = DateTime.Today;
            var tomorrow = DateTime.Today.AddDays(1);

            DateTime oneHourAgo = DateTime.Now.AddHours(-1);

            // Fetch alerts from the last hour
            var recentAlerts = await _repo.Issues
                                            .Where(a => a.status != "closed")
                                            .Select(b => new { x = b.title, y = b.Author, z = b.timestamp, a = b.Severity })
                                            .OrderByDescending(a => a.z) 
                                            .Take(4)
                                            .ToListAsync();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true, 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            };

            string jsonString = JsonSerializer.Serialize(recentAlerts, options);


            var tapData = _repo.Blockchain
                .Where(b => b.Timestamp >= today && b.Timestamp < tomorrow && b.Station.StartsWith(station) && (b.EventType == "TapIn" || b.EventType == "TapOut"))
                .AsEnumerable()
                .GroupBy(b => new DateTime(b.Timestamp.Year, b.Timestamp.Month, b.Timestamp.Day, b.Timestamp.Hour, b.Timestamp.Minute, 0))
                .Select(g => new
                {
                    x = g.Key.ToString("yyyy-MM-ddTHH:mm:ss"),
                    y = g.Count()
                })
                .OrderBy(g => g.x)
                .ToList();


            // get the selected station (or the first match in case of multiple)
            var selectedStation = _repo.stations
                .Where(s => s.StationName.StartsWith(station))
                .Select(s => new { s.Lat, s.Lon })
                .FirstOrDefault();

            // find most recent location for each train
            var latestLocations = _repo.TrainLocations
                .GroupBy(tl => tl.TrainId)
                .Select(g => g.OrderByDescending(tl => tl.Timestamp).First()).ToList();

            // find the train nearest to the selected station
            var nearestTrain = latestLocations
                                    .Select(tl => new
                                    {
                                        TrainId = tl.TrainId,
                                        Distance = CalculateHaversineDistance(
                                            tl.Latitude,
                                            tl.Longitude,
                                            (double)selectedStation.Lat,
                                            (double)selectedStation.Lon
                                        )
                                    })
                                    .OrderBy(item => item.Distance)
                                    .FirstOrDefault();

            // load all weight measurements for that train
            var loadWeights = _repo.LoadWeights
                .Where(lw => lw.Timestamp >= today && lw.Timestamp < tomorrow && lw.TrainId == nearestTrain.TrainId)
                .AsEnumerable()
                .GroupBy(lw => new DateTime(lw.Timestamp.Year, lw.Timestamp.Month, lw.Timestamp.Day, lw.Timestamp.Hour, lw.Timestamp.Minute, 0))
                .Select(g => new
                {
                    x = g.Key.ToString("yyyy-MM-ddTHH:mm:ss"),
                    y = g.MaxBy(lw => lw.Kilograms)?.Kilograms ?? 0 
                })
                .OrderBy(lw => lw.x)
                .ToList();

            double time = nearestTrain.Distance / 70 * 60;
          
            var stationCount = _repo.Blockchain.Count(b => b.Timestamp >= today && b.Timestamp < tomorrow && b.Station.StartsWith(station) && (b.EventType == "TapIn" || b.EventType == "TapOut"));
            int rowCount = _repo.Blockchain.Count(b => b.Timestamp >= today && b.Timestamp < tomorrow  && (b.EventType == "TapIn" || b.EventType == "TapOut"));
            
            rowCount = (rowCount == 0) ? 1 : rowCount; 
            var Predjson =  PredictNext30Minutes((double)stationCount / rowCount);  
            

            var Active = await _repo.stations.FirstOrDefaultAsync(b => b.StationName == station);

            var tapInCount = await _repo.Blockchain.CountAsync(b => b.Timestamp >= today && b.Timestamp < tomorrow && b.Station.StartsWith(station) && b.EventType == "TapIn");
            var tapOutCount = await _repo.Blockchain.CountAsync(b => b.Timestamp >= today && b.Timestamp < tomorrow && b.Station.StartsWith(station) && b.EventType == "TapOut");

            double totalTapped = tapInCount + tapOutCount;
            double taps, tapRatio;

            if (totalTapped == 0) { taps = 0.5; }
            else { taps = (double)tapInCount / totalTapped; }

            tapRatio = (taps * 10.0);

            var totalTaps = tapData.Sum(x => x.y);
            var peakTap = tapData.MaxBy(x => x.y)?.y ?? 0;
            var avgTap = tapData.Count > 0 ? tapData.Average(x => x.y) : 0;
            var firstIntervalTap = tapData.FirstOrDefault()?.y ?? 0;

            var MinimumWeight = loadWeights.MinBy(x => x.y)?.y ?? 0;
            var peakWeight = loadWeights.MaxBy(x => x.y)?.y ?? 0;
            var avgWeight = loadWeights.Count > 0 ? loadWeights.Average(x => x.y) : 0;
            var lastMeasuredWeight = loadWeights.LastOrDefault()?.y ?? 0;

            var coordinates = await _repo.stations
                .Where(n => n.StationName.StartsWith(station))
                .Select(s => new { s.Lat, s.Lon, s.StationLine })
                .FirstOrDefaultAsync();

            var hvac = await _repo.HVAC.OrderByDescending(t => t.timestamp).FirstOrDefaultAsync();
            var T = await _repo.Threshold.FirstOrDefaultAsync();

            return Json(new
            {
                success = true,
                tapRatio = tapRatio,
                isActive = Active?.Active ?? false,
                lat = coordinates?.Lat,
                lon = coordinates?.Lon,
                stationLine = coordinates?.StationLine,
                s1n1 = totalTaps.ToString(),
                s1n2 = peakTap.ToString(),
                s1n3 = avgTap.ToString("F2"),
                s1n4 = firstIntervalTap.ToString(),
                s2n1 = MinimumWeight.ToString("F2"),
                s2n2 = peakWeight.ToString("F2"),
                s2n3 = avgWeight.ToString("F2"),
                s2n4 = lastMeasuredWeight.ToString("F2"),
                data1 = tapData,
                data2 = loadWeights,
                traintiming = time,
                hvacTemp = hvac?.temperature,
                hvacHumidity = hvac?.humidity,
                hvacPsi = hvac?.psi,
                hvacGasDetection = hvac?.GasDetection,
                TTemp = T?.temperature,
                THumidity = T?.humidity,
                TPsi = T?.psi,
                TGasDetection = T?.GasDetection,
                alerts = recentAlerts,
                Pred = Predjson
            });
        }

      

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
