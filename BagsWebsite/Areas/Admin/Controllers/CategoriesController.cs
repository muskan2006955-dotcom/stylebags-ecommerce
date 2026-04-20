using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BagsWebsite.Models;

namespace BagsWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoriesController : Controller
    {
        private readonly BagDbContext _context;

        public CategoriesController(BagDbContext context)
        {
            _context = context;
        }

        // --- LIST ---
        // --- LIST (Updated with Filter) ---
        // Purane saare Index methods delete karke sirf ye EK rakhein
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm)
        {
            // 1. Query ko start karein (AsQueryable taake filter baad mein lag sake)
            var categoriesQuery = _context.Categories
                .Include(c => c.Products)
                .AsQueryable();

            // 2. Agar search box mein kuch likha hai to filter apply karein
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                categoriesQuery = categoriesQuery.Where(c =>
                    c.Name.Contains(searchTerm) ||
                    (c.Description != null && c.Description.Contains(searchTerm))
                );
            }

            // 3. Last mein list fetch karein
            var categories = await categoriesQuery.ToListAsync();

            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // --- CREATE (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            ModelState.Clear();

            try
            {
                if (string.IsNullOrEmpty(category.Name))
                {
                    ModelState.AddModelError("Name", "Please enter a category name.");
                    return View(category);
                }

                // Description khud hi 'category' object ke saath bind ho kar save ho jayegi
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Database Error: " + (ex.InnerException?.Message ?? ex.Message));
                return View(category);
            }
        }

        // --- DETAILS ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // --- EDIT (GET) ---
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // --- EDIT (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            if (string.IsNullOrEmpty(category.Name))
            {
                ModelState.AddModelError("Name", "Name is required.");
                return View(category);
            }

            try
            {
                // Update method poore object (Name + Description) ko update kar dega
                _context.Update(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Category updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View(category);
            }
        }

        // --- DELETE (AJAX) ---
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return Json(new { success = false, message = "Category nahi mili!" });
            }

            if (category.Products.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "This category contains active products. Please reassign or delete the products before removing the category."
                });
            }

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}