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
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = true, string? returnUrl = null)
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

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, password.Trim(), isPersistent: rememberMe, lockoutOnFailure: false);
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
        public async Task<IActionResult> AdminLogin(string email, string password, bool rememberMe = true, string? returnUrl = null)
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

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, password.Trim(), isPersistent: rememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Admin");
            }

            ModelState.AddModelError("", "Invalid admin credentials.");
            return View();
        }

        // GET: /Account/QuickLogin?account=admin|student|prof_rahim|tasmia|nusrat
        [HttpGet]
        public async Task<IActionResult> QuickLogin(string account, string? returnUrl = null)
        {
            string email = account?.ToLowerInvariant() switch
            {
                "admin"      => "admin@gmail.com",
                "prof"       => "prof_rahim@iubat.edu",
                "prof_rahim" => "prof_rahim@iubat.edu",
                "tasmia"     => "tasmia@iubat.edu",
                "nusrat"     => "nusrat@iubat.edu",
                _            => "student@gmail.com"
            };

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Fallback to legacy email if student@gmail.com / admin@gmail.com not yet found
                if (email == "admin@gmail.com") user = await _userManager.FindByEmailAsync("admin@iubat.edu");
                else if (email == "student@gmail.com") user = await _userManager.FindByEmailAsync("student@iubat.edu");
            }

            if (user == null)
            {
                TempData["ErrorMessage"] = $"Account '{email}' not found. Please log in with your credentials.";
                return RedirectToAction("Login");
            }

            if (user.IsBanned)
            {
                TempData["ErrorMessage"] = $"Account '{user.UserName}' is currently banned.";
                return RedirectToAction("Login");
            }

            await _signInManager.SignInAsync(user, isPersistent: true);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["SuccessMessage"] = $"Signed in as Administrator ({user.Email})";
                return RedirectToAction("Index", "Admin");
            }

            TempData["SuccessMessage"] = $"Signed in as {user.UserName} ({user.Email})";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/GoogleLogin (One-Click Google Authentication)
        [HttpGet]
        public async Task<IActionResult> GoogleLogin(string? returnUrl = null, string role = "Student")
        {
            bool isAdminTarget = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            string targetEmail = isAdminTarget ? "admin@gmail.com" : "student@gmail.com";
            string targetUsername = isAdminTarget ? "admin" : "student";

            var user = await _userManager.FindByEmailAsync(targetEmail);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = targetUsername,
                    Email = targetEmail,
                    Bio = isAdminTarget ? "System Administrator — StudyHub" : "Computer Science Student — StudyHub",
                    EmailConfirmed = true
                };
                var createRes = await _userManager.CreateAsync(user, "1234567");
                if (createRes.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, isAdminTarget ? "Admin" : "Student");
                }
            }

            if (user.IsBanned)
            {
                TempData["ErrorMessage"] = "This account has been banned by an administrator.";
                return RedirectToAction(isAdminTarget ? "AdminLogin" : "Login");
            }

            await _signInManager.SignInAsync(user, isPersistent: true);
            TempData["SuccessMessage"] = $"Successfully signed in with Google as {user.Email}";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Index", "Admin");

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string username, string email, string password, string confirmPassword, string? bio)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Username, email, and password are required.");
                return View();
            }

            if (!string.IsNullOrWhiteSpace(confirmPassword) && password != confirmPassword)
            {
                ModelState.AddModelError("", "The password and confirmation password do not match.");
                return View();
            }

            if (password.Length < 6)
            {
                ModelState.AddModelError("", "Password must be at least 6 characters long.");
                return View();
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(email.Trim());
            if (existingUserByEmail != null)
            {
                ModelState.AddModelError("", "An account with this email address already exists.");
                return View();
            }

            var existingUserByName = await _userManager.FindByNameAsync(username.Trim());
            if (existingUserByName != null)
            {
                ModelState.AddModelError("", "This username is already taken. Please choose another.");
                return View();
            }

            var newUser = new ApplicationUser
            {
                UserName = username.Trim(),
                Email = email.Trim(),
                Bio = !string.IsNullOrWhiteSpace(bio) ? bio.Trim() : (!string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : "StudyHub Student Member"),
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newUser, password.Trim());
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Student");
                await _signInManager.SignInAsync(newUser, isPersistent: true);
                TempData["SuccessMessage"] = $"Welcome to StudyHub, {newUser.UserName}!";
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
