using System;
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
    public class AlertDefinitionsController : Controller
    {
        private readonly MRTDbContext _context;
        public AlertDefinitionsController(MRTDbContext context) => _context = context;

        // GET: AlertDefinitions
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.AlertDefinitions.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.Name.Contains(search) ||
                    r.TargetId.Contains(search));
            }
            ViewData["Search"] = search;
            var list = await query.ToListAsync();
            return View(list);
        }

        public IActionResult CreateBayRule()
        {
            ViewData["SelectionList"] = new SelectList(new[] { "Bay-01", "Bay-02", "Bay-03" });
            ViewData["RoleList"] = new SelectList(new[] { "Engineer1", "Engineer2", "Engineer3" });
            var model = new AlertDefinition { Type = AlertType.BayPower };
            return View("Create", model);
        }

        public IActionResult CreateCapacityRule()
        {
            ViewData["SelectionList"] = new SelectList(new[] { "Train1", "Train2", "Train3", "Train4", "Train5" });
            ViewData["RoleList"] = new SelectList(new[] { "Staff1", "Staff2", "Staff3" });
            var model = new AlertDefinition { Type = AlertType.CapacityUtilization };
            return View("Create", model);
        }

        public IActionResult CreateWheelRule()
        {
            var trains = _context.Joel_Train
                                 .Where(t => t.IsActive)
                                 .Select(t => t.TrainId)
                                 .OrderBy(id => id)
                                 .ToList();
            ViewData["SelectionList"] = new SelectList(trains);
            ViewData["RoleList"] = new SelectList(new[] { "Engineer1", "Engineer2", "Engineer3" });

            var model = new AlertDefinition
            {
                Type = AlertType.WheelMaintenance,
                Threshold = 8
            };
            return View("Create", model);
        }

        // POST: AlertDefinitions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AlertDefinition alertDefinition)
        {
            // server-side unique-name check (case-insensitive on SQL Server anyway)
            var nameExists = await _context.AlertDefinitions
                .AnyAsync(a => a.Name == alertDefinition.Name);

            if (nameExists)
                ModelState.AddModelError("Name", "An alert with this name already exists. Please choose another.");

            if (ModelState.IsValid)
            {
                _context.Add(alertDefinition);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // repopulate dropdowns when we redisplay the form
            PopulateSelectionList(alertDefinition.Type, alertDefinition.TargetId);
            PopulateRoleList(alertDefinition.Type);
            return View("Create", alertDefinition);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var m = await _context.AlertDefinitions.FindAsync(id);
            if (m == null) return NotFound();
            PopulateSelectionList(m.Type, m.TargetId);
            PopulateRoleList(m.Type);
            return View(m);
        }

        // POST: Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Type,Role,Threshold,MessageTemplate,TargetId")] AlertDefinition m)
        {
            if (id != m.Id) return NotFound();

            // unique-name check (ignore the current record)
            var nameExists = await _context.AlertDefinitions
                .AnyAsync(a => a.Name == m.Name && a.Id != id);

            if (nameExists)
                ModelState.AddModelError("Name", "An alert with this name already exists. Please choose another.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(m);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.AlertDefinitions.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
            }

            // invalid => repopulate and return
            PopulateSelectionList(m.Type, m.TargetId);
            PopulateRoleList(m.Type);
            return View(m);
        }

        // GET: AlertDefinitions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var alertDefinition = await _context.AlertDefinitions.FirstOrDefaultAsync(m => m.Id == id);
            if (alertDefinition == null) return NotFound();
            return View(alertDefinition);
        }

        // POST: AlertDefinitions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var alertDefinition = await _context.AlertDefinitions.FindAsync(id);
            if (alertDefinition != null)
            {
                _context.AlertDefinitions.Remove(alertDefinition);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private void PopulateSelectionList(AlertType t, string? selected)
        {
            if (t == AlertType.WheelMaintenance)
            {
                ViewData["SelectionList"] = new SelectList(
                    _context.Joel_Train.Where(tr => tr.IsActive).Select(tr => tr.TrainId),
                    selected);
            }
            else if (t == AlertType.BayPower)
            {
                ViewData["SelectionList"] = new SelectList(new[] { "Bay-01", "Bay-02", "Bay-03" }, selected);
            }
            else
            {
                ViewData["SelectionList"] = new SelectList(new[] { "Train1", "Train2", "Train3", "Train4", "Train5" }, selected);
            }
        }

        private void PopulateRoleList(AlertType t)
        {
            // FIX: Use OR (BayPower OR WheelMaintenance => Engineer{1..3})
            if (t == AlertType.BayPower || t == AlertType.WheelMaintenance)
                ViewData["RoleList"] = new SelectList(new[] { "Engineer1", "Engineer2", "Engineer3" });
            else
                ViewData["RoleList"] = new SelectList(new[] { "Staff1", "Staff2", "Staff3" });
        }
    }
}