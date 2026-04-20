using BagsWebsite.Models;
using BagsWebsite.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace BagsWebsite.Controllers
{
    public class AccountController : Controller
    {
        private readonly BagDbContext _context;

        public AccountController(BagDbContext context)
        {
            _context = context;
        }

        // --- REGISTRATION & LOGIN (Aapka purana code theek hai) ---
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
                int defaultRoleId = customerRole != null ? customerRole.Id : 2;

                var newUser = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    CreatedAt = DateTime.Now,
                    RoleId = defaultRoleId
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return RedirectToAction("Login");
            }
            return View(model);
        }
        // 1. Signup OTP Bhejna
        [HttpPost]
        public async Task<IActionResult> SendSignupOTP(string email)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == email);
            if (exists) return Json(new { success = false, message = "Email already registered!" });

            string otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("SignupOTP", otp);

            try
            {
                var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("mushtaqmuskan72@gmail.com", "tkrg algd wizf eucw"),
                    EnableSsl = true,
                };
                smtp.Send("mushtaqmuskan72@gmail.com", email, "Verify Your Registration", $"Your Signup OTP is: {otp}");
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false, message = "Could not send email. Use a real email address." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> FinalRegister(User model, string userOTP)
        {
            string sessionOTP = HttpContext.Session.GetString("SignupOTP");

            if (sessionOTP != null && sessionOTP == userOTP)
            {
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                model.CreatedAt = DateTime.Now;
                model.RoleId = (await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer"))?.Id ?? 2;

                _context.Users.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Invalid or expired OTP." });
        }
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
  
        public async Task<IActionResult> Login(string email, string password)
        {
            // 1. User dhoondein
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // --- LOGGING LOGIC START ---
            var loginLog = new LoginLog
            {
                UserEmail = email,
                LoginTime = DateTime.Now,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Browser = Request.Headers["User-Agent"].ToString()
            };
            // --- LOGGING LOGIC END ---

            if (user != null)
            {
                // 2. Password check karein
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

                if (isPasswordValid)
                {
                    // --- Success Logging ---
                    loginLog.UserId = user.Id;
                    loginLog.Status = "Success";

                    var role = await _context.Roles.FindAsync(user.RoleId);

                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                    if (role != null) HttpContext.Session.SetString("UserRole", role.Name);

                    // Log save karein
                    _context.LoginLogs.Add(loginLog);
                    await _context.SaveChangesAsync();

                    if (role?.Name == "Admin")
                    {
                        return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });
                    }
                    return RedirectToAction("Index", "Home");
                }
            }

            // --- Failure Logging ---
            loginLog.Status = "Failed"; // Wrong password ya email nahi mila
            _context.LoginLogs.Add(loginLog);
            await _context.SaveChangesAsync();

            ViewBag.Error = "Invalid Email or Password. Please try again.";
            return View();
        }
        // --- FORGOT PASSWORD (AJAX FLOW) ---

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        // 1. Send OTP
        [HttpPost]
        public async Task<IActionResult> SendOTP(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return Json(new { success = false, message = "Email not found!" });

            string otp = new Random().Next(100000, 999999).ToString();
            user.ResetCode = otp;
            await _context.SaveChangesAsync();

            try
            {
                using (var smtp = new SmtpClient("smtp.gmail.com"))
                {
                    smtp.Port = 587;
                    smtp.Credentials = new NetworkCredential("mushtaqmuskan72@gmail.com", "tkrg algd wizf eucw");
                    smtp.EnableSsl = true;

                    var mail = new MailMessage("mushtaqmuskan72@gmail.com", email, "Password Reset Code", $"Your code is: {otp}");
                    await smtp.SendMailAsync(mail);
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Email failed: " + ex.Message });
            }
        }

        // 2. Final Update
        [HttpPost]
        public async Task<IActionResult> UpdatePassword(string email, string otp, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.ResetCode == otp);
            if (user != null)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.ResetCode = null;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Invalid OTP or Email." });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}