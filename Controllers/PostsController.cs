using System;
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

        // GET: /Posts or /Posts/Index
        public async Task<IActionResult> Index(string? search, PostFlair? flair)
        {
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

            var posts = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            ViewData["SearchQuery"] = search;
            ViewData["SelectedFlair"] = flair;

            return View(posts);
        }

        // GET: /Posts/Details/5 (Zoom-in Post View)
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

            if (post == null)
            {
                return NotFound();
            }

            // Fetch course sidebar resources for context
            ViewBag.CourseResources = await _context.CourseResources
                .Where(r => r.CourseId == post.CourseId)
                .ToListAsync();

            return View(post);
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
            if (post == null)
            {
                return NotFound();
            }

            // For demonstration / testing without auth enforcement, fallback to first user if not logged in
            var currentUserId = _context.Users.Select(u => u.Id).FirstOrDefault();
            if (User?.Identity?.IsAuthenticated == true)
            {
                var loggedInUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                if (loggedInUser != null)
                {
                    currentUserId = loggedInUser.Id;
                }
            }

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction(nameof(Details), new { id = postId });
            }

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
    }
}
