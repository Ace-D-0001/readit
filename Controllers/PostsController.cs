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
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Posts?search=kw&flair=N&sort=hot|top|new
        public async Task<IActionResult> Index(string? search, PostFlair? flair, string sort = "hot")
        {
            ViewData["SearchQuery"] = search;
            ViewData["SelectedFlair"] = flair;
            ViewBag.ActiveSort = sort.ToLower();

            var query = _context.Posts
                .Include(p => p.Course)
                .Include(p => p.User)
                .Include(p => p.Comments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Title.Contains(search) || p.Body.Contains(search));
            }

            if (flair.HasValue)
            {
                query = query.Where(p => p.Flair == flair.Value);
            }

            List<Post> posts;
            if (sort.ToLower() == "top")
            {
                posts = await query.OrderByDescending(p => p.UpVotes - p.DownVotes).ToListAsync();
            }
            else if (sort.ToLower() == "new")
            {
                posts = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            }
            else // "hot" default
            {
                posts = await query.ToListAsync();
                posts = posts
                    .OrderByDescending(p => p.IsPinned)
                    .ThenByDescending(p => (double)(p.UpVotes - p.DownVotes + 1) / Math.Pow((DateTime.UtcNow - p.CreatedAt).TotalHours + 2.0, 1.2))
                    .ToList();
            }

            var userVotes = new Dictionary<int, int>();
            string? currentUserId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                userVotes = await _context.Votes
                    .Where(v => v.UserId == currentUserId && v.TargetType == VoteTargetType.Post)
                    .ToDictionaryAsync(v => v.TargetId, v => v.VoteValue);
            }
            ViewBag.UserVotes = userVotes;
            ViewBag.CurrentUserId = currentUserId;

            return View(posts);
        }

        // GET: /Posts/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Course)
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.ChildComments)
                        .ThenInclude(cc => cc.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            ViewBag.CourseResources = await _context.CourseResources
                .Where(r => r.CourseId == post.CourseId)
                .ToListAsync();

            string? currentUserId = GetCurrentUserId();
            int userPostVote = 0;
            var userCommentVotes = new Dictionary<int, int>();

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var pVote = await _context.Votes.FirstOrDefaultAsync(v => v.UserId == currentUserId && v.TargetType == VoteTargetType.Post && v.TargetId == id);
                if (pVote != null) userPostVote = pVote.VoteValue;

                var cVotes = await _context.Votes.Where(v => v.UserId == currentUserId && v.TargetType == VoteTargetType.Comment).ToListAsync();
                userCommentVotes = cVotes.ToDictionary(v => v.TargetId, v => v.VoteValue);
            }
            ViewBag.UserPostVote = userPostVote;
            ViewBag.UserCommentVotes = userCommentVotes;
            ViewBag.CurrentUserId = currentUserId;

            return View(post);
        }

        // GET: /Posts/Create?courseCode=CSC391
        public async Task<IActionResult> Create(string? courseCode)
        {
            var courses = await _context.Courses.OrderBy(c => c.Code).ToListAsync();
            ViewBag.Courses = courses;
            ViewBag.SelectedCourseCode = courseCode;
            return View();
        }

        // POST: /Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseId, string title, string body, PostFlair flair)
        {
            if (courseId <= 0 || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                ViewBag.Courses = await _context.Courses.OrderBy(c => c.Code).ToListAsync();
                ViewBag.Error = "Please select a course and fill in both the title and body.";
                return View();
            }

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var post = new Post
            {
                CourseId  = courseId,
                UserId    = currentUserId,
                Title     = title.Trim(),
                Body      = body.Trim(),
                Flair     = flair,
                CreatedAt = DateTime.UtcNow,
                UpVotes   = 1,
                DownVotes = 0,
                IsPinned  = false
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            _context.Votes.Add(new Vote
            {
                UserId = currentUserId,
                TargetType = VoteTargetType.Post,
                TargetId = post.Id,
                VoteValue = 1
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = post.Id });
        }

        // GET: /Posts/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts.Include(p => p.Course).FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (post.UserId != currentUserId && User?.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            ViewBag.Courses = await _context.Courses.OrderBy(c => c.Code).ToListAsync();
            return View(post);
        }

        // POST: /Posts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int courseId, string title, string body, PostFlair flair)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (post.UserId != currentUserId && User?.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            if (courseId <= 0 || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                ViewBag.Courses = await _context.Courses.OrderBy(c => c.Code).ToListAsync();
                ViewBag.Error = "Please fill in all fields.";
                return View(post);
            }

            post.CourseId = courseId;
            post.Title    = title.Trim();
            post.Body     = body.Trim();
            post.Flair    = flair;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = post.Id });
        }

        // POST: /Posts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                var currentUserId = GetCurrentUserId();
                if (post.UserId == currentUserId || User?.Identity?.IsAuthenticated != true)
                {
                    _context.Posts.Remove(post);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Posts/VoteApi (AJAX — No Page Reload!)
        [HttpPost]
        public async Task<IActionResult> VoteApi([FromBody] VoteRequestModel request)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            VoteTargetType typeEnum = request.TargetType.ToLower() == "comment" ? VoteTargetType.Comment : VoteTargetType.Post;
            int voteVal = request.Direction > 0 ? 1 : -1;
            int newScore = 0;
            int userVote = 0;

            var existingVote = await _context.Votes
                .FirstOrDefaultAsync(v => v.UserId == currentUserId && v.TargetType == typeEnum && v.TargetId == request.TargetId);

            if (typeEnum == VoteTargetType.Post)
            {
                var post = await _context.Posts.FindAsync(request.TargetId);
                if (post != null)
                {
                    if (existingVote == null)
                    {
                        _context.Votes.Add(new Vote { UserId = currentUserId, TargetType = typeEnum, TargetId = request.TargetId, VoteValue = voteVal });
                        if (voteVal == 1) post.UpVotes++; else post.DownVotes++;
                        userVote = voteVal;
                    }
                    else if (existingVote.VoteValue == voteVal)
                    {
                        _context.Votes.Remove(existingVote);
                        if (voteVal == 1) post.UpVotes--; else post.DownVotes--;
                        userVote = 0;
                    }
                    else
                    {
                        existingVote.VoteValue = voteVal;
                        if (voteVal == 1) { post.UpVotes++; post.DownVotes--; }
                        else { post.DownVotes++; post.UpVotes--; }
                        userVote = voteVal;
                    }
                    await _context.SaveChangesAsync();
                    newScore = post.UpVotes - post.DownVotes;
                }
            }
            else if (typeEnum == VoteTargetType.Comment)
            {
                var comment = await _context.Comments.FindAsync(request.TargetId);
                if (comment != null)
                {
                    if (existingVote == null)
                    {
                        _context.Votes.Add(new Vote { UserId = currentUserId, TargetType = typeEnum, TargetId = request.TargetId, VoteValue = voteVal });
                        if (voteVal == 1) comment.UpVotes++; else comment.DownVotes++;
                        userVote = voteVal;
                    }
                    else if (existingVote.VoteValue == voteVal)
                    {
                        _context.Votes.Remove(existingVote);
                        if (voteVal == 1) comment.UpVotes--; else comment.DownVotes--;
                        userVote = 0;
                    }
                    else
                    {
                        existingVote.VoteValue = voteVal;
                        if (voteVal == 1) { comment.UpVotes++; comment.DownVotes--; }
                        else { comment.DownVotes++; comment.UpVotes--; }
                        userVote = voteVal;
                    }
                    await _context.SaveChangesAsync();
                    newScore = comment.UpVotes - comment.DownVotes;
                }
            }

            return Json(new { success = true, score = newScore, userVote = userVote });
        }

        // POST: /Posts/Vote (Fallback HTML Form)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vote(int targetId, string targetType, int direction, string? returnUrl)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            VoteTargetType typeEnum = targetType.ToLower() == "comment" ? VoteTargetType.Comment : VoteTargetType.Post;
            int voteVal = direction > 0 ? 1 : -1;

            var existingVote = await _context.Votes
                .FirstOrDefaultAsync(v => v.UserId == currentUserId && v.TargetType == typeEnum && v.TargetId == targetId);

            if (typeEnum == VoteTargetType.Post)
            {
                var post = await _context.Posts.FindAsync(targetId);
                if (post != null)
                {
                    if (existingVote == null)
                    {
                        _context.Votes.Add(new Vote { UserId = currentUserId, TargetType = typeEnum, TargetId = targetId, VoteValue = voteVal });
                        if (voteVal == 1) post.UpVotes++; else post.DownVotes++;
                    }
                    else if (existingVote.VoteValue == voteVal)
                    {
                        _context.Votes.Remove(existingVote);
                        if (voteVal == 1) post.UpVotes--; else post.DownVotes--;
                    }
                    else
                    {
                        existingVote.VoteValue = voteVal;
                        if (voteVal == 1) { post.UpVotes++; post.DownVotes--; }
                        else { post.DownVotes++; post.UpVotes--; }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Posts/CreateComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComment(int postId, int? parentCommentId, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return RedirectToAction(nameof(Details), new { id = postId });
            }

            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId)) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var comment = new Comment
            {
                PostId = postId,
                ParentCommentId = parentCommentId,
                UserId = currentUserId,
                Body = body.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = postId });
        }

        // POST: /Posts/ReportPost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportPost(int postId, string reason)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound();

            var report = new Report
            {
                TargetType = ReportTargetType.Post,
                PostId = postId,
                ReportedByUserId = currentUserId,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Inappropriate Content" : reason.Trim(),
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you! Your report has been submitted to the admin for review.";
            return RedirectToAction(nameof(Details), new { id = postId });
        }

        // POST: /Posts/ReportComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportComment(int commentId, string reason)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();

            var report = new Report
            {
                TargetType = ReportTargetType.Comment,
                CommentId = commentId,
                ReportedByUserId = currentUserId,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Inappropriate Content" : reason.Trim(),
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you! Your comment report has been submitted to the admin.";
            return RedirectToAction(nameof(Details), new { id = comment.PostId });
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

    public class VoteRequestModel
    {
        public int TargetId { get; set; }
        public string TargetType { get; set; } = "post";
        public int Direction { get; set; }
    }
}
