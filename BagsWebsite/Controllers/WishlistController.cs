using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BagsWebsite.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BagsWebsite.Controllers
{
    public class WishlistController : Controller
    {
        private readonly BagDbContext _context;

        public WishlistController(BagDbContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> GetWishlistItems([FromBody] List<int> productIds)
        {
            if (productIds == null || !productIds.Any())
                return Json(new { success = false, message = "No items", data = new List<object>() });

            try
            {
                var products = await _context.Products
                    .Include(p => p.ProductImages)
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        price = p.Price,
                        discount = p.Discount,
                        imageUrl = p.ProductImages.FirstOrDefault() != null
                                   ? p.ProductImages.FirstOrDefault().ImageUrl
                                   : "/img/no-image.jpg"
                    }).ToListAsync();

                return Json(new { success = true, data = products });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncWishlist([FromBody] List<string> productIds)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString))
                    return Json(new { success = false, message = "Login first" });

                int userId = int.Parse(userIdString);

                if (productIds != null && productIds.Any())
                {
                    foreach (var idStr in productIds)
                    {
                        if (int.TryParse(idStr, out int pId))
                        {
                            // Check if already exists
                            var exists = await _context.Wishlists
                                .AnyAsync(w => w.UserId == userId && w.ProductId == pId);

                            if (!exists)
                            {
                                // Pura path use karein model ka
                                var entry = new BagsWebsite.Models.Wishlist
                                {
                                    UserId = userId,
                                    ProductId = pId
                                };
                                _context.Wishlists.Add(entry);
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                // Agar ab bhi error aaye toh asli wajah pata chale
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}