using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TryMeBitch.Models;

namespace TryMeBitch.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;

        public UsersController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // GET: /Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // GET: /Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // GET: /Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: /Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, User updatedUser)
        {
            if (id != updatedUser.Id) return NotFound();
            if (!ModelState.IsValid) return View(updatedUser);

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Email = updatedUser.Email;
            user.UserName = updatedUser.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(updatedUser);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Delete/5
        // GET: /Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: /Users/DeleteConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("DeleteConfirmed")] // This must match the form asp-action="DeleteConfirmed"
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Failed to delete user.");
                return View("Delete", user);
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
