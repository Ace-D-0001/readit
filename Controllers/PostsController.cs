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

            // Fetch user vote state
            var userVotes = new Dictionary<int, int>();
            string? currentUserId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                userVotes = await _context.Votes
                    .Where(v => v.UserId == currentUserId && v.TargetType == VoteTargetType.Post)
                    .ToDictionaryAsync(v => v.TargetId, v => v.VoteValue);
            }
            ViewBag.UserVotes = userVotes;

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

            // Fetch course sidebar resources for context
            ViewBag.CourseResources = await _context.CourseResources
                .Where(r => r.CourseId == post.CourseId)
                .ToListAsync();

            // Vote states
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

            // Auto-add author's upvote in Vote table
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

        // POST: /Posts/Vote
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
                        // New vote
                        _context.Votes.Add(new Vote { UserId = currentUserId, TargetType = typeEnum, TargetId = targetId, VoteValue = voteVal });
                        if (voteVal == 1) post.UpVotes++; else post.DownVotes++;
                    }
                    else if (existingVote.VoteValue == voteVal)
                    {
                        // Undo vote
                        _context.Votes.Remove(existingVote);
                        if (voteVal == 1) post.UpVotes--; else post.DownVotes--;
                    }
                    else
                    {
                        // Swap vote (Up to Down or vice versa)
                        existingVote.VoteValue = voteVal;
                        if (voteVal == 1) { post.UpVotes++; post.DownVotes--; }
                        else { post.DownVotes++; post.UpVotes--; }
                    }
                    await _context.SaveChangesAsync();
                }
            }
            else if (typeEnum == VoteTargetType.Comment)
            {
                var comment = await _context.Comments.FindAsync(targetId);
                if (comment != null)
                {
                    if (existingVote == null)
                    {
                        _context.Votes.Add(new Vote { UserId = currentUserId, TargetType = typeEnum, TargetId = targetId, VoteValue = voteVal });
                        if (voteVal == 1) comment.UpVotes++; else comment.DownVotes++;
                    }
                    else if (existingVote.VoteValue == voteVal)
                    {
                        _context.Votes.Remove(existingVote);
                        if (voteVal == 1) comment.UpVotes--; else comment.DownVotes--;
                    }
                    else
                    {
                        existingVote.VoteValue = voteVal;
                        if (voteVal == 1) { comment.UpVotes++; comment.DownVotes--; }
                        else { comment.DownVotes++; comment.UpVotes--; }
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
                CreatedAt = DateTime.UtcNow,
                UpVotes = 1
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = postId });
        }

        private string? GetCurrentUserId()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var u = _context.Users.FirstOrDefault(usr => usr.UserName == User.Identity.Name);
                if (u != null) return u.Id;
            }
            // Fallback first user for seamless guest testing
            return _context.Users.Select(u => u.Id).FirstOrDefault();
        }
    }
}
