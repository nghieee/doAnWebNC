using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

[Route("Products")]
public class ProductController : Controller
{
    private readonly LongChauDbContext _context;
    private readonly ILogger<ProductController> _logger;

    public ProductController(LongChauDbContext context, ILogger<ProductController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{id}")]
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

    [HttpGet("Search")]
    public IActionResult Search(string query, int page = 1, string sort = "name")
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return RedirectToAction("Index", "Home");
        }

        // Redirect đến Categories controller với query parameter
        return RedirectToAction("Index", "Categories", new { 
            categoryId = 0, // 0 để chỉ ra đây là tìm kiếm toàn bộ
            search = query,
            sort = sort,
            page = page
        });
    }

    [HttpGet("Suggest")]
    public IActionResult Suggest(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new { products = new List<object>(), categories = new List<object>(), brands = new List<string>() });
            }

            _logger.LogInformation($"Searching for suggestions with query: {query}");

            // Gợi ý sản phẩm (tối đa 5)
            var products = _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.ProductName.Contains(query) && p.IsActive)
                .OrderBy(p => p.ProductName)
                .Select(p => new {
                    productId = p.ProductId,
                    productName = p.ProductName ?? "Không xác định",
                    brand = p.Brand ?? "Không xác định",
                    imageUrl = p.ProductImages.FirstOrDefault(pi => pi.IsMain == true).ImageUrl ?? 
                               p.ProductImages.FirstOrDefault().ImageUrl ?? "default.png"
                })
                .Take(5)
                .ToList();

            // Gợi ý danh mục lv2, lv3 (tối đa 5)
            var categories = _context.Categories
                .Where(c => (c.CategoryLevel == "2" || c.CategoryLevel == "3") && c.CategoryName.Contains(query))
                .OrderBy(c => c.CategoryName)
                .Select(c => new {
                    categoryId = c.CategoryId,
                    categoryName = c.CategoryName ?? "Không xác định",
                    categoryLevel = c.CategoryLevel ?? "2"
                })
                .Take(5)
                .ToList();

            // Gợi ý thương hiệu (brand, tối đa 5, distinct)
            var brands = _context.Products
                .Where(p => p.Brand != null && p.Brand.Contains(query) && p.IsActive)
                .Select(p => p.Brand)
                .Distinct()
                .Take(5)
                .ToList();

            var result = new { products, categories, brands };
            
            _logger.LogInformation($"Found {products.Count} products, {categories.Count} categories, {brands.Count} brands");
            
            // Debug: Log first product details
            if (products.Any())
            {
                var firstProduct = products.First();
                _logger.LogInformation($"First product: ID={firstProduct.productId}, Name={firstProduct.productName}, Brand={firstProduct.brand}, Image={firstProduct.imageUrl}");
            }
            
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Suggest method for query: {Query}", query);
            return StatusCode(500, new { error = "Có lỗi xảy ra khi tìm kiếm" });
        }
    }
}