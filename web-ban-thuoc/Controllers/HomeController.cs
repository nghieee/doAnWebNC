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
            Banners = _context.Banners
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder)
                .ThenBy(b => b.CreatedAt)
                .ToList(),
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
        // Kiểm tra user đăng nhập và có voucher mới không
        if (User.Identity.IsAuthenticated)
        {
            var userId = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name)?.Id;
            if (!string.IsNullOrEmpty(userId))
            {
                bool hasNewGift = _context.UserVouchers.Any(uv => uv.UserId == userId && uv.IsNew);
                if (hasNewGift)
                {
                    ViewBag.ShowGiftPopup = true;
                }
            }
        }
        return View(viewModel);
    }

    [HttpPost]
    public IActionResult MarkGiftSeen()
    {
        if (User.Identity.IsAuthenticated)
        {
            var userId = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name)?.Id;
            if (!string.IsNullOrEmpty(userId))
            {
                var newVouchers = _context.UserVouchers.Where(uv => uv.UserId == userId && uv.IsNew).ToList();
                foreach (var uv in newVouchers)
                {
                    uv.IsNew = false;
                }
                _context.SaveChanges();
            }
        }
        return Json(new { success = true });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
