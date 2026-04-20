using BagsWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace BagsWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BagDbContext _context;

        public HomeController(ILogger<HomeController> logger, BagDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // HomeController.cs mein add karein
        // Controller - Index Action (NO CHANGES NEEDED - Aapka sahi hai)
        public IActionResult Index()
        {
            // 1. Products (existing)
            var bags = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .ToList();

            // 2. Reviews (existing)
            ViewBag.Reviews = _context.ProductReviews
                .Where(r => r.IsApproved == true)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToList();

            // 3. Best Selling (existing)
            var bestCategories = _context.Categories
                .Include(c => c.Products)
                .ThenInclude(p => p.ProductImages)
                .OrderByDescending(c => c.Products.Count)
                .Take(3)
                .ToList();


            var allBestProducts = bestCategories
                .SelectMany(c => c.Products)
                .Take(15)
                .ToList();

            ViewBag.BestSelling = allBestProducts;
            ViewBag.BestCategoryName = bestCategories.FirstOrDefault()?.Name ?? "Best Selling";

            // ===== BEST SELLING BAGS (NEW) =====
            // ===== BEST SELLING BAGS (Flexible Search) =====
            var bestSellingBagsCat = _context.Categories
                .Include(c => c.Products)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefault(c =>
                    c.Name.ToLower().Contains("best") &&
                    c.Name.ToLower().Contains("selling"));

            ViewBag.BestSellingBags = bestSellingBagsCat?.Products?.ToList() ?? new List<Product>();
            ViewBag.DebugCategoryName = bestSellingBagsCat?.Name ?? "NOT FOUND";
            // ====================================

            // 4. HERO SECTIONS - NEW (from database)
            ViewBag.HeroSections = _context.HeroSections
                .OrderBy(h => h.Id)
                .Take(5)
                .ToList();

            return View(bags);
        }
        // FIXED: Proper JsonResult with IActionResult alternative
        [HttpGet]
        public IActionResult Search(string term)  // <-- IActionResult use karo
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                    return Json(new List<object>());

                term = term.ToLower();

                var products = _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p =>
                        p.Name.ToLower().Contains(term) ||
                        (p.Category != null && p.Category.Name.ToLower().Contains(term)) ||
                        (p.Description != null && p.Description.ToLower().Contains(term))
                    )
                    .Take(6)
                    .Select(p => new
                    {
                        type = "product",
                        id = p.Id,
                        name = p.Name,
                        category = p.Category != null ? p.Category.Name : "Uncategorized",
                        price = p.Price,
                        finalPrice = p.Discount > 0 ? p.Price * (1 - p.Discount / 100m) : p.Price,
                        discount = p.Discount ?? 0,
                        image = p.ProductImages != null && p.ProductImages.Any()
                            ? p.ProductImages.First().ImageUrl
                            : "/uploads/products/no-image.jpg",
                        url = $"/Products/Details/{p.Id}"
                    })
                    .ToList();

                var categories = _context.Categories
                    .AsNoTracking()
                    .Where(c =>
                        c.Name.ToLower().Contains(term) ||
                        (c.Description != null && c.Description.ToLower().Contains(term))
                    )
                    .Take(4)
                    .Select(c => new
                    {
                        type = "category",
                        id = c.Id,
                        name = c.Name,
                        productCount = c.Products != null ? c.Products.Count : 0,
                        url = $"/Products?categoryId={c.Id}"
                    })
                    .ToList();

                var results = new List<object>();
                results.AddRange(categories);
                results.AddRange(products);

                return Json(results);  // <-- Ab yeh kaam karega
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search Error: {ex.Message}");
                return Json(new List<object>());
            }
        }
            // Review Submit Action
            [HttpPost]
        public async Task<IActionResult> CreateReview(ProductReview review, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Fill all fields!" });

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var folder = Path.Combine("wwwroot", "img", "reviews");
                    Directory.CreateDirectory(folder);
                    var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    var path = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                        await imageFile.CopyToAsync(stream);

                    review.ImageUrl = "/img/reviews/" + fileName;
                }

                review.IsApproved = true; // Approve immediately for demo
                review.CreatedAt = DateTime.Now;

                _context.ProductReviews.Add(review);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Review posted!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public async Task<IActionResult> GetQuickView(int id)
        {
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

            ViewBag.RelatedProducts = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .AsNoTracking()
                .ToListAsync();

            return View(product);
        }

        // ==========================================
        // REVIEW ACTIONS - YEH ADD KAREIN
        // ==========================================

        // GET: Reviews display karna (Index page ke liye)
        public async Task<IActionResult> GetReviews()
        {
            var reviews = await _context.ProductReviews
                .Where(r => r.IsApproved == true)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            return PartialView("_ReviewsList", reviews);
        }

        // POST: Review submit karna - WITHOUT IWebHostEnvironment
     

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}