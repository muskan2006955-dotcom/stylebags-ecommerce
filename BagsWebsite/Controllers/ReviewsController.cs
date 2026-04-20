using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BagsWebsite.Models;

namespace BagsWebsite.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly BagDbContext _context;

        // Constructor mein DbContext inject karna
        public ReviewsController(BagDbContext context)
        {
            _context = context;
        }

        // GET: /Reviews - Sirf approved reviews dikhana
        public async Task<IActionResult> Index()
        {
            // Approved reviews fetch karna
            var reviews = await _context.ProductReviews
                .Where(r => r.IsApproved == true)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Stats calculate karna
            ViewBag.TotalReviews = reviews.Count;
            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            // Star counts
            ViewBag.FiveStar = reviews.Count(r => r.Rating == 5);
            ViewBag.FourStar = reviews.Count(r => r.Rating == 4);
            ViewBag.ThreeStar = reviews.Count(r => r.Rating == 3);
            ViewBag.TwoStar = reviews.Count(r => r.Rating == 2);
            ViewBag.OneStar = reviews.Count(r => r.Rating == 1);

            return View(reviews);
        }
    }
}