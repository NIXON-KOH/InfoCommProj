using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Models;

namespace TryMeBitch.Controllers
{
    public class IssuesController : Controller
    {
        private readonly MRTDbContext _context;

        public IssuesController(MRTDbContext context)
        {
            _context = context;
        }

        // GET: Issues
        public async Task<IActionResult> Index()
        {
            return View(await _context.Issues.ToListAsync());
        }


        public class IssueDetailsViewModel
        {
            public Issues Issue { get; set; }
            public List<comment> Comments { get; set; }
            public List<Timeline> Timelines { get; set; }
        }


        // GET: Issues/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty)
            {
                return NotFound();
            }

            var issue = await _context.Issues
                .FirstOrDefaultAsync(m => m.id == id);

            if (issue == null)
            {
                return NotFound();
            }

            var comments = await _context.comments
                .Where(c => c.IssueId == id)
                .OrderByDescending(c => c.timestamp)
                .ToListAsync();

            var timelines = await _context.Timelines
                .Where(t => t.IssueId == id)
                .OrderBy(t => t.timestamp)
                .ToListAsync();

            var viewModel = new IssueDetailsViewModel
            {
                Issue = issue,
                Comments = comments,
                Timelines = timelines
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(Guid issueId, string content, string station)
        {
            Console.WriteLine($"Add Comment: {issueId} {content} {station}");
            if (string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError("content", "Comment content cannot be empty.");
                return RedirectToAction(nameof(Details), new { id = issueId });
            }
            var comment = new comment
            {
                id = Guid.NewGuid(),
                IssueId = issueId,
                station = station,
                Author = User.Identity.Name ?? "Unknown Author",
                content = content,
                timestamp = DateTime.Now
            };
            Console.WriteLine(comment.ToString());
            _context.comments.Add(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = issueId });
        }

        // GET: Issues/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Issues/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Issues issues)
        {

            issues.id = Guid.NewGuid();
            issues.Author = User.Identity.Name ?? "Unknown Author";
            issues.timestamp = DateTime.Now;
            Console.WriteLine(issues.ToString());
            ModelState.Remove("id");
            ModelState.Remove("Author");
            ModelState.Remove("timestamp");

            if (ModelState.IsValid)
            {
                Timeline initial = new Timeline
                {
                    id = Guid.NewGuid(),
                    IssueId = issues.id,
                    Author = issues.Author,
                    content = "Issue created",
                    timestamp = DateTime.Now
                };
                _context.Timelines.Add(initial);
                _context.Add(issues);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(issues);
        }

        // GET: Issues/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var issues = await _context.Issues.FindAsync(id);
            if (issues == null)
            {
                return NotFound();
            }
            return View(issues);
        }

        // POST: Issues/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Issues issues)
        {
            if (id != issues.id)
            {
                return NotFound();
            }

            ModelState.Remove("id");
            ModelState.Remove("Author");
            ModelState.Remove("timestamp");
            ModelState.Remove("Station");
            ModelState.Remove("title");

            if (ModelState.IsValid)
            {
                // Retrieve the original entity from DB (no tracking so EF won't overwrite it yet)
                var oldIssue = await _context.Issues.AsNoTracking().FirstOrDefaultAsync(i => i.id == id);
                if (oldIssue == null)
                {
                    return NotFound();
                }

                // Track the changes
                var changes = new List<string>();
                if (oldIssue.summary != issues.summary)
                    changes.Add($"Summary changed from '{oldIssue.summary}' to '{issues.summary}'");

                if (oldIssue.Severity != issues.Severity)
                    changes.Add($"Severity changed from '{oldIssue.Severity}' to '{issues.Severity}'");

                if (oldIssue.status != issues.status)
                    changes.Add($"Status changed from '{oldIssue.status}' to '{issues.status}'");

                if (oldIssue.station != issues.station)
                    changes.Add($"Station changed from '{oldIssue.station}' to '{issues.station}'");

                // Save changes to Issues table
                try
                {
                    _context.Update(issues);
                    await _context.SaveChangesAsync();

                    // Add to Timeline table if any changes
                    if (changes.Any())
                    {
                        _context.Timelines.Add(new Timeline
                        {
                            id = Guid.NewGuid(),
                            IssueId = issues.id,
                            Author = User.Identity.Name ?? "Unknown Author",
                            content = string.Join("; ", changes), // Join all change descriptions
                            timestamp = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IssuesExists(issues.id))
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
            return View(issues);
        }


        // GET: Issues/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var issues = await _context.Issues
                .FirstOrDefaultAsync(m => m.id == id);
            if (issues == null)
            {
                return NotFound();
            }

            return View(issues);
        }

        // POST: Issues/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var issues = await _context.Issues.FindAsync(id);
            var timelines = await _context.Timelines
                .Where(t => t.IssueId == id)
                .ToListAsync();
            var comments = await _context.comments.Where(c => c.IssueId == id)
                .ToListAsync();
            if (issues != null)
            {
                
                _context.Timelines.RemoveRange(timelines);
                await _context.SaveChangesAsync();
                _context.comments.RemoveRange(comments);
                await _context.SaveChangesAsync();
                _context.Issues.Remove(issues);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(issues);


        }

        private bool IssuesExists(Guid id)
        {
            return _context.Issues.Any(e => e.id == id);
        }
    }
}
