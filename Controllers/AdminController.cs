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

            ViewBag.RecentReports = await _context.Reports
                .Include(r => r.ReportedByUser)
                .Include(r => r.Post)
                .Include(r => r.Comment)
                .Where(r => r.Status == ReportStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentPendingNotes = await _context.CourseResources
                .Include(r => r.Course)
                .Include(r => r.UploadedByUser)
                .Where(r => r.Type == CourseResourceType.Notes && r.Status == ResourceStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // GET: /Admin/Reports
        public async Task<IActionResult> Reports(ReportStatus? status = null)
        {
            ViewBag.SelectedStatus = status;

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
                TempData["SuccessMessage"] = "Report approved (action noted).";
            }
            else if (actionType == "reject")
            {
                report.Status = ReportStatus.Rejected;
                TempData["SuccessMessage"] = "Report rejected.";
            }
            else if (actionType == "delete_content")
            {
                report.Status = ReportStatus.Approved;
                if (report.PostId != null && report.Post != null)
                {
                    _context.Posts.Remove(report.Post);
                    TempData["SuccessMessage"] = "Report approved and reported post deleted successfully.";
                }
                else if (report.CommentId != null && report.Comment != null)
                {
                    _context.Comments.Remove(report.Comment);
                    TempData["SuccessMessage"] = "Report approved and reported comment deleted successfully.";
                }
            }
            else if (actionType == "ban_user")
            {
                report.Status = ReportStatus.Approved;
                string? targetUserId = report.Post?.UserId ?? report.Comment?.UserId;
                if (!string.IsNullOrEmpty(targetUserId))
                {
                    var userToBan = await _userManager.FindByIdAsync(targetUserId);
                    if (userToBan != null)
                    {
                        userToBan.IsBanned = true;
                        await _userManager.UpdateAsync(userToBan);
                        TempData["SuccessMessage"] = $"Report approved and user u/{userToBan.UserName} has been banned.";
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Reports));
        }

        // GET: /Admin/Notes
        public async Task<IActionResult> Notes(ResourceStatus? status = null)
        {
            ViewBag.SelectedStatus = status;

            var query = _context.CourseResources
                .Include(r => r.Course)
                .Include(r => r.UploadedByUser)
                .Where(r => r.Type == CourseResourceType.Notes)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var notes = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return View(notes);
        }

        // POST: /Admin/ApproveNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveNote(int noteId)
        {
            var note = await _context.CourseResources.FindAsync(noteId);
            if (note == null) return NotFound();

            note.Status = ResourceStatus.Approved;
            note.RejectionReason = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Note '{note.Title}' has been approved and is now publicly visible!";
            return RedirectToAction(nameof(Notes));
        }

        // POST: /Admin/RejectNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectNote(int noteId, string? rejectionReason)
        {
            var note = await _context.CourseResources.FindAsync(noteId);
            if (note == null) return NotFound();

            note.Status = ResourceStatus.Rejected;
            note.RejectionReason = string.IsNullOrWhiteSpace(rejectionReason) ? "Note did not meet quality guidelines." : rejectionReason.Trim();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Note '{note.Title}' rejected.";
            return RedirectToAction(nameof(Notes));
        }

        // GET: /Admin/Subpages
        public async Task<IActionResult> Subpages()
        {
            var courses = await _context.Courses
                .Include(c => c.CourseDepartments)
                    .ThenInclude(cd => cd.Department)
                .Include(c => c.Resources)
                .Include(c => c.Posts)
                .OrderBy(c => c.Code)
                .ToListAsync();

            return View(courses);
        }

        // GET: /Admin/CreateSubpage
        public async Task<IActionResult> CreateSubpage()
        {
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View();
        }

        // POST: /Admin/CreateSubpage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubpage(string code, string title, string description, bool isGeneral, int[]? departmentIds)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("", "Subject Code and Title are required.");
                ViewBag.Departments = await _context.Departments.ToListAsync();
                return View();
            }

            code = code.Trim().ToUpper();
            if (await _context.Courses.AnyAsync(c => c.Code == code))
            {
                ModelState.AddModelError("", $"Subpage with code '{code}' already exists.");
                ViewBag.Departments = await _context.Departments.ToListAsync();
                return View();
            }

            var course = new Course
            {
                Code = code,
                Title = title.Trim(),
                Description = description?.Trim() ?? string.Empty,
                IsGeneral = isGeneral,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            if (!isGeneral && departmentIds != null && departmentIds.Length > 0)
            {
                foreach (var deptId in departmentIds)
                {
                    _context.CourseDepartments.Add(new CourseDepartment { CourseId = course.Id, DepartmentId = deptId });
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Subpage c/{code} created successfully!";
            return RedirectToAction(nameof(Subpages));
        }

        // GET: /Admin/EditSubpage/5
        public async Task<IActionResult> EditSubpage(int id)
        {
            var course = await _context.Courses
                .Include(c => c.CourseDepartments)
                .Include(c => c.Resources)
                .Include(c => c.Videos)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(course);
        }

        // POST: /Admin/EditSubpage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubpage(int id, string title, string description, bool isGeneral, int[]? departmentIds)
        {
            var course = await _context.Courses
                .Include(c => c.CourseDepartments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            course.Title = title.Trim();
            course.Description = description?.Trim() ?? string.Empty;
            course.IsGeneral = isGeneral;

            _context.CourseDepartments.RemoveRange(course.CourseDepartments);
            if (!isGeneral && departmentIds != null && departmentIds.Length > 0)
            {
                foreach (var deptId in departmentIds)
                {
                    _context.CourseDepartments.Add(new CourseDepartment { CourseId = course.Id, DepartmentId = deptId });
                }
            }

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

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Subpage c/{course.Code} deleted successfully.";
            return RedirectToAction(nameof(Subpages));
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.OrderBy(u => u.UserName).ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userRoles[u.Id] = roles.FirstOrDefault() ?? "Student";
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // POST: /Admin/ToggleBanUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBanUser(string userId)
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

            TempData["SuccessMessage"] = user.IsBanned ? $"User u/{user.UserName} has been banned." : $"User u/{user.UserName} has been unbanned.";
            return RedirectToAction(nameof(Users));
        }
    }
}
