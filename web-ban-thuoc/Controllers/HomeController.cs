using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        var viewModel = new HomeViewModel
        {
            FeaturedCategories = _context.Categories
                .Where(c => c.IsFeature && c.ParentCategoryId != null && c.CategoryLevel == 2.ToString())
                .OrderBy(c => c.CategoryName)
                .Take(12)
                .ToList(),
            FeaturedProducts = _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.IsFeature && p.IsActive && p.StockQuantity > 0)
                .OrderBy(p => p.ProductName)
                .Take(12)
                .ToList()
        };

        return View(viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
