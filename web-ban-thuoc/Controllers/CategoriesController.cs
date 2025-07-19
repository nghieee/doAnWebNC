using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers;

[Route("Categories")]
public class CategoriesController : Controller
{
    private readonly LongChauDbContext _context;

    public CategoriesController(LongChauDbContext context)
    {
        _context = context;
    }

    // Hiển thị danh sách sản phẩm theo danh mục hoặc tìm kiếm toàn bộ
    [HttpGet("{categoryId}")]
    public IActionResult Index(
        int categoryId,
        string sort,
        string[] brands,
        string[] origins,
        string priceRange,
        string search,
        int page = 1
    )
    {
        int pageSize = 20;
        var isSearchMode = categoryId == 0 && !string.IsNullOrEmpty(search);
        
        IQueryable<Product> productsQuery;

        if (isSearchMode)
        {
            // Chế độ tìm kiếm toàn bộ
            ViewData["Title"] = $"Kết quả tìm kiếm: {search} - Nhà Thuốc Long Châu";
            
            // Lấy các danh mục lv2, lv3 có tên khớp
            var matchedCategories = _context.Categories
                .Where(c => (c.CategoryLevel == "2" || c.CategoryLevel == "3") && c.CategoryName.Contains(search))
                .Select(c => c.CategoryId)
                .ToList();

            productsQuery = _context.Products
                .Where(p => p.IsActive && (
                    p.ProductName.Contains(search) ||
                    (p.Brand != null && p.Brand.Contains(search)) ||
                    (p.CategoryId != null && matchedCategories.Contains(p.CategoryId.Value))
                ));
        }
        else
        {
            // Chế độ hiển thị theo danh mục
            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
            if (category == null)
                return NotFound();

            ViewData["Title"] = $"{category.CategoryName} - Nhà Thuốc Long Châu";

            var childCategoryIds = _context.Categories
                .Where(c => c.ParentCategoryId == categoryId)
                .Select(c => c.CategoryId)
                .ToList();

            List<int> allCategoryIds;
            if (childCategoryIds.Any())
            {
                // Nếu có con, lấy sản phẩm của chính nó và các con
                allCategoryIds = new List<int> { categoryId };
                allCategoryIds.AddRange(childCategoryIds);
            }
            else
            {
                // Nếu không có con, chỉ lấy sản phẩm của chính nó (danh mục cháu)
                allCategoryIds = new List<int> { categoryId };
            }

            productsQuery = _context.Products
                .Where(p => p.CategoryId.HasValue && allCategoryIds.Contains(p.CategoryId.Value) && p.IsActive);

            ViewBag.Category = category;
        }

        // Lọc theo thương hiệu
        if (brands != null && brands.Length > 0)
            productsQuery = productsQuery.Where(p => brands.Contains(p.Brand));

        // Lọc theo nguồn gốc
        if (origins != null && origins.Length > 0)
            productsQuery = productsQuery.Where(p => origins.Contains(p.Origin));

        // Lọc theo giá
        if (!string.IsNullOrEmpty(priceRange))
        {
            switch (priceRange)
            {
                case "1":
                    productsQuery = productsQuery.Where(p => p.Price < 200000);
                    break;
                case "2":
                    productsQuery = productsQuery.Where(p => p.Price >= 200000 && p.Price <= 500000);
                    break;
                case "3":
                    productsQuery = productsQuery.Where(p => p.Price > 500000);
                    break;
            }
        }

        // Lọc theo tên sản phẩm (chỉ khi không phải search mode)
        if (!isSearchMode && !string.IsNullOrEmpty(search))
            productsQuery = productsQuery.Where(p => p.ProductName.Contains(search));

        // Sắp xếp
        if (sort == "price_asc")
            productsQuery = productsQuery.OrderBy(p => p.Price);
        else if (sort == "price_desc")
            productsQuery = productsQuery.OrderByDescending(p => p.Price);
        else if (sort == "name")
            productsQuery = productsQuery.OrderBy(p => p.ProductName);
        else
            productsQuery = productsQuery.OrderByDescending(p => p.SoldQuantity ?? 0).ThenByDescending(p => p.ProductId);

        // Phân trang
        var totalCount = productsQuery.Count();
        var products = productsQuery
            .Include(p => p.ProductImages)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.Products = products;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        ViewBag.TotalCount = totalCount;
        ViewBag.IsSearchMode = isSearchMode;
        ViewBag.SearchQuery = search;

        // Truyền dữ liệu filter cho view
        ViewBag.Brands = _context.Products
            .Where(p => p.IsActive)
            .Select(p => p.Brand)
            .Where(b => !string.IsNullOrEmpty(b))
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        ViewBag.Countries = _context.Products
            .Where(p => p.IsActive)
            .Select(p => p.Origin)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        // Nếu là AJAX request, trả về partial view chỉ chứa danh sách sản phẩm
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_ProductList", products);
        }

        return View("~/Views/Categories/Index.cshtml");
    }
}
