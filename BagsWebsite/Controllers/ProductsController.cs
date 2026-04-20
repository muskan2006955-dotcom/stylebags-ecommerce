using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BagsWebsite.Models; // Apne Models ka sahi namespace check karein

namespace BagsWebsite.Controllers
{
    public class ProductsController : Controller
    {
        private readonly BagDbContext _context;

        // Constructor Injection: Database context ko yahan connect kiya hai
        public ProductsController(BagDbContext context)
        {
            _context = context;
        }

        // 1. Shop/Index Page (Sare products dikhane ke liye)
        // ProductsController.cs
        public async Task<IActionResult> Index(int? categoryId)
        {
            var query = _context.Products.Include(p => p.ProductImages).AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);

                // Select ki hui category ka data nikal kar ViewBag mein daal dein
                var selectedCategory = await _context.Categories.FindAsync(categoryId.Value);
                ViewBag.CategoryName = selectedCategory?.Name;
                ViewBag.CategoryDesc = selectedCategory?.Description;
            }
            else
            {
                // Default values jab "All Collection" ho
                ViewBag.CategoryName = "Our Premium Collection";
                ViewBag.CategoryDesc = "Explore our exclusive range of high-performance bags and accessories.";
            }

            var model = await query.ToListAsync();
            return View(model);
        }        // 2. Product Details Page (Single Product View)
        [HttpGet]
        public async Task<IActionResult> GetQuickView(int id)
        {
            // Yahan breakpoint laga kar check karein ke control yahan aa raha hai ya nahi
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return PartialView("_QuickViewPartial", product);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductComments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            // Limit hatadi hai taake saari category images ayein
            ViewBag.RelatedProducts = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .AsNoTracking()
                .ToListAsync();

            return View(product);
        }
        [HttpPost]
        public async Task<IActionResult> AddComment(int ProductId, string Content, int Rating)
        {
            // Session se login user ki ID lein
            var sessionUserId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(sessionUserId))
            {
                // Agar login nahi hai to login page par bhejein
                return RedirectToAction("Login", "Account");
            }

            var comment = new ProductComment
            {
                ProductId = ProductId,
                Content = Content,
                Rating = Rating,
                UserId = int.Parse(sessionUserId), // String to Int conversion
                CreatedAt = DateTime.Now
            };

            _context.ProductComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = ProductId });
        }






        [HttpPost]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.ProductComments.FindAsync(id);
            if (comment == null) return NotFound();

            var sessionUserId = HttpContext.Session.GetString("UserId");
            var sessionUserRole = HttpContext.Session.GetString("UserRole");

            // Security Check: Sirf Admin ya wahi user jisne comment kiya ho
            if (sessionUserRole == "Admin" || comment.UserId.ToString() == sessionUserId)
            {
                _context.ProductComments.Remove(comment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = comment.ProductId });
        }
        [HttpPost]
        public async Task<IActionResult> ToggleWishlist(int productId)
        {
            // 1. Check karein ke user login hai ya nahi
            // Aapne User model use kiya hai, toh Session ya Identity se UserId lein
            var userId = HttpContext.Session.GetInt32("UserId"); // Example session usage

            if (userId == null)
            {
                return Json(new { success = false, message = "Please login first!" });
            }

            // 2. Check karein ke kya ye product pehle se wishlist mein hai?
            var existingEntry = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.ProductId == productId && w.UserId == userId);

            if (existingEntry != null)
            {
                // Agar pehle se hai, toh remove kar dein (Toggle behavior)
                _context.Wishlists.Remove(existingEntry);
                await _context.SaveChangesAsync();
                return Json(new { success = true, status = "removed" });
            }
            else
            {
                // Agar nahi hai, toh naya save karein
                var newItem = new Wishlist
                {
                    ProductId = productId,
                    UserId = userId.Value
                };
                _context.Wishlists.Add(newItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, status = "added" });
            }
        }
    }
}