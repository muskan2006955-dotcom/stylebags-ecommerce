using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BagsWebsite.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BagsWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly BagDbContext _context;

        public UsersController(BagDbContext context)
        {
            _context = context;
        }

        // --- USERS LIST ---
        public async Task<IActionResult> Index()
        {
            // Security Check: Sirf Admin hi access kar sakay
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var users = await _context.Users
                .Include(u => u.Role)
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return View(users);
        }

        // --- USER DETAILS ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Orders) // User ke orders dekhne ke liye
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // --- EDIT (GET) ---
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Roles list ko dropdown ke liye load karein (Id aur Name properties ke sath)
            ViewBag.RoleId = new SelectList(_context.Roles, "Id", "Name", user.RoleId);

            return View(user);
        }

        // --- EDIT USER ROLE & INFO (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.Id) return NotFound();

            // Database se purana user nikalain taake password aur CreatedAt secure rahein
            var userInDb = await _context.Users.FindAsync(id);

            if (userInDb == null) return NotFound();

            try
            {
                // Nayi fields ke mutabiq update
                userInDb.FirstName = user.FirstName;
                userInDb.LastName = user.LastName;
                userInDb.Email = user.Email;
                userInDb.RoleId = user.RoleId; // Role change karne ke liye

                _context.Update(userInDb);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
            }

            // Agar error aaye to dropdown dobara bharna parta hai
            ViewBag.RoleId = new SelectList(_context.Roles, "Id", "Name", user.RoleId);
            return View(user);
        }

        // --- DELETE USER (AJAX) ---
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return Json(new { success = false, message = "User not found" });

            // Safety Check: Admin khud ko delete na kar sakay
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (id.ToString() == currentUserId)
            {
                return Json(new { success = false, message = "You cannot delete your own admin account!" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}