using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RVSite.Controllers
{
    public class LoginController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public LoginController(
            AppDbContext db,
            IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        // GET: /Login
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            var normalizedEmail = email.Trim().ToLower();

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            if (user.IsLocked)
            {
                ViewBag.Error = "This account is currently locked.";
                return View();
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

            /*
             * Temporary compatibility for existing development accounts that
             * were saved as plain text before password hashing was introduced.
             * When one successfully signs in, its password is upgraded to a hash.
             */
            var legacyPasswordMatches = user.PasswordHash == password;

            if (verificationResult == PasswordVerificationResult.Failed &&
                !legacyPasswordMatches)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            if (legacyPasswordMatches ||
                verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                await _db.SaveChangesAsync();
            }

            await SignInUser(user);

            if (user.Role?.Type == RoleType.Admin ||
                user.Role?.Type == RoleType.Staff)
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Login/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Login/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            string password,
            string confirmPassword)
        {
            firstName = firstName?.Trim() ?? "";
            lastName = lastName?.Trim() ?? "";
            email = email?.Trim().ToLower() ?? "";
            phoneNumber = phoneNumber?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(firstName))
                ModelState.AddModelError("firstName", "First name is required.");

            if (string.IsNullOrWhiteSpace(lastName))
                ModelState.AddModelError("lastName", "Last name is required.");

            if (string.IsNullOrWhiteSpace(email))
                ModelState.AddModelError("email", "Email is required.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                ModelState.AddModelError("phoneNumber", "Phone number is required.");

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "Password is required.");

            if (password != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Passwords do not match.");

            if (!string.IsNullOrWhiteSpace(password) && password.Length < 8)
            {
                ModelState.AddModelError(
                    "password",
                    "Password must contain at least 8 characters.");
            }

            var emailExists = await _db.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "email",
                    "An account with this email address already exists.");
            }

            var customerRole = await _db.Role
                .FirstOrDefaultAsync(r => r.Type == RoleType.Customer);

            if (customerRole == null)
            {
                ModelState.AddModelError(
                    "",
                    "Customer registration is temporarily unavailable because the customer role is not configured.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.FirstName = firstName;
                ViewBag.LastName = lastName;
                ViewBag.Email = email;
                ViewBag.PhoneNumber = phoneNumber;

                return View();
            }

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                RoleID = customerRole!.RoleID,
                IsLocked = false,
                PasswordHash = ""
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            user.Role = customerRole;

            await SignInUser(user);

            return RedirectToAction("Index", "Home");
        }

        // POST: /Login/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(
                    ClaimTypes.Role,
                    user.Role?.Type.ToString() ?? RoleType.Customer.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);
        }
    }
}