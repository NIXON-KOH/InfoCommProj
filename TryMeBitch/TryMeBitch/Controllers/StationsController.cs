using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Data;
using TryMeBitch.Models;

namespace TryMeBitch.Controllers
{
    [Authorize(Roles = "Station,Administrator")]

    public class StationsController : Controller
    {
        private readonly MRTDbContext _context;

        public StationsController(MRTDbContext context)
        {
            _context = context;
        }

        // GET: Stations
        public async Task<IActionResult> Index()
        {
            return View(await _context.stations.ToListAsync());
        }

        // GET: Stations/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var station = await _context.stations
                .FirstOrDefaultAsync(m => m.StationID == id);
            if (station == null)
            {
                return NotFound();
            }

            return View(station);
        }

        // GET: Stations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Stations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StationID,StationName,Type,StationLine,Lat,Lon,Distance,NextStation")] Station station)
        {
            if (ModelState.IsValid)
            {
                station.StationID = Guid.NewGuid();
                _context.Add(station);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(station);
        }

        // GET: Stations/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var station = await _context.stations.FindAsync(id);
            if (station == null)
            {
                return NotFound();
            }
            return View(station);
        }

        // POST: Stations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("StationID,StationName,Type,StationLine,Lat,Lon,Distance,NextStation")] Station station)
        {
            if (id != station.StationID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(station);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StationExists(station.StationID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(station);
        }

        // GET: Stations/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var station = await _context.stations
                .FirstOrDefaultAsync(m => m.StationID == id);
            if (station == null)
            {
                return NotFound();
            }

            return View(station);
        }

        // POST: Stations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var station = await _context.stations.FindAsync(id);
            if (station != null)
            {
                _context.stations.Remove(station);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StationExists(Guid id)
        {
            return _context.stations.Any(e => e.StationID == id);
        }
    }
}
