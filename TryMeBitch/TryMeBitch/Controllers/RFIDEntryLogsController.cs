using TryMeBitch.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace TryMeBitch.Controllers
{
    [Authorize(Roles = "Engineer,Administrator")]
    public class RFIDEntryLogsController : Controller
    {
        private readonly MRTDbContext _context;
        public RFIDEntryLogsController(MRTDbContext ct) => _context = ct;

        public async Task<IActionResult> Index(string selectedTrain)
        {
            // Dropdown options (active trains)
            var activeTrains = await _context.Joel_Train
                .Select(t => t.TrainId)
                .Distinct()
                .ToListAsync();

            var query = _context.Joel_RFIDEntryLogs.AsQueryable();
            if (!string.IsNullOrEmpty(selectedTrain))
                query = query.Where(l => l.TrainId == selectedTrain);

            var vm = new RFIDEntryLogsViewModel
            {
                TrainOptions = activeTrains.Select(t => new SelectListItem(t, t)).ToList(),
                SelectedTrain = selectedTrain,
                Logs = await query.OrderByDescending(l => l.EntryTime).Take(20).ToListAsync()
            };

            return View(vm);
        }


        public async Task<IActionResult> Chart(string trainId)
        {
            // Dropdown options
            var trains = await _context.Joel_Train.Select(t => t.TrainId).Distinct().ToListAsync();
            ViewBag.TrainOptions = new SelectList(trains, trainId);
            ViewBag.SelectedTrain = trainId;

            // Get logs and filter if trainId selected
            var query = _context.Joel_RFIDEntryLogs.AsQueryable();
            if (!string.IsNullOrEmpty(trainId))
                query = query.Where(x => x.TrainId == trainId);

            var now = DateTime.UtcNow;
            var firstMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-11); // 12 months

            var data = await query
                .OrderBy(l => l.EntryTime)
                .Where(x => x.EntryTime >= firstMonth)
                .GroupBy(x => new { x.EntryTime.Year, x.EntryTime.Month, x.EntryStatus })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Status = g.Key.EntryStatus, // "Entered"/"Exited"
                    Count = g.Count()
                })
                .ToListAsync();

            var labels = new List<string>();
            var entries = new List<int>();
            var exits = new List<int>();

            for (int i = 0; i < 12; i++)
            {
                var dt = firstMonth.AddMonths(i);
                labels.Add(dt.ToString("MMM yyyy"));
                var entry = data.FirstOrDefault(x => x.Year == dt.Year && x.Month == dt.Month && x.Status == "Entered");
                var exit = data.FirstOrDefault(x => x.Year == dt.Year && x.Month == dt.Month && x.Status == "Exited");
                entries.Add(entry?.Count ?? 0);
                exits.Add(exit?.Count ?? 0);
            }

            ViewBag.Labels = labels;
            ViewBag.Entries = entries;
            ViewBag.Exits = exits;


            return View();
        }


    }
}
