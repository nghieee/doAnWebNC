using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers;

public class HomeController : Controller
{
    private readonly LongChauDbContext _context;

    public HomeController(LongChauDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var featuredCategories = _context.Categories
            .Where(c => c.IsFeatured && c.ParentCategoryId != null)
            .OrderBy(c => c.CategoryName)
            .Take(12)
            .ToList();

        return View(featuredCategories);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
