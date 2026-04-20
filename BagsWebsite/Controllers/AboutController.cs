using BagsWebsite.Models;
using Microsoft.AspNetCore.Mvc;

public class AboutController : Controller
{
    private readonly BagDbContext _context;

    public AboutController(BagDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var about = _context.Abouts.FirstOrDefault();
        return View(about);
    }
}