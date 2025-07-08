using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

public class ProductController : Controller
{
    private readonly LongChauDbContext _context;

    public ProductController(LongChauDbContext context)
    {
        _context = context;
    }

    public IActionResult Details(int id)
    {
        ViewData["Title"] = "Chi tiết sản phẩm - Nhà Thuốc Long Châu";
        var product = _context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Category)
            .FirstOrDefault(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }
}