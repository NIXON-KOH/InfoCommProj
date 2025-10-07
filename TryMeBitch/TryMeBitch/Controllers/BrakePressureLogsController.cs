using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Models;

namespace TryMeBitch.Controllers
{
    [Authorize(Roles = "Engineer,Administrator")]
    public class BrakePressureLogsController : Controller
    {
        private readonly MRTDbContext _context;
        public BrakePressureLogsController(MRTDbContext context) => _context = context;

        public async Task<IActionResult> Index(string selectedTrain)
        {
            // 1) Build dropdown options
            var activeTrains = await _context.Joel_Train
                .Where(t => t.IsActive)
                .Select(t => t.TrainId)
                .Distinct()
                .ToListAsync();

            // 2) Query logs, filter if a train is selected
            var query = _context.Joel_BrakePressureLogs.AsQueryable();
            if (!string.IsNullOrEmpty(selectedTrain))
                query = query.Where(l => l.TrainId == selectedTrain);

            // 3) Build the VM
            var vm = new BrakePressureLogsViewModel
            {
                TrainOptions = activeTrains
                                  .Select(t => new SelectListItem(t, t))
                                  .ToList(),
                SelectedTrain = selectedTrain,
                Logs = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Take(20)
                    .ToListAsync()

            };

            return View(vm);
        }

        public async Task<IActionResult> Chart(string trainId)

        {
            // Same dropdown for chart
            var activeTrains = await _context.Joel_Train
                .Where(t => t.IsActive)
                .Select(t => t.TrainId)
                .Distinct()
                .ToListAsync();

            ViewBag.Trains = new SelectList(activeTrains, trainId);
            ViewBag.SelectedTrain = trainId;

            var query = _context.Joel_BrakePressureLogs.AsQueryable();
            if (!string.IsNullOrEmpty(trainId))
                query = query.Where(l => l.TrainId == trainId);

            var data = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(20)
                .OrderBy(l => l.Timestamp) // Add this!
                .Select(l => new { l.Timestamp, l.Pressure })
                .ToListAsync();

            return View(data);
        }
    }
}
