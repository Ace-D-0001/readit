using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Read_It.Data;

namespace Read_It.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Search?q=term&type=all|courses|posts
        public async Task<IActionResult> Index(string? q, string? type = "all")
        {
            ViewBag.Query = q ?? string.Empty;
            ViewBag.ActiveType = type ?? "all";

            if (string.IsNullOrWhiteSpace(q))
            {
                ViewBag.MatchingCourses = Enumerable.Empty<Models.Course>();
                ViewBag.MatchingPosts = Enumerable.Empty<Models.Post>();
                return View();
            }

            var queryLower = q.Trim().ToLower();

            // Search Courses (SubSubjects)
            var matchingCourses = await _context.Courses
                .Include(c => c.Posts)
                .Include(c => c.Resources)
                .Include(c => c.CourseDepartments)
                    .ThenInclude(cd => cd.Department)
                .Where(c => c.Code.ToLower().Contains(queryLower) ||
                            c.Title.ToLower().Contains(queryLower) ||
                            c.Description.ToLower().Contains(queryLower) ||
                            c.CourseDepartments.Any(cd => cd.Department != null && cd.Department.Name.ToLower().Contains(queryLower)))
                .ToListAsync();

            // Search Posts (Content)
            var matchingPosts = await _context.Posts
                .Include(p => p.Course)
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Where(p => p.Title.ToLower().Contains(queryLower) ||
                            p.Body.ToLower().Contains(queryLower) ||
                            p.Flair.ToString().ToLower().Contains(queryLower))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.MatchingCourses = matchingCourses;
            ViewBag.MatchingPosts = matchingPosts;

            return View();
        }
    }
}
