using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Data;
using TryMeBitch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace TryMeBitch.Controllers
{
    [Authorize(Roles = "Engineer,Administrator")]

    [Route("TrainLocation/[action]")]
    public class TrainLocationController : Controller
    {
        private readonly MRTDbContext _db;
        public TrainLocationController(MRTDbContext db) => _db = db;

        // GET /TrainLocation/Live
        public IActionResult Live()
        {
            // just renders the view with your leaflet map & JS
            return View();
        }

        /// <summary>
        /// Returns the latest N GPS points (JSON) for all trains, or optionally a single train.
        /// </summary>
        /// <param name="trainId">optional filter</param>
        /// <param name="maxPoints">max points per train</param>
        [HttpGet]
        public async Task<IActionResult> GetLatest(string? trainId = null, int maxPoints = 200)
        {
            var cutoff = DateTime.Now.AddHours(-1);

            // base query
            var q = _db.TrainLocations
                       .Where(x => x.Timestamp >= cutoff);

            // if the client passed ?trainId=Train3, apply it
            if (!string.IsNullOrEmpty(trainId))
                q = q.Where(x => x.TrainId == trainId);

            // order oldest→newest, then take up to maxPoints
            var pts = await q
                  .OrderBy(x => x.Timestamp)
                  .Take(50)
                  .Select(x => new {
                      trainId = x.TrainId,
                      latitude = x.Latitude,
                      longitude = x.Longitude,
                      timestamp = x.Timestamp
                  })
                  .ToListAsync();

            // return as JSON
            return Json(pts);
        }
    }
}
