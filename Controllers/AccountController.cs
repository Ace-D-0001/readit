using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Read_It.Models;

namespace Read_It.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Email/Username and Password are required.");
                return View();
            }

            string identifier = email.Trim();
            var user = await _userManager.FindByEmailAsync(identifier) ?? await _userManager.FindByNameAsync(identifier);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt. Account not found.");
                return View();
            }

            if (user.IsBanned)
            {
                ModelState.AddModelError("", "Your account has been banned by an administrator.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, password.Trim(), isPersistent: true, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Index", "Admin");

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid password. Please check your credentials.");
            return View();
        }

        // GET: /Account/AdminLogin
        [HttpGet]
        public IActionResult AdminLogin(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User) && User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/AdminLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(string email, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Admin Email and Password are required.");
                return View();
            }

            string identifier = email.Trim();
            var user = await _userManager.FindByEmailAsync(identifier) ?? await _userManager.FindByNameAsync(identifier);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid admin credentials.");
                return View();
            }

            if (user.IsBanned)
            {
                ModelState.AddModelError("", "Account is banned.");
                return View();
            }

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                ModelState.AddModelError("", "Access Denied: Account does not have Admin privileges.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, password.Trim(), isPersistent: true, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Admin");
            }

            ModelState.AddModelError("", "Invalid admin credentials.");
            return View();
        }

        // GET: /Account/GoogleLogin (Mock Google OAuth for test/dev)
        [HttpGet]
        public async Task<IActionResult> GoogleLogin()
        {
            var studentUser = await _userManager.FindByEmailAsync("student@iubat.edu");
            if (studentUser != null)
            {
                if (studentUser.IsBanned)
                {
                    TempData["ErrorMessage"] = "Test Student account is banned.";
                    return RedirectToAction("Login");
                }
                await _signInManager.SignInAsync(studentUser, isPersistent: true);
                TempData["SuccessMessage"] = "Successfully signed in via Google OAuth (Test Account)!";
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Login");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string? bio)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }

            var newUser = new ApplicationUser
            {
                UserName = username.Trim(),
                Email = email.Trim(),
                Bio = bio?.Trim() ?? "IUBAT Community Student",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newUser, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Student");
                await _signInManager.SignInAsync(newUser, isPersistent: true);
                return RedirectToAction("Index", "Home");
            }

            foreach (var err in result.Errors)
            {
                ModelState.AddModelError("", err.Description);
            }
            return View();
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Please enter your email.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                ViewBag.Token = await _userManager.GeneratePasswordResetTokenAsync(user);
                ViewBag.Email = email;
                ViewBag.Message = "Password reset link generated for test mode!";
                return View("ResetPassword");
            }

            ViewBag.Message = "If that email exists in our system, a password reset link has been generated.";
            return View();
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string? email, string? token)
        {
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("", "Email and New Password are required.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Message = "Password successfully reset! You can now log in.";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(token))
            {
                token = await _userManager.GeneratePasswordResetTokenAsync(user);
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password reset successful! Please log in.";
                return RedirectToAction("Login");
            }

            foreach (var err in result.Errors)
            {
                ModelState.AddModelError("", err.Description);
            }
            return View();
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // POST: /Account/Logout
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Response.Cookies.Delete(".AspNetCore.Identity.Application");
            HttpContext.Response.Cookies.Delete(".AspNetCore.Antiforgery");
            TempData["SuccessMessage"] = "You have been signed out successfully.";
            return RedirectToAction("Index", "Home");
        }
    }
}
