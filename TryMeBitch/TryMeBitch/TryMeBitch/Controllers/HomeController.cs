using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Data;
using TryMeBitch.Models;

namespace TryMeBitch.Controllers
{
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
            var processor = new FaceProcessor(model, config, wantedFace);
            processor.ProcessVideo(input1, output1);
            processor.ProcessVideo(input2, output2);
            // Return the output video paths as static files under /Processed
            ViewBag.Video1 = "/Processed/output1.mp4";
            ViewBag.Video2 = "/Processed/output2.mp4";
            return View();
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


        [HttpGet]
        [Route("Home/Data")]
        public async Task<IActionResult> Data(string station)
        {
            var today = DateTime.Today;
            var tomorrow = DateTime.Today.AddDays(1);

           
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

            var fareData = _repo.Blockchain
                .Where(b => b.Timestamp >= today && b.Timestamp < tomorrow && b.Station.StartsWith(station))
                .AsEnumerable()
                .GroupBy(b => new DateTime(b.Timestamp.Year, b.Timestamp.Month, b.Timestamp.Day, b.Timestamp.Hour, b.Timestamp.Minute, 0))
                .Select(g => new
                {
                    x = g.Key.ToString("yyyy-MM-ddTHH:mm:ss"),
                    y = g.Sum(b => b.FareCharged)
                })
                .OrderBy(g => g.x)
                .ToList();

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

            var totalFares = fareData.Sum(x => x.y);
            var peakFare = fareData.MaxBy(x => x.y)?.y ?? 0;
            var avgFare = fareData.Count > 0 ? fareData.Average(x => x.y) : 0;
            var firstIntervalFare = fareData.FirstOrDefault()?.y ?? 0;

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
                s2n1 = totalFares.ToString("F2"),
                s2n2 = peakFare.ToString("F2"),
                s2n3 = avgFare.ToString("F2"),
                s2n4 = firstIntervalFare.ToString("F2"),
                data1 = tapData,
                data2 = fareData,
                hvacTemp = hvac?.temperature,
                hvacHumidity = hvac?.humidity,
                hvacPsi = hvac?.psi,
                hvacGasDetection = hvac?.GasDetection,
                TTemp = T?.temperature,
                THumidity = T?.humidity,
                TPsi = T?.psi,
                TGasDetection = T?.GasDetection,
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
