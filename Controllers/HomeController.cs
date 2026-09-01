using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Read_It.Data;
using Read_It.Models;

namespace Read_It.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /?sort=hot|top|new&feed=all|my
        public async Task<IActionResult> Index(string sort = "hot", string feed = "all")
        {
            ViewBag.ActiveSort = sort.ToLower();
            ViewBag.ActiveFeed = feed.ToLower();

            string? currentUserId = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                if (user != null) currentUserId = user.Id;
            }

            var query = _context.Posts
                .Include(p => p.Course)
                .Include(p => p.User)
                .Include(p => p.Comments)
                .AsQueryable();

            if (feed.ToLower() == "my" && !string.IsNullOrEmpty(currentUserId))
            {
                var followedCourseIds = await _context.CourseFollows
                    .Where(cf => cf.UserId == currentUserId)
                    .Select(cf => cf.CourseId)
                    .ToListAsync();

                query = query.Where(p => followedCourseIds.Contains(p.CourseId));
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
            else // "hot" (default) — sort by net votes desc then recency
            {
                posts = await query.ToListAsync();
                posts = posts
                    .OrderByDescending(p => p.IsPinned)
                    .ThenByDescending(p => (double)(p.UpVotes - p.DownVotes + 1) / Math.Pow((DateTime.UtcNow - p.CreatedAt).TotalHours + 2.0, 1.2))
                    .ToList();
            }

            // User vote states dictionary for highlighting active upvote/downvote arrows
            var userVotes = new Dictionary<int, int>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var votes = await _context.Votes
                    .Where(v => v.UserId == currentUserId && v.TargetType == VoteTargetType.Post)
                    .ToListAsync();
                userVotes = votes.ToDictionary(v => v.TargetId, v => v.VoteValue);
            }
            ViewBag.UserVotes = userVotes;
            ViewBag.CurrentUserId = currentUserId;

            return View(posts);
        }

        public IActionResult StyleGuide()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
