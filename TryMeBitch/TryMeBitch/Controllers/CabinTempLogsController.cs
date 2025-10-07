using TryMeBitch.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace TryMeBitch.Controllers
{
    [Authorize(Roles = "Engineer,Administrator")]
    public class CabinTempLogsController : Controller
        {
            private readonly MRTDbContext _context;
            public CabinTempLogsController(MRTDbContext context) => _context = context;

            // Index for logs + filter
            public async Task<IActionResult> Index(string selectedTrain)
            {
                var activeTrains = await _context.Joel_Train
                    .Where(t => t.IsActive)
                    .Select(t => t.TrainId)
                    .Distinct()
                    .ToListAsync();

                var query = _context.Joel_CabinTempLogs.AsQueryable();
                if (!string.IsNullOrEmpty(selectedTrain))
                    query = query.Where(l => l.TrainId == selectedTrain);

                var vm = new CabinTempLogsViewModel
                {
                    TrainOptions = activeTrains.Select(t => new SelectListItem(t, t)).ToList(),
                    SelectedTrain = selectedTrain,
                    Logs = await query
                        .OrderByDescending(l => l.Timestamp)
                        .Take(20)
                        .ToListAsync()
                };

                return View(vm);
            }

            // Chart for logs + filter
            public async Task<IActionResult> Chart(string trainId)
            {
                var activeTrains = await _context.Joel_Train
                    .Where(t => t.IsActive)
                    .Select(t => t.TrainId)
                    .Distinct()
                    .ToListAsync();

                ViewBag.Trains = new SelectList(activeTrains, trainId);
                ViewBag.SelectedTrain = trainId;

                var query = _context.Joel_CabinTempLogs.AsQueryable();
                if (!string.IsNullOrEmpty(trainId))
                    query = query.Where(l => l.TrainId == trainId);

            var data = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(20)
                .OrderBy(l => l.Timestamp) // Add this!
                .Select(l => new { l.Timestamp, l.Temperature })
                .ToListAsync();

            return View(data);
            }
        }


    }

