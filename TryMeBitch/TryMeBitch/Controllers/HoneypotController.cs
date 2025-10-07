using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TryMeBitch.Models;

namespace TryMeBitch.Controllers
{
    public class HoneypotController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly MRTDbContext _repo;
        private readonly SignInManager<User> _signInManager;
        public HoneypotController(UserManager<User> userManager, MRTDbContext repo, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _repo = repo;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (ipAddress == null)
                throw new Exception("No IPv4 address found for this machine.");

            return ipAddress.ToString();
        }

        [HttpPost("lockout")]
        [Route("/honeypot/lockout")]
        public async Task<IActionResult> lockout()
        {
            string localIP = GetLocalIPAddress();
            //add issue
            var issue = new Issues
            {
            id = Guid.NewGuid(),
            Author = "System",
            station = "Honeypot",
            title = "Honeypot Triggered",
            summary = $"A honeypot was triggered, indicating a potential security breach. Logged Ip: {localIP}",
            Severity = "High",
            status = "Open",
            timestamp = DateTime.UtcNow
     
            };
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
                return NotFound("User not found.");

            // Enable lockout and set lockout end date (e.g., 1 day from now)
            
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1));
            await _signInManager.SignOutAsync();
            return Content("Shouldnt be seeing this");
        }
    }
}
