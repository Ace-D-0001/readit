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
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /User/Details?username=tasmia_cse
        public async Task<IActionResult> Details(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                if (User?.Identity?.IsAuthenticated == true)
                {
                    username = User.Identity.Name;
                }
                else
                {
                    username = await _context.Users.Select(u => u.UserName).FirstOrDefaultAsync();
                }
            }

            if (string.IsNullOrEmpty(username)) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null) return NotFound();

            // Check if current user is viewing their own profile
            bool isOwnProfile = false;
            string? currentUserId = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                if (currentUser != null)
                {
                    currentUserId = currentUser.Id;
                    if (currentUser.Id == user.Id) isOwnProfile = true;
                }
            }
            else
            {
                // Guest demo mode: treat first user as logged in
                var firstUser = await _context.Users.FirstOrDefaultAsync();
                if (firstUser != null && firstUser.Id == user.Id) isOwnProfile = true;
            }

            ViewBag.IsOwnProfile = isOwnProfile;

            // Load user's posts
            var posts = await _context.Posts
                .Include(p => p.Course)
                .Include(p => p.Comments)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Load user's comments
            var comments = await _context.Comments
                .Include(c => c.Post)
                    .ThenInclude(p => p.Course)
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Load followed courses
            var followedCourses = await _context.CourseFollows
                .Include(cf => cf.Course)
                .Where(cf => cf.UserId == user.Id && cf.Course != null)
                .Select(cf => cf.Course!)
                .ToListAsync();

            // Calculate total karma
            int totalPostVotes = posts.Sum(p => p.UpVotes - p.DownVotes);
            int totalCommentVotes = comments.Sum(c => c.UpVotes - c.DownVotes);

            ViewBag.Posts = posts;
            ViewBag.Comments = comments;
            ViewBag.FollowedCourses = followedCourses;
            ViewBag.TotalKarma = totalPostVotes + totalCommentVotes;

            return View(user);
        }

        // POST: /User/UpdateBio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBio(string username, string bio)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null || currentUser == null || currentUser.Id != user.Id)
                return Forbid();

            user.Bio = bio?.Trim() ?? "";
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { username });
        }
    }
}
