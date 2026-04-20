using BagsWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BagsWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrdersController : Controller
    {
        private readonly BagDbContext _context;

        public OrdersController(BagDbContext context)
        {
            _context = context;
        }

        // --- 1. ACTIVE ORDERS (Sirf Pending aur Shipped dikhayega) ---
        public async Task<IActionResult> Index()
        {
            var activeOrders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status != "Delivered" && o.Status != "Cancelled") // Filter logic
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(activeOrders);
        }

        // --- 2. ORDER HISTORY (Sirf Delivered aur Cancelled dikhayega) ---
        public async Task<IActionResult> History()
        {
            var completedOrders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status == "Delivered" || o.Status == "Cancelled") // Filter logic
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(completedOrders);
        }

        // --- 3. STATUS UPDATE ---
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order #{id} has been marked as {status}.";

            // Redirect Index par hi jayega, agar status 'Delivered' hua 
            // to wo khud hi Index se hat kar History mein chala jayega.
            return RedirectToAction(nameof(Index));
        }
    }
}

