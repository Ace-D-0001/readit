using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Read_It.Data;

namespace Read_It.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses.ToListAsync();
            return View(courses);
        }

        public async Task<IActionResult> Details(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.Posts)
                    .ThenInclude(p => p.User)
                .Include(c => c.Resources)
                .FirstOrDefaultAsync(m => m.Code == code);
                
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }
    }
}
