using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Models;
using static System.Collections.Specialized.BitVector32;

namespace TryMeBitch.Controllers
{
    public class HVACsController : Controller
    {
        private readonly MRTDbContext _context;

        public HVACsController(MRTDbContext context)
        {
            _context = context;
        }
        public class HvacDetailsViewModel
        {
            public HVAC Hvac { get; set; }
            public Threshold Threshold { get; set; }
        }

        // GET: HVACs
        public async Task<IActionResult> Index()
        {
            return View(await _context.HVAC.ToListAsync());
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var hvac = await _context.HVAC.FindAsync(id);
            if (hvac == null)
                return NotFound();

            // Assuming you have only one threshold record or you fetch the correct one based on context.
            var threshold = await _context.Threshold.FirstOrDefaultAsync();
            if (threshold == null)
                return NotFound();

            var viewModel = new HvacDetailsViewModel
            {
                Hvac = hvac,
                Threshold = threshold
            };

            return View(viewModel);
        }



        [HttpGet]
        [Route("HVACs/data")]
        public async Task<IActionResult> data()
        {
            var hvacList = _context.HVAC.ToList();

            // Get the currently configured Thresholds (assume only one row for now)
            var threshold = _context.Threshold.FirstOrDefault();

            return Json(new
            {
                success = true,
                data = hvacList,
                threshold = threshold
            });
        }

        [HttpGet]
        public IActionResult Edit()
        {
            var threshold = _context.Threshold.FirstOrDefault();
            if (threshold == null)
                return NotFound();

            return View(threshold);
        }

        // POST: Threshold/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async  Task<IActionResult> Edit(Threshold model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var threshold = _context.Threshold.FirstOrDefault();
            if (threshold == null)
                return NotFound();

            // Update the properties
            threshold.temperature = model.temperature;
            threshold.humidity = model.humidity;
            threshold.psi = model.psi;
            threshold.GasDetection = model.GasDetection;
            _context.Update(threshold);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Threshold updated successfully!";
            return RedirectToAction("Index");
        }
    

        private bool HVACExists(Guid id)
        {
            return _context.HVAC.Any(e => e.Id == id);
        }
    }
}
