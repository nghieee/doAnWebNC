using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;
using Microsoft.AspNetCore.Identity;

[Route("Products")]
public class ProductController : Controller
{
    private readonly LongChauDbContext _context;
    private readonly ILogger<ProductController> _logger;
    private readonly UserManager<IdentityUser> _userManager;

    public ProductController(LongChauDbContext context, ILogger<ProductController> logger, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpGet("{id}")]
    public IActionResult Details(int id)
    {
        ViewData["Title"] = "Chi tiết sản phẩm - Nhà Thuốc Long Châu";
        var product = _context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .ThenInclude(r => r.User)
            .FirstOrDefault(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound();
        }

        if (User.Identity.IsAuthenticated)
        {
            var userId = _userManager.GetUserId(User);
            var userReview = product.Reviews.FirstOrDefault(r => r.UserId == userId);
            if (userReview != null)
            {
                ViewBag.UserReview = userReview;
            }
            else
            {
                // Kiểm tra user đã mua sản phẩm này với đơn hàng đã giao/thanh toán
                bool canReview = _context.Orders
                    .Where(o => o.UserId == userId && (o.Status == "Đã giao" || o.PaymentStatus == "Đã thanh toán"))
                    .SelectMany(o => o.OrderItems)
                    .Any(oi => oi.ProductId == id);
                ViewBag.CanReview = canReview;
            }
        }
        return View(product);
    }

    [HttpPost("AddReview")]
    [ValidateAntiForgeryToken]
    public IActionResult AddReview(int productId, int rating, string comment)
    {
        if (!User.Identity.IsAuthenticated)
        {
            TempData["ReviewError"] = "Bạn cần đăng nhập để đánh giá sản phẩm.";
            return RedirectToAction("Details", new { id = productId });
        }
        var userId = _userManager.GetUserId(User);
        // Kiểm tra đã mua hàng
        bool hasPurchased = _context.Orders
            .Where(o => o.UserId == userId && (o.Status == "Đã giao" || o.PaymentStatus == "Đã thanh toán"))
            .SelectMany(o => o.OrderItems)
            .Any(oi => oi.ProductId == productId);
        if (!hasPurchased)
        {
            TempData["ReviewError"] = "Bạn chỉ có thể đánh giá khi đã mua sản phẩm này.";
            return RedirectToAction("Details", new { id = productId });
        }
        // Kiểm tra đã đánh giá chưa
        if (_context.Reviews.Any(r => r.UserId == userId && r.ProductId == productId))
        {
            TempData["ReviewError"] = "Bạn đã đánh giá sản phẩm này rồi.";
            return RedirectToAction("Details", new { id = productId });
        }
        var review = new Review
        {
            UserId = userId,
            ProductId = productId,
            Rating = rating,
            Comment = comment,
            ReviewDate = DateTime.Now
        };
        _context.Reviews.Add(review);
        _context.SaveChanges();
        TempData["ReviewSuccess"] = "Đánh giá của bạn đã được ghi nhận!";
        return RedirectToAction("Details", new { id = productId });
    }

    [HttpPost("EditReview")]
    [ValidateAntiForgeryToken]
    public IActionResult EditReview(int reviewId, int rating, string comment)
    {
        var review = _context.Reviews.FirstOrDefault(r => r.ReviewId == reviewId);
        if (review == null)
        {
            TempData["ReviewError"] = "Không tìm thấy đánh giá để sửa.";
            return RedirectToAction("Details", new { id = review?.ProductId ?? 0 });
        }
        var userId = _userManager.GetUserId(User);
        if (review.UserId != userId)
        {
            TempData["ReviewError"] = "Bạn không có quyền sửa đánh giá này.";
            return RedirectToAction("Details", new { id = review.ProductId });
        }
        // Xác nhận lại (có thể xác nhận ở phía client bằng JS, ở đây chỉ cập nhật)
        review.Rating = rating;
        review.Comment = comment;
        review.ReviewDate = DateTime.Now;
        _context.SaveChanges();
        TempData["ReviewSuccess"] = "Đã cập nhật đánh giá thành công!";
        return RedirectToAction("Details", new { id = review.ProductId });
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

[Route("api/products")]
[ApiController]
public class ProductApiController : ControllerBase
{
    private readonly LongChauDbContext _context;
    public ProductApiController(LongChauDbContext context) { _context = context; }

    [HttpGet("{id}")]
    public IActionResult GetProduct(int id)
    {
        var p = _context.Products.FirstOrDefault(x => x.ProductId == id && x.IsActive);
        if (p == null) return NotFound();
        return Ok(new {
            id = p.ProductId,
            name = p.ProductName,
            brand = p.Brand,
            price = p.Price,
            image = p.ProductImages.FirstOrDefault()?.ImageUrl,
            uses = p.Uses,
            targetUsers = p.TargetUsers
        });
    }
}