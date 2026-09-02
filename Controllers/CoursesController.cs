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

        // GET /Courses?search=keyword
        public async Task<IActionResult> Index(string? search)
        {
            ViewBag.SearchQuery = search;

            var query = _context.Courses
                .Include(c => c.Posts)
                .Include(c => c.Followers)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Code.Contains(search) || c.Title.Contains(search) || c.Description.Contains(search));
            }

            var courses = await query.OrderBy(c => c.Code).ToListAsync();
            return View(courses);
        }

        // GET /Courses/Details?code=CSC391&sort=hot|top|new&examCategory=Midterm
        public async Task<IActionResult> Details(string code, string sort = "hot", string? examCategory = null)
        {
            if (string.IsNullOrEmpty(code)) return NotFound();
            ViewBag.ActiveSort = sort.ToLower();

            var course = await _context.Courses
                .Include(c => c.Posts)
                    .ThenInclude(p => p.User)
                .Include(c => c.Posts)
                    .ThenInclude(p => p.Comments)
                .Include(c => c.Resources)
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
            var userVideoVotes = new List<int>();
            string? currentUserId = GetCurrentUserId();

            if (!string.IsNullOrEmpty(currentUserId))
            {
                isFollowing = await _context.CourseFollows
                    .AnyAsync(cf => cf.UserId == currentUserId && cf.CourseId == course.Id);

                userPostVotes = await _context.Votes
                    .Where(v => v.UserId == currentUserId && v.TargetType == VoteTargetType.Post)
                    .ToDictionaryAsync(v => v.TargetId, v => v.VoteValue);

                userVideoVotes = await _context.Votes
                    .Where(v => v.UserId == currentUserId && v.TargetType == VoteTargetType.Video)
                    .Select(v => v.TargetId)
                    .ToListAsync();
            }

            ViewBag.IsFollowing = isFollowing;
            ViewBag.UserPostVotes = userPostVotes;
            ViewBag.UserVideoVotes = userVideoVotes;

            // Outline & Notes resources for right column (All published and non-rejected resources are publicly visible)
            ViewBag.OutlineResources = course.Resources.Where(r => r.Type == CourseResourceType.Outline && r.Status != ResourceStatus.Rejected).ToList();
            var approvedNotes = course.Resources.Where(r => r.Type == CourseResourceType.Notes && r.Status != ResourceStatus.Rejected).AsQueryable();
            if (!string.IsNullOrWhiteSpace(examCategory) && !examCategory.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                approvedNotes = approvedNotes.Where(r => r.ExamCategory != null && r.ExamCategory.Equals(examCategory, StringComparison.OrdinalIgnoreCase));
            }
            ViewBag.NotesResources = approvedNotes.ToList();
            ViewBag.ActiveExamCategory = string.IsNullOrWhiteSpace(examCategory) ? "all" : examCategory;

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

        // POST /Courses/Follow
        [HttpPost]
        public async Task<IActionResult> Follow(string code)
        {
            if (User.IsInRole("Admin"))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Admins cannot follow courses." });

                TempData["ErrorMessage"] = "Administrators automatically manage and oversee all subReadIt subjects.";
                return RedirectToAction(nameof(Details), new { code });
            }

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Code == code);
            if (course == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, requireAuth = true });

                return RedirectToAction("Login", "Account");
            }

            var existing = await _context.CourseFollows
                .FirstOrDefaultAsync(cf => cf.UserId == currentUserId && cf.CourseId == course.Id);

            bool isFollowing;
            if (existing != null)
            {
                _context.CourseFollows.Remove(existing);
                isFollowing = false;
            }
            else
            {
                _context.CourseFollows.Add(new CourseFollow { UserId = currentUserId, CourseId = course.Id, FollowedAt = DateTime.UtcNow });
                isFollowing = true;
            }

            await _context.SaveChangesAsync();

            int followerCount = await _context.CourseFollows.CountAsync(cf => cf.CourseId == course.Id);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, isFollowing, followerCount });
            }

            return RedirectToAction(nameof(Details), new { code });
        }

        // POST /Courses/UploadNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadNote(string code, string title, string? description, string? examCategory, Microsoft.AspNetCore.Http.IFormFile? file, string? url)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Code == code);
            if (course == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId)) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "Note title is required.";
                return RedirectToAction(nameof(Details), new { code });
            }

            string? filePath = null;
            if (file != null && file.Length > 0)
            {
                var uploadsDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "uploads", "notes");
                if (!System.IO.Directory.Exists(uploadsDir))
                {
                    System.IO.Directory.CreateDirectory(uploadsDir);
                }

                var uniqueFileName = System.Guid.NewGuid().ToString() + System.IO.Path.GetExtension(file.FileName);
                var fullPath = System.IO.Path.Combine(uploadsDir, uniqueFileName);

                using (var stream = new System.IO.FileStream(fullPath, System.IO.FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                filePath = "/uploads/notes/" + uniqueFileName;
            }

            var noteResource = new CourseResource
            {
                CourseId = course.Id,
                Type = CourseResourceType.Notes,
                Title = title.Trim(),
                Description = description?.Trim(),
                ExamCategory = string.IsNullOrWhiteSpace(examCategory) ? "General" : examCategory.Trim(),
                Url = !string.IsNullOrWhiteSpace(url) ? url.Trim() : (filePath ?? string.Empty),
                FilePath = filePath,
                UploadedByUserId = currentUserId,
                Status = ResourceStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };

            _context.CourseResources.Add(noteResource);
            await _context.SaveChangesAsync();

            // ── Notify Admins ──
            await NotifyAdminsAsync(
                "📚 New Study Note Uploaded",
                $"u/{User.Identity?.Name ?? "A student"} uploaded note \"{noteResource.Title}\" in c/{course.Code}",
                $"/Courses/Details?code={course.Code}#notes");

            TempData["SuccessMessage"] = "Your note has been uploaded successfully and is now visible to all students!";
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
            if (string.IsNullOrEmpty(currentUserId)) return RedirectToAction("Login", "Account");

            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(url))
            {
                var cleanUrl = url.Trim();
                if (!cleanUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !cleanUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    cleanUrl = "https://" + cleanUrl;
                }

                _context.CourseResources.Add(new CourseResource
                {
                    CourseId = course.Id,
                    Type = type,
                    Title = title.Trim(),
                    Url = cleanUrl,
                    UploadedByUserId = currentUserId,
                    Status = ResourceStatus.Approved,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                // ── Notify Admins ──
                await NotifyAdminsAsync(
                    "🔗 New Course Outline Link",
                    $"u/{User.Identity?.Name ?? "A student"} added outline link \"{title.Trim()}\" in c/{course.Code}",
                    $"/Courses/Details?code={course.Code}");

                TempData["SuccessMessage"] = "Course outline link added successfully and is now visible to all students!";
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
            if (string.IsNullOrEmpty(currentUserId)) return RedirectToAction("Login", "Account");

            if (!string.IsNullOrWhiteSpace(topic) && !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(videoUrl))
            {
                var cleanUrl = videoUrl.Trim();
                if (!cleanUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !cleanUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    cleanUrl = "https://" + cleanUrl;
                }

                var video = new CourseVideo
                {
                    CourseId           = course.Id,
                    Topic              = topic.Trim(),
                    Title              = title.Trim(),
                    VideoUrl           = cleanUrl,
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

                // ── Notify Admins ──
                await NotifyAdminsAsync(
                    "🎥 New Course Video Added",
                    $"u/{User.Identity?.Name ?? "A student"} added video \"{video.Title}\" under topic \"{video.Topic}\" in c/{course.Code}",
                    $"/Courses/Details?code={course.Code}");

                TempData["SuccessMessage"] = "Video submitted successfully and is now live for all students!";
            }

            return RedirectToAction(nameof(Details), new { code });
        }

        // POST /Courses/UpvoteVideo/5
        [HttpPost]
        public async Task<IActionResult> UpvoteVideo(int videoId, string code)
        {
            var video = await _context.CourseVideos.FindAsync(videoId);
            if (video == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, requireAuth = true, message = "Please sign in to vote." });

                return RedirectToAction("Login", "Account");
            }

            var existingVote = await _context.Votes
                .FirstOrDefaultAsync(v => v.UserId == currentUserId && v.TargetType == VoteTargetType.Video && v.TargetId == videoId);

            bool hasVoted = false;
            if (existingVote != null)
            {
                // Undo vote
                _context.Votes.Remove(existingVote);
                video.UpVotes = Math.Max(0, video.UpVotes - 1);
                hasVoted = false;
            }
            else
            {
                // Add vote
                _context.Votes.Add(new Vote { UserId = currentUserId, TargetType = VoteTargetType.Video, TargetId = videoId, VoteValue = 1 });
                video.UpVotes++;
                hasVoted = true;

                // ── Notify Video Submitter ──
                if (!string.IsNullOrEmpty(video.SubmittedByUserId) && video.SubmittedByUserId != currentUserId)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = video.SubmittedByUserId,
                        Title = "New Upvote on your video! ⭐",
                        Message = $"u/{User.Identity?.Name ?? "A student"} upvoted your video \"{video.Title}\" in c/{code}",
                        LinkUrl = $"/Courses/Details?code={code}",
                        Type = NotificationType.System,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, upVotes = video.UpVotes, hasVoted });
            }

            return RedirectToAction(nameof(Details), new { code });
        }

        private async Task NotifyAdminsAsync(string title, string message, string linkUrl, NotificationType type = NotificationType.System)
        {
            try
            {
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole != null)
                {
                    var adminUserIds = await _context.UserRoles
                        .Where(ur => ur.RoleId == adminRole.Id)
                        .Select(ur => ur.UserId)
                        .ToListAsync();

                    foreach (var adminId in adminUserIds)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = adminId,
                            Title = title,
                            Message = message,
                            LinkUrl = linkUrl,
                            Type = type,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Notification Error] {ex.Message}");
            }
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
