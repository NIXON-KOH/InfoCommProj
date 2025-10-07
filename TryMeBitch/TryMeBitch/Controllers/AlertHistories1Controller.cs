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
    [Authorize(Roles = "Engineer,Administrator")]

    public class AlertHistories1Controller : Controller
    {
        private readonly MRTDbContext _context;

        public AlertHistories1Controller(MRTDbContext context)
        {
            _context = context;
        }

        // GET: AlertHistories1
        public async Task<IActionResult> Index()
        {
            var histories = await _context.AlertHistories
                .Include(h => h.Definition)     // bring in Definition.Name
                .OrderByDescending(h => h.FiredAt)
                .ToListAsync();

            return View(histories);
        }

        // GET: AlertHistories1/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alertHistory = await _context.AlertHistories
                .Include(a => a.Definition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (alertHistory == null)
            {
                return NotFound();
            }

            return View(alertHistory);
        }

        // GET: AlertHistories1/Create
        public IActionResult Create()
        {
            ViewData["DefinitionId"] = new SelectList(_context.AlertDefinitions, "Id", "MessageTemplate");
            return View();
        }

        // POST: AlertHistories1/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DefinitionId,FiredAt,RecipientEmail,ObservedValue,MessageSent")] AlertHistory alertHistory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(alertHistory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DefinitionId"] = new SelectList(_context.AlertDefinitions, "Id", "MessageTemplate", alertHistory.DefinitionId);
            return View(alertHistory);
        }

        // GET: AlertHistories1/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alertHistory = await _context.AlertHistories.FindAsync(id);
            if (alertHistory == null)
            {
                return NotFound();
            }
            ViewData["DefinitionId"] = new SelectList(_context.AlertDefinitions, "Id", "MessageTemplate", alertHistory.DefinitionId);
            return View(alertHistory);
        }

        // POST: AlertHistories1/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DefinitionId,FiredAt,RecipientEmail,ObservedValue,MessageSent")] AlertHistory alertHistory)
        {
            if (id != alertHistory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(alertHistory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AlertHistoryExists(alertHistory.Id))
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
            ViewData["DefinitionId"] = new SelectList(_context.AlertDefinitions, "Id", "MessageTemplate", alertHistory.DefinitionId);
            return View(alertHistory);
        }

        // GET: AlertHistories1/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alertHistory = await _context.AlertHistories
                .Include(a => a.Definition)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (alertHistory == null)
            {
                return NotFound();
            }

            return View(alertHistory);
        }

        // POST: AlertHistories1/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var alertHistory = await _context.AlertHistories.FindAsync(id);
            if (alertHistory != null)
            {
                _context.AlertHistories.Remove(alertHistory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AlertHistoryExists(int id)
        {
            return _context.AlertHistories.Any(e => e.Id == id);
        }
    }
}
