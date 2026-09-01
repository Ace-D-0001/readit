using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Read_It.Data;
using Read_It.Models;

namespace Read_It.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalStudents = await _context.Users.CountAsync(u => u.UserName != "admin");
            ViewBag.PendingNotes = await _context.CourseResources.CountAsync(r => r.Type == CourseResourceType.Notes && r.Status == ResourceStatus.Pending);
            ViewBag.PendingReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending);
            ViewBag.TotalPosts = await _context.Posts.CountAsync();
            ViewBag.TotalComments = await _context.Comments.CountAsync();
            ViewBag.BannedUsers = await _context.Users.CountAsync(u => u.IsBanned);
            ViewBag.TotalCourses = await _context.Courses.CountAsync();
            ViewBag.TotalAnnouncements = await _context.Posts.CountAsync(p => p.Flair == PostFlair.Announcement);

            ViewBag.RecentReports = await _context.Reports
                .Include(r => r.ReportedByUser)
                .Include(r => r.Post)
                .Include(r => r.Comment)
                .Where(r => r.Status == ReportStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .Take(6)
                .ToListAsync();

            ViewBag.RecentPendingNotes = await _context.CourseResources
                .Include(r => r.Course)
                .Include(r => r.UploadedByUser)
                .Where(r => r.Type == CourseResourceType.Notes && r.Status == ResourceStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .Take(6)
                .ToListAsync();

            ViewBag.RecentAnnouncements = await _context.Posts
                .Include(p => p.Course)
                .Include(p => p.User)
                .Where(p => p.Flair == PostFlair.Announcement)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            return View();
        }

        // GET: /Admin/CreateAnnouncement
        public async Task<IActionResult> CreateAnnouncement(string? courseCode)
        {
            ViewBag.Courses = await _context.Courses.OrderBy(c => c.Code).ToListAsync();
            ViewBag.SelectedCourseCode = courseCode;
            return View();
        }

        // POST: /Admin/CreateAnnouncement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(int? courseId, string title, string body, bool isPinned = true, DateTime? expiresAt = null)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                ModelState.AddModelError("", "Title and content are required for an announcement.");
                ViewBag.Courses = await _context.Courses.OrderBy(c => c.Code).ToListAsync();
                return View();
            }

            var currentAdmin = await _userManager.GetUserAsync(User);
            if (currentAdmin == null) return Unauthorized();

            // Default to first course if none selected
            int targetCourseId = courseId ?? (await _context.Courses.Select(c => c.Id).FirstOrDefaultAsync());
            if (targetCourseId == 0)
            {
                ModelState.AddModelError("", "Please create at least one course subpage before posting announcements.");
                ViewBag.Courses = await _context.Courses.OrderBy(c => c.Code).ToListAsync();
                return View();
            }

            var course = await _context.Courses.Include(c => c.Followers).FirstOrDefaultAsync(c => c.Id == targetCourseId);

            var post = new Post
            {
                CourseId    = targetCourseId,
                UserId      = currentAdmin.Id,
                Title       = "[OFFICIAL ANNOUNCEMENT] " + title.Trim(),
                Body        = body.Trim(),
                Flair       = PostFlair.Announcement,
                CreatedAt   = DateTime.UtcNow,
                UpVotes     = 1,
                DownVotes   = 0,
                IsPinned    = isPinned,
                ExpiresAt   = expiresAt
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Notify Course Followers
            if (course?.Followers != null && course.Followers.Any())
            {
                foreach (var follower in course.Followers)
                {
                    if (follower.UserId != currentAdmin.Id)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = follower.UserId,
                            Title = $"📢 Official Announcement in c/{course.Code}",
                            Message = title.Trim(),
                            LinkUrl = $"/Posts/Details/{post.Id}",
                            Type = NotificationType.Announcement,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await LogActionAsync("Broadcast Announcement", $"c/{course?.Code ?? "General"} — \"{title.Trim()}\"", $"Pinned: {isPinned}, Expires: {(expiresAt.HasValue ? expiresAt.Value.ToString("yyyy-MM-dd") : "Never")}");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Official Announcement published and broadcasted successfully!";
            return RedirectToAction("Details", "Posts", new { id = post.Id });
        }

        // POST: /Admin/PinPost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PinPost(int postId, string? returnUrl)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound();

            post.IsPinned = !post.IsPinned;
            await _context.SaveChangesAsync();

            await LogActionAsync(post.IsPinned ? "Pin Post" : "Unpin Post", $"Post #{postId} — \"{post.Title}\"", null);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = post.IsPinned ? "Post has been pinned to the top of the feed." : "Post has been unpinned.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Details", "Posts", new { id = postId });
        }

        // POST: /Admin/DeletePost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int postId, string? returnUrl)
        {
            var post = await _context.Posts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return NotFound();

            string postTitle = post.Title;
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            await LogActionAsync("Delete Post", $"Post #{postId} — \"{postTitle}\"", "Post and comments removed by admin.");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Post deleted by administrator.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("/Posts/Details/"))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Posts");
        }

        // POST: /Admin/DeleteComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId, string? returnUrl)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();

            int postId = comment.PostId;
            string snippet = comment.Body.Length > 40 ? comment.Body.Substring(0, 40) + "..." : comment.Body;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            await LogActionAsync("Delete Comment", $"Comment #{commentId} on Post #{postId}", $"Snippet: \"{snippet}\"");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment removed by administrator.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Details", "Posts", new { id = postId });
        }

        // GET: /Admin/Reports
        public async Task<IActionResult> Reports(ReportStatus? status = null, string? target = null)
        {
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedTarget = target;

            var query = _context.Reports
                .Include(r => r.ReportedByUser)
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.Comment)
                    .ThenInclude(c => c.User)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(target))
            {
                if (target.Equals("post", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(r => r.TargetType == ReportTargetType.Post);
                else if (target.Equals("comment", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(r => r.TargetType == ReportTargetType.Comment);
            }

            var reports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return View(reports);
        }

        // POST: /Admin/ResolveReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int reportId, string actionType, string? adminNotes)
        {
            var report = await _context.Reports
                .Include(r => r.Post)
                .Include(r => r.Comment)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null) return NotFound();

            report.AdminNotes = adminNotes;
            report.ResolvedAt = DateTime.UtcNow;

            if (actionType == "approve")
            {
                report.Status = ReportStatus.Approved;
                await LogActionAsync("Approve Report", $"Report #{reportId} ({report.TargetType})", $"Reason: {report.Reason}");
                TempData["SuccessMessage"] = "Report approved and logged.";
            }
            else if (actionType == "reject")
            {
                report.Status = ReportStatus.Rejected;
                await LogActionAsync("Dismiss Report", $"Report #{reportId} ({report.TargetType})", $"Dismissed flag: {report.Reason}");
                TempData["SuccessMessage"] = "Report rejected and marked as dismissed.";
            }
            else if (actionType == "delete_content")
            {
                report.Status = ReportStatus.Approved;
                if (report.PostId != null && report.Post != null)
                {
                    string pTitle = report.Post.Title;
                    _context.Posts.Remove(report.Post);
                    await LogActionAsync("Report: Delete Post", $"Post #{report.PostId} — \"{pTitle}\"", $"Report reason: {report.Reason}");
                    TempData["SuccessMessage"] = "Report resolved: Reported post has been permanently removed.";
                }
                else if (report.CommentId != null && report.Comment != null)
                {
                    _context.Comments.Remove(report.Comment);
                    await LogActionAsync("Report: Delete Comment", $"Comment #{report.CommentId}", $"Report reason: {report.Reason}");
                    TempData["SuccessMessage"] = "Report resolved: Reported comment has been permanently removed.";
                }
            }
            else if (actionType == "ban_user")
            {
                report.Status = ReportStatus.Approved;
                string? targetUserId = report.Post?.UserId ?? report.Comment?.UserId;
                if (!string.IsNullOrEmpty(targetUserId))
                {
                    var userToBan = await _userManager.FindByIdAsync(targetUserId);
                    if (userToBan != null && userToBan.UserName != "admin")
                    {
                        userToBan.IsBanned = true;
                        await _userManager.UpdateAsync(userToBan);

                        if (report.Post != null) _context.Posts.Remove(report.Post);
                        else if (report.Comment != null) _context.Comments.Remove(report.Comment);

                        await LogActionAsync("Report: Ban User", $"u/{userToBan.UserName}", $"Banned from report #{reportId}. Reason: {report.Reason}");
                        TempData["SuccessMessage"] = $"Report resolved: u/{userToBan.UserName} has been banned and content deleted.";
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Reports));
        }

        // GET: /Admin/Notes
        public async Task<IActionResult> Notes(ResourceStatus? status = null, string? search = null)
        {
            ViewBag.SelectedStatus = status;
            ViewBag.SearchQuery = search;

            var query = _context.CourseResources
                .Include(r => r.Course)
                .Include(r => r.UploadedByUser)
                .Where(r => r.Type == CourseResourceType.Notes)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.Title.Contains(search) || (r.Course != null && r.Course.Code.Contains(search)));
            }

            var notes = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return View(notes);
        }

        // POST: /Admin/ApproveNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveNote(int noteId)
        {
            var note = await _context.CourseResources.Include(r => r.Course).FirstOrDefaultAsync(r => r.Id == noteId);
            if (note == null) return NotFound();

            note.Status = ResourceStatus.Approved;
            note.RejectionReason = null;

            // Notify Uploader
            if (!string.IsNullOrEmpty(note.UploadedByUserId))
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = note.UploadedByUserId,
                    Title = "Study Note Approved! 📚",
                    Message = $"Your uploaded note \"{note.Title}\" has been approved and published to c/{note.Course?.Code ?? "General"}.",
                    LinkUrl = $"/Courses/Details?code={note.Course?.Code}#notes",
                    Type = NotificationType.NoteApproved,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await LogActionAsync("Approve Note", $"Note #{noteId} — \"{note.Title}\"", $"Course: c/{note.Course?.Code}");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Note '{note.Title}' approved and published to public subpage.";
            return RedirectToAction(nameof(Notes));
        }

        // POST: /Admin/RejectNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectNote(int noteId, string? rejectionReason)
        {
            var note = await _context.CourseResources.Include(r => r.Course).FirstOrDefaultAsync(r => r.Id == noteId);
            if (note == null) return NotFound();

            string feedback = string.IsNullOrWhiteSpace(rejectionReason) ? "Does not meet course quality standards." : rejectionReason.Trim();
            note.Status = ResourceStatus.Rejected;
            note.RejectionReason = feedback;

            // Notify Uploader
            if (!string.IsNullOrEmpty(note.UploadedByUserId))
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = note.UploadedByUserId,
                    Title = "Study Note Update",
                    Message = $"Your uploaded note \"{note.Title}\" was not approved. Feedback: {feedback}",
                    LinkUrl = $"/Courses/Details?code={note.Course?.Code}#notes",
                    Type = NotificationType.NoteRejected,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await LogActionAsync("Reject Note", $"Note #{noteId} — \"{note.Title}\"", $"Feedback: {feedback}");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Note '{note.Title}' rejected.";
            return RedirectToAction(nameof(Notes));
        }

        // GET: /Admin/Subpages
        public async Task<IActionResult> Subpages(string? search = null)
        {
            ViewBag.SearchQuery = search;

            var query = _context.Courses
                .Include(c => c.Resources)
                .Include(c => c.Posts)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Code.Contains(search) || c.Title.Contains(search));
            }

            var courses = await query.OrderBy(c => c.Code).ToListAsync();
            return View(courses);
        }

        // GET: /Admin/CreateSubpage
        public IActionResult CreateSubpage()
        {
            return View();
        }

        // POST: /Admin/CreateSubpage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubpage(string code, string title, string description)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("", "Subject Code and Title are required.");
                return View();
            }

            code = code.Trim().ToUpper();
            if (await _context.Courses.AnyAsync(c => c.Code == code))
            {
                ModelState.AddModelError("", $"Subpage with code '{code}' already exists.");
                return View();
            }

            var course = new Course
            {
                Code = code,
                Title = title.Trim(),
                Description = description?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            await LogActionAsync("Create Subpage", $"c/{code} — \"{title.Trim()}\"", null);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Subpage c/{code} created successfully!";
            return RedirectToAction(nameof(Subpages));
        }

        // GET: /Admin/EditSubpage/5
        public async Task<IActionResult> EditSubpage(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Resources)
                .Include(c => c.Videos)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // POST: /Admin/EditSubpage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubpage(int id, string title, string description)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            course.Title = title.Trim();
            course.Description = description?.Trim() ?? string.Empty;

            await LogActionAsync("Edit Subpage", $"c/{course.Code} — \"{title.Trim()}\"", null);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Subpage c/{course.Code} updated successfully!";
            return RedirectToAction(nameof(Subpages));
        }

        // POST: /Admin/DeleteSubpage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubpage(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            string code = course.Code;
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            await LogActionAsync("Delete Subpage", $"c/{code}", "Subpage and all materials removed.");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Subpage c/{code} deleted successfully.";
            return RedirectToAction(nameof(Subpages));
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users(string? search = null, string? status = null, string? role = null)
        {
            ViewBag.SearchQuery = search;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedRole = role;

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.UserName.Contains(search) || (u.Email != null && u.Email.Contains(search)));
            }

            if (status == "banned")
            {
                query = query.Where(u => u.IsBanned);
            }
            else if (status == "active")
            {
                query = query.Where(u => !u.IsBanned);
            }

            var users = await query.OrderBy(u => u.UserName).ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userRoles[u.Id] = roles.FirstOrDefault() ?? "Student";
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                users = users.Where(u => userRoles.ContainsKey(u.Id) && userRoles[u.Id].Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // POST: /Admin/ToggleBanUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBanUser(string userId, string? reason)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (user.UserName == "admin")
            {
                TempData["ErrorMessage"] = "Cannot ban primary admin account.";
                return RedirectToAction(nameof(Users));
            }

            user.IsBanned = !user.IsBanned;
            await _userManager.UpdateAsync(user);

            string logReason = string.IsNullOrWhiteSpace(reason) ? "Violation of community rules" : reason;
            await LogActionAsync(user.IsBanned ? "Ban User" : "Unban User", $"u/{user.UserName}", $"Reason: {logReason}");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = user.IsBanned 
                ? $"User u/{user.UserName} has been banned. Reason: {logReason}" 
                : $"User u/{user.UserName} has been unbanned.";

            return RedirectToAction(nameof(Users));
        }

        // POST: /Admin/WarnUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WarnUser(string userId, string reason)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentAdmin = await _userManager.GetUserAsync(User);
            string warnReason = string.IsNullOrWhiteSpace(reason) ? "Official Warning: Please adhere to IUBAT Community Guidelines." : reason.Trim();

            var warning = new UserWarning
            {
                UserId = userId,
                AdminUserId = currentAdmin?.Id ?? "admin",
                AdminUserName = currentAdmin?.UserName ?? "admin",
                Reason = warnReason,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserWarnings.Add(warning);

            // Send notification
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "⚠️ Formal Community Warning",
                Message = warnReason,
                LinkUrl = "/User/Details",
                Type = NotificationType.Warning,
                CreatedAt = DateTime.UtcNow
            });

            await LogActionAsync("Warn User", $"u/{user.UserName}", $"Warning issued: \"{warnReason}\"");
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Formal warning issued to u/{user.UserName}.";
            return RedirectToAction(nameof(Users));
        }

        // GET: /Admin/AuditLogs
        public async Task<IActionResult> AuditLogs(string? search = null, string? actionType = null)
        {
            ViewBag.SearchQuery = search;
            ViewBag.SelectedActionType = actionType;

            var query = _context.AdminLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l => l.TargetDescription.Contains(search) || (l.Details != null && l.Details.Contains(search)) || l.AdminUserName.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query = query.Where(l => l.ActionType == actionType);
            }

            var logs = await query.OrderByDescending(l => l.CreatedAt).Take(100).ToListAsync();
            return View(logs);
        }

        private async Task LogActionAsync(string actionType, string target, string? details)
        {
            var currentAdmin = await _userManager.GetUserAsync(User);
            _context.AdminLogs.Add(new AdminLog
            {
                AdminUserId = currentAdmin?.Id ?? "admin",
                AdminUserName = currentAdmin?.UserName ?? "admin",
                ActionType = actionType,
                TargetDescription = target,
                Details = details,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
