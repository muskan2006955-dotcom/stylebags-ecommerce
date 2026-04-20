using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BagsWebsite.Models;
using BagsWebsite.Areas.Admin.Models;

namespace BagsWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly BagDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(BagDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index(string searchTerm)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var search = $"%{searchTerm}%"; // SQL Like pattern
                productsQuery = productsQuery.Where(p =>
                    EF.Functions.Like(p.Name, search) ||
                    (p.Category != null && EF.Functions.Like(p.Category.Name, search)) ||
                    (p.Color != null && EF.Functions.Like(p.Color, search))
                );
            }

            var products = await productsQuery.OrderByDescending(p => p.Id).ToListAsync();
            ViewBag.SearchTerm = searchTerm;
            return View(products);
        }
        [HttpGet]
        // GET: Admin/Products/Create
        public IActionResult Create()
        {
            // Check karein ke '_context.Categories' khali toh nahi?
            var categories = _context.Categories.ToList();

            // "Id" value banegi aur "Name" display hoga
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name");

            return View();
        }

        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(ProductViewModel vm)
{
    // Validation cleanup
    ModelState.Remove("Category");
    ModelState.Remove("ImageVariants");

    if (ModelState.IsValid)
    {
        // 1. Pehle Main Product Save Karein
        var product = new Product
        {
            Name = vm.Name,
            Description = vm.Description,
            Price = vm.Price,
            Stock = vm.Stock,
            CategoryId = vm.CategoryId,
            Discount = vm.Discount,
            CreatedAt = DateTime.Now
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // 2. Ab Images Groups ko Handle Karein
        if (vm.ImageVariants != null && vm.ImageVariants.Any())
        {
            string folder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/products");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            // PEHLA LOOP: Har Color Group ke liye (e.g. Red Group, Blue Group)
            foreach (var group in vm.ImageVariants)
            {
                // DOSRA LOOP: Us specific color ki tamam images ke liye
                if (group.Files != null && group.Files.Any())
                {
                    foreach (var file in group.Files)
                    {
                        if (file.Length > 0)
                        {
                            string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                            string filePath = Path.Combine(folder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // Database mein entry (Har pic ke sath wahi Group Color jayega)
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = "/uploads/products/" + fileName,
                                ColorCode = group.ColorCode 
                            });
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    
    ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name", vm.CategoryId);
    return View(vm);
}
        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var vm = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                Discount = product.Discount ?? 0,
                CategoryId = product.CategoryId ?? 0,
                Color = product.Color
            };

            ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.CurrentImages = product.ProductImages.ToList(); // Purani images dikhane ke liye

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(ProductViewModel vm)
        {
            // Skip validation for a moment to test
            var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == vm.Id);

            if (product != null)
            {
                product.Name = vm.Name;
                product.Description = vm.Description;
                product.Price = vm.Price;
                product.Stock = vm.Stock;
                product.CategoryId = vm.CategoryId;
                product.Discount = vm.Discount;
                product.Color = vm.Color;

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                // Handle Images
                if (vm.ImageVariants != null)
                {
                    foreach (var group in vm.ImageVariants)
                    {
                        if (group.Files != null && group.Files.Any())
                        {
                            foreach (var file in group.Files)
                            {
                                string fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                                string path = Path.Combine(_hostEnvironment.WebRootPath, "uploads/products", fileName);
                                using (var stream = new FileStream(path, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }
                                _context.ProductImages.Add(new ProductImage
                                {
                                    ProductId = product.Id,
                                    ImageUrl = "/uploads/products/" + fileName,
                                    ColorCode = group.ColorCode
                                });
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Index");
            }
            return Content("Product not found in DB!");
        }

        // Image Delete Action (AJAX ke liye)
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var img = await _context.ProductImages.FindAsync(id);
            if (img == null) return Json(new { success = false });

            var path = Path.Combine(_hostEnvironment.WebRootPath, img.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Product ke saath Categories aur ProductImages ko load karna
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return Json(new { success = false });

            foreach (var img in product.ProductImages)
            {
                var path = Path.Combine(_hostEnvironment.WebRootPath, img.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        [HttpPost]
        //public async Task<IActionResult> DeleteImage(int id)
        //{
        //    var img = await _context.ProductImages.FindAsync(id);
        //    if (img != null)
        //    {
        //        // 1. Physical file delete karna
        //        var filePath = Path.Combine(_hostEnvironment.WebRootPath, img.ImageUrl.TrimStart('/'));
        //        if (System.IO.File.Exists(filePath))
        //        {
        //            System.IO.File.Delete(filePath);
        //        }

        //        // 2. Database se record khatam karna
        //        _context.ProductImages.Remove(img);
        //        await _context.SaveChangesAsync();
        //        return Json(new { success = true });
        //    }
        //    return Json(new { success = false });
        //}
        private void LoadCategories(object selected = null)
        {
            ViewBag.CategoryList = new SelectList(_context.Categories.ToList(), "Id", "Name", selected);
        }
    }
}