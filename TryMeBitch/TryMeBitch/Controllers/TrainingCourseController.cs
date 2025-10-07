using Microsoft.AspNetCore.Mvc;
using TryMeBitch.Models;

namespace TryMeBitch.Controllers
{
    public class TrainingCourseController : Controller
    {
        private readonly MRTDbContext _dbContext;
        public TrainingCourseController(MRTDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpGet]
        public IActionResult Index()
        {
            var training = _dbContext.TrainingCourses.ToList();
            return View(training);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(TrainingCourse training)
        {
            if (ModelState.IsValid)
            {
                _dbContext.TrainingCourses.Add(training);
                _dbContext.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(training);
        }
    }
}
