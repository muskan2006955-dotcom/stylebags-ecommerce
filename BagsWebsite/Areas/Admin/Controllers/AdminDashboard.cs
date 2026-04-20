using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BagsWebsite.Models; // Apne Models ka sahi namespace check karlein

namespace BagsWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly BagDbContext _context;

        // Context Dependency Injection
        public AdminDashboardController(BagDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Security check (Aapka original logic)
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // 2. Fetch Data for 4 Stats (Updated for eCommerce)

            // Total Revenue (Order table se sum)
            ViewBag.TotalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount) ?? 0;

            // Total Orders count
            ViewBag.OrderCount = await _context.Orders.CountAsync();

            // Total Products count
            ViewBag.TotalProducts = await _context.Products.CountAsync();

            // Low Stock (Bags jin ki quantity 5 ya usse kam hai)
            ViewBag.LowStockCount = await _context.Products.CountAsync(p => p.Stock <= 5);

            // 3. Recent Products List (Wahi jo aap pehle use kar rahi thin)
            var recentProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();

            return View(recentProducts);
        }
    }
}