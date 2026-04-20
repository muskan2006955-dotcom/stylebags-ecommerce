using BagsWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BagsWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SystemLogController : Controller
    {
        private readonly BagDbContext _context;

        public SystemLogController(BagDbContext context)
        {
            _context = context;
        }

        // --- MAIN INDEX PAGE ---
        public async Task<IActionResult> Index()
        {
            // A. Auto-Cleanup: 30 din se purane logs delete karein
            var thresholdDate = DateTime.Now.AddDays(-30);
            var oldLogs = _context.LoginLogs.Where(l => l.LoginTime < thresholdDate);
            if (await oldLogs.AnyAsync())
            {
                _context.LoginLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();
            }

            // B. Chart Data Preparation (Last 7 Days)
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.Date.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var logs = await _context.LoginLogs.ToListAsync();

            var chartData = last7Days.Select(date => new {
                Date = date.ToString("dd MMM"),
                Success = logs.Count(l => l.LoginTime?.Date == date && l.Status == "Success"),
                Failed = logs.Count(l => l.LoginTime?.Date == date && l.Status == "Failed")
            }).ToList();

            ViewBag.ChartLabels = chartData.Select(x => x.Date).ToList();
            ViewBag.SuccessData = chartData.Select(x => x.Success).ToList();
            ViewBag.FailedData = chartData.Select(x => x.Failed).ToList();

            // C. Table Data (Latest 50 logs)
            var displayLogs = logs.OrderByDescending(l => l.LoginTime).Take(50).ToList();
            return View(displayLogs);
        }

        // --- MANUAL CLEAR ALL LOGS ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAllLogs()
        {
            var allLogs = await _context.LoginLogs.ToListAsync();
            if (allLogs.Any())
            {
                _context.LoginLogs.RemoveRange(allLogs);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "System history has been cleared successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}