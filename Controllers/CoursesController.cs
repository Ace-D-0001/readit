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
                query = query.Where(c => c.IsGeneral ||
                    c.CourseDepartments.Any(cd => cd.DepartmentId == deptId.Value));
            }

            var courses = await query.OrderBy(c => c.Code).ToListAsync();
            return View(courses);
        }

        // GET /Courses/Details?code=CSC391&sort=hot|top|new
        public async Task<IActionResult> Details(string code, string sort = "hot")
        {
            if (string.IsNullOrEmpty(code)) return NotFound();
            ViewBag.ActiveSort = sort.ToLower();

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
            ViewBag.FollowerCount = course.Followers.Count;

            // Is current user following & vote states
            bool isFollowing = false;
            var userPostVotes = new Dictionary<int, int>();
            string? currentUserId = GetCurrentUserId();

            if (!string.IsNullOrEmpty(currentUserId))
            {
                isFollowing = await _context.CourseFollows
                    .AnyAsync(cf => cf.UserId == currentUserId && cf.CourseId == course.Id);

                userPostVotes = await _context.Votes
                    .Where(v => v.UserId == currentUserId && v.TargetType == VoteTargetType.Post)
                    .ToDictionaryAsync(v => v.TargetId, v => v.VoteValue);
            }

            ViewBag.IsFollowing = isFollowing;
            ViewBag.UserPostVotes = userPostVotes;

            // Outline & Question Bank resources for right column
            ViewBag.OutlineResources      = course.Resources.Where(r => r.Type == CourseResourceType.Outline).ToList();
            ViewBag.QuestionBankResources = course.Resources.Where(r => r.Type == CourseResourceType.QuestionBank).ToList();

            // Post sorting
            if (sort.ToLower() == "top")
                course.Posts = course.Posts.OrderByDescending(p => p.UpVotes - p.DownVotes).ToList();
            else if (sort.ToLower() == "new")
                course.Posts = course.Posts.OrderByDescending(p => p.CreatedAt).ToList();
            else
                course.Posts = course.Posts
                    .OrderByDescending(p => p.IsPinned)
                    .ThenByDescending(p => (double)(p.UpVotes - p.DownVotes + 1) / Math.Pow((DateTime.UtcNow - p.CreatedAt).TotalHours + 2.0, 1.2))
                    .ToList();

            return View(course);
        }

        // POST /Courses/Follow?code=CSC391
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(string code)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Code == code);
            if (course == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId)) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var existing = await _context.CourseFollows
                .FirstOrDefaultAsync(cf => cf.UserId == currentUserId && cf.CourseId == course.Id);

            if (existing != null)
                _context.CourseFollows.Remove(existing);
            else
                _context.CourseFollows.Add(new CourseFollow { UserId = currentUserId, CourseId = course.Id, FollowedAt = DateTime.UtcNow });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { code });
        }

        // POST /Courses/AddResource
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddResource(string code, CourseResourceType type, string title, string url)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Code == code);
            if (course == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId)) return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(url))
            {
                _context.CourseResources.Add(new CourseResource
                {
                    CourseId = course.Id,
                    Type = type,
                    Title = title.Trim(),
                    Url = url.Trim(),
                    UploadedByUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { code });
        }

        // POST /Courses/AddVideo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVideo(string code, string topic, string title, string videoUrl)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Code == code);
            if (course == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId)) return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (!string.IsNullOrWhiteSpace(topic) && !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(videoUrl))
            {
                var video = new CourseVideo
                {
                    CourseId           = course.Id,
                    Topic              = topic.Trim(),
                    Title              = title.Trim(),
                    VideoUrl           = videoUrl.Trim(),
                    SubmittedByUserId  = currentUserId,
                    SubmittedAt        = DateTime.UtcNow,
                    UpVotes            = 1
                };
                _context.CourseVideos.Add(video);
                await _context.SaveChangesAsync();

                // Add submitter vote
                _context.Votes.Add(new Vote
                {
                    UserId = currentUserId,
                    TargetType = VoteTargetType.Video,
                    TargetId = video.Id,
                    VoteValue = 1
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
                var currentUserId = GetCurrentUserId();
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var existingVote = await _context.Votes
                        .FirstOrDefaultAsync(v => v.UserId == currentUserId && v.TargetType == VoteTargetType.Video && v.TargetId == videoId);

                    if (existingVote != null)
                    {
                        // Undo vote
                        _context.Votes.Remove(existingVote);
                        video.UpVotes = Math.Max(0, video.UpVotes - 1);
                    }
                    else
                    {
                        // Add vote
                        _context.Votes.Add(new Vote { UserId = currentUserId, TargetType = VoteTargetType.Video, TargetId = videoId, VoteValue = 1 });
                        video.UpVotes++;
                    }
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Details), new { code });
        }

        private string? GetCurrentUserId()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var u = _context.Users.FirstOrDefault(usr => usr.UserName == User.Identity.Name);
                if (u != null) return u.Id;
            }
            return null;
        }
    }
}
