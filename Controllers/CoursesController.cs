using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Read_It.Data;
using Read_It.Models;

namespace Read_It.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Courses?deptId=N
        public async Task<IActionResult> Index(int? deptId)
        {
            // Load departments for filter pills
            var departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
            ViewBag.Departments  = departments;
            ViewBag.ActiveDeptId = deptId;

            var query = _context.Courses
                .Include(c => c.CourseDepartments)
                    .ThenInclude(cd => cd.Department)
                .Include(c => c.Posts)
                .Include(c => c.Followers)
                .AsQueryable();

            if (deptId.HasValue)
            {
                // Show: General courses PLUS courses linked to the selected department
                query = query.Where(c => c.IsGeneral ||
                    c.CourseDepartments.Any(cd => cd.DepartmentId == deptId.Value));
            }

            var courses = await query.OrderBy(c => c.Code).ToListAsync();
            return View(courses);
        }

        // GET /Courses/Details?code=CSC391
        public async Task<IActionResult> Details(string code)
        {
            if (string.IsNullOrEmpty(code)) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Posts)
                    .ThenInclude(p => p.User)
                .Include(c => c.Posts)
                    .ThenInclude(p => p.Comments)
                .Include(c => c.Resources)
                .Include(c => c.CourseDepartments)
                    .ThenInclude(cd => cd.Department)
                .Include(c => c.Followers)
                .Include(c => c.Videos)
                    .ThenInclude(v => v.SubmittedByUser)
                .FirstOrDefaultAsync(c => c.Code == code);

            if (course == null) return NotFound();

            // Videos grouped by topic, each group sorted by UpVotes desc
            var videosByTopic = course.Videos
                .GroupBy(v => v.Topic)
                .Select(g => new
                {
                    Topic  = g.Key,
                    Videos = g.OrderByDescending(v => v.UpVotes).ToList()
                })
                .OrderBy(g => g.Topic)
                .ToList();

            ViewBag.VideosByTopic = videosByTopic;

            // Follower count
            ViewBag.FollowerCount = course.Followers.Count;

            // Is current user following?
            bool isFollowing = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                if (currentUser != null)
                {
                    isFollowing = await _context.CourseFollows
                        .AnyAsync(cf => cf.UserId == currentUser.Id && cf.CourseId == course.Id);
                }
            }
            ViewBag.IsFollowing = isFollowing;

            // Outline & Question Bank resources for right column
            ViewBag.OutlineResources     = course.Resources.Where(r => r.Type == CourseResourceType.Outline).ToList();
            ViewBag.QuestionBankResources= course.Resources.Where(r => r.Type == CourseResourceType.QuestionBank).ToList();

            return View(course);
        }

        // POST /Courses/Follow?code=CSC391
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(string code)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Code == code);
            if (course == null) return NotFound();

            if (User.Identity?.IsAuthenticated != true)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (currentUser == null) return Unauthorized();

            var existing = await _context.CourseFollows
                .FirstOrDefaultAsync(cf => cf.UserId == currentUser.Id && cf.CourseId == course.Id);

            if (existing != null)
                _context.CourseFollows.Remove(existing);   // Unfollow
            else
                _context.CourseFollows.Add(new CourseFollow { UserId = currentUser.Id, CourseId = course.Id });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { code });
        }

        // POST /Courses/AddVideo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVideo(string code, string topic, string title, string videoUrl)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Code == code);
            if (course == null) return NotFound();

            if (User.Identity?.IsAuthenticated != true)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (currentUser == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(topic) && !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(videoUrl))
            {
                _context.CourseVideos.Add(new CourseVideo
                {
                    CourseId           = course.Id,
                    Topic              = topic.Trim(),
                    Title              = title.Trim(),
                    VideoUrl           = videoUrl.Trim(),
                    SubmittedByUserId  = currentUser.Id,
                    SubmittedAt        = DateTime.UtcNow,
                    UpVotes            = 1
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { code });
        }

        // POST /Courses/UpvoteVideo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpvoteVideo(int videoId, string code)
        {
            var video = await _context.CourseVideos.FindAsync(videoId);
            if (video != null)
            {
                video.UpVotes++;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { code });
        }
    }
}
