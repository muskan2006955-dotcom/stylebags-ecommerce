using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BagsWebsite.Models;

namespace BagsWebsite.Controllers
{
    public class CartController : Controller
    {
        private readonly BagDbContext _context;

        public CartController(BagDbContext context)
        {
            _context = context;
        }

        // ============================================
        // HELPER: Get User ID from Session
        // ============================================
        private int? GetUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int id))
                return id;
            return null;
        }

        // ============================================
        // GET: Cart Summary for Badge
        // ============================================
        [HttpGet]
        public async Task<IActionResult> GetCartSummary()
        {
            var userId = GetUserId();

            if (userId == null)
                return Json(new { count = 0, loggedIn = false });

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            var count = cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;

            return Json(new { count, loggedIn = true });
        }

        // ============================================
        // GET: Cart Items for Sidebar
        // ============================================
        [HttpGet]
        public async Task<IActionResult> GetCartItems()
        {
            var userId = GetUserId();

            if (userId == null)
                return Json(new { success = false, message = "Not logged in" });

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || !cart.CartItems.Any())
            {
                return Json(new
                {
                    success = true,
                    items = new List<object>(),
                    total = 0,
                    itemCount = 0
                });
            }

            var items = cart.CartItems.Select(ci => new
            {
                cartItemId = ci.Id,
                productId = ci.ProductId,
                productName = ci.Product.Name,
                price = ci.Product.Price,
                quantity = ci.Quantity,
                imageUrl = ci.Product.ProductImages.FirstOrDefault()?.ImageUrl ?? "/img/no-image.jpg"
            }).ToList();

            var total = items.Sum(i => i.price * i.quantity);

            return Json(new
            {
                success = true,
                items,
                total,
                itemCount = items.Sum(i => i.quantity)
            });
        }

        // ============================================
        // POST: Add to Cart
        // ============================================
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = GetUserId();

            if (userId == null)
                return Json(new { success = false, message = "Please login first", redirectUrl = "/Account/Login" });

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
            {
                cart = new Cart { UserId = userId.Value };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingItem = cart.CartItems?.FirstOrDefault(ci => ci.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();

            var count = cart.CartItems?.Sum(ci => ci.Quantity) ?? 0;
            return Json(new { success = true, count, message = "Added to cart" });
        }

        // ============================================
        // POST: Update Quantity
        // ============================================
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var userId = GetUserId();

            if (userId == null)
                return Json(new { success = false, message = "Please login first" });

            var item = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId.Value);

            if (item == null)
                return Json(new { success = false, message = "Item not found" });

            if (quantity <= 0)
                _context.CartItems.Remove(item);
            else
                item.Quantity = quantity;

            await _context.SaveChangesAsync();

            // Recalculate
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == item.CartId);

            var total = cart?.CartItems.Sum(ci => ci.Quantity * ci.Product.Price) ?? 0;
            var itemCount = cart?.CartItems.Sum(ci => ci.Quantity) ?? 0;

            return Json(new { success = true, total, itemCount });
        }

        // ============================================
        // POST: Remove Item
        // ============================================
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Json(new { success = false, message = "Please login first" });

            var item = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId.Value);

            if (item == null)
                return Json(new { success = false, message = "Item not found" });

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == item.CartId);

            var total = cart?.CartItems.Sum(ci => ci.Quantity * ci.Product.Price) ?? 0;
            var itemCount = cart?.CartItems.Sum(ci => ci.Quantity) ?? 0;

            return Json(new { success = true, total, itemCount });
        }
    }
}