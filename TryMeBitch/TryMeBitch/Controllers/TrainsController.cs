using System;
using System.Collections.Generic;
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
    public class TrainsController : Controller
    {
        private readonly MRTDbContext _context;

        public TrainsController(MRTDbContext context)
        {
            _context = context;
        }

        // GET: Trains
        public async Task<IActionResult> Index()
        {
            return View(await _context.Joel_Train.ToListAsync());
        }

        // GET: Trains/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var train = await _context.Joel_Train
                .FirstOrDefaultAsync(m => m.Id == id);
            if (train == null)
            {
                return NotFound();
            }

            return View(train);
        }

        // GET: Trains/Create
        public IActionResult Create()
        {
            SetDropdowns();
            return View();
        }

        // POST: Trains/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TrainId,Name,NumCarriages,IsActive,Line,Status ")] Joel_Train train)
        {
            // ❶ Duplicate check
            bool exists = await _context.Joel_Train
                .AnyAsync(t => t.TrainId == train.TrainId);

            if (exists)
                ModelState.AddModelError("TrainId", "Train ID must be unique.");

            if (!ModelState.IsValid)
            {
                SetDropdowns();
                return View(train);
            }

            _context.Add(train);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Trains/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var train = await _context.Joel_Train.FindAsync(id);
            if (train == null)
            {
                return NotFound();
            }
            SetDropdowns();
            return View(train);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TrainId,Name,NumCarriages,IsActive,Line,Status")] Joel_Train train)
        {
            if (id != train.Id)
            {
                return NotFound();
            }

            train.TrainId = train.TrainId?.Trim();

            // ❷ Duplicate check excluding this record
            bool exists = await _context.Joel_Train
                .AnyAsync(t => t.TrainId == train.TrainId && t.Id != id);

            if (exists)
                ModelState.AddModelError("TrainId", "Train ID must be unique.");

            if (!ModelState.IsValid)
            {
                SetDropdowns();
                return View(train);
            }

            try
            {
                _context.Update(train);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TrainExists(train.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Trains/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var train = await _context.Joel_Train
                .FirstOrDefaultAsync(m => m.Id == id);
            if (train == null)
            {
                return NotFound();
            }

            return View(train);
        }

        // POST: Trains/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var train = await _context.Joel_Train.FindAsync(id);
            if (train != null)
            {
                _context.Joel_Train.Remove(train);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TrainExists(int id)
        {
            return _context.Joel_Train.Any(e => e.Id == id);
        }

        private void SetDropdowns()
        {
            ViewBag.Lines = new SelectList(new[] {
        "North-South Line", "East-West Line", "North-East Line", "Circle Line", "Downtown Line", "Thomson-East Coast Line"
    });

            ViewBag.Statuses = new SelectList(new[] {
        "On Schedule", "Maintenance", "Delayed"
    });
        }

    }
}