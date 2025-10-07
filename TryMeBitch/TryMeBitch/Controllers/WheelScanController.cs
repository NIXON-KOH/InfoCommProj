using TryMeBitch.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TryMeBitch.Controllers
{
    [Route("WheelScan/[action]")]
    public class WheelScanController : Controller
    {
        private readonly MRTDbContext _context;
        public WheelScanController(MRTDbContext context)
        {
            _context = context;
        }

        public IActionResult Live()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent(string? trainId = null, int? wheel = null, int limit = 50)
        {
            var q = _context.WheelScans.AsNoTracking().AsQueryable();

            // optional filters
            if (!string.IsNullOrEmpty(trainId))
                q = q.Where(w => w.TrainId == trainId);

            if (wheel.HasValue && wheel.Value > 0)
                q = q.Where(w => w.WheelPosition == wheel.Value);

            // newest first + safety cap
            var data = q.OrderByDescending(w => w.Timestamp)
                 .Take(Math.Clamp(limit, 1, 500)).ToList();

 

            return Json(data);
        }
    }
}