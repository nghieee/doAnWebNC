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
        try
        {
            var categoriesList = _context.Categories.ToList();
            var activeProductsList = _context.Products.Where(p => p.IsActive).ToList();
            bool hasChanged = false;

            foreach (var cat in categoriesList)
            {
                var subCatIds = categoriesList
                    .Where(c => c.ParentCategoryId == cat.CategoryId)
                    .Select(c => c.CategoryId)
                    .ToList();

                var allCatIds = new List<int> { cat.CategoryId };
                allCatIds.AddRange(subCatIds);

                var subSubCatIds = categoriesList
                    .Where(c => c.ParentCategoryId != null && subCatIds.Contains(c.ParentCategoryId.Value))
                    .Select(c => c.CategoryId)
                    .ToList();
                allCatIds.AddRange(subSubCatIds);

                int count = activeProductsList.Count(p => p.CategoryId.HasValue && allCatIds.Contains(p.CategoryId.Value));
                if (cat.ProductCount != count)
                {
                    cat.ProductCount = count;
                    hasChanged = true;
                }
            }

            if (hasChanged)
            {
                _context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error sync Category ProductCount: " + ex.Message);
        }

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
                .ToList(),
            FeaturedNews = _context.News
                .Where(n => n.IsPublished && n.IsFeature)
                .OrderByDescending(n => n.PublishedAt ?? n.CreatedAt)
                .Take(4)
                .ToList()
        };

        if (User.Identity.IsAuthenticated)
        {
            var userId = _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name)?.Id;
            if (!string.IsNullOrEmpty(userId) && _context.UserVouchers.Any(uv => uv.UserId == userId && uv.IsNew))
            {
                ViewBag.ShowGiftPopup = true;
            }
        }

        var now = DateTime.Now;
        var campaignBannersList = _context.PromotionCampaigns
            .Where(c => c.BannerId != null && c.IsActive && c.StartDate <= now && c.EndDate >= now)
            .ToList();
            
        ViewBag.CampaignBanners = campaignBannersList
            .GroupBy(c => c.BannerId!.Value)
            .ToDictionary(g => g.Key, g => g.First().PromotionCampaignId);

        ViewBag.BannersWithProducts = _context.Products
            .Where(p => p.BannerId != null && p.IsActive)
            .Select(p => p.BannerId!.Value)
            .Distinct()
            .ToList();

        // Flash Sale Query
        var activeFlashCampaigns = _context.PromotionCampaigns
            .Where(c => c.BannerId == null && c.IsActive && c.StartDate <= now && c.EndDate >= now)
            .OrderBy(c => c.EndDate)
            .ToList();

        var flashSaleProducts = new List<Product>();
        if (activeFlashCampaigns.Any())
        {
            foreach (var campaign in activeFlashCampaigns)
            {
                var pQuery = _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.IsDiscountActive && p.DiscountPercent == campaign.DiscountPercent);

                if (campaign.CategoryId.HasValue)
                {
                    var catIds = GetCategoryAndDescendants(campaign.CategoryId.Value);
                    pQuery = pQuery.Where(p => p.CategoryId.HasValue && catIds.Contains(p.CategoryId.Value));
                }

                if (!string.IsNullOrWhiteSpace(campaign.Brand))
                {
                    pQuery = pQuery.Where(p => p.Brand == campaign.Brand);
                }

                var list = pQuery.ToList();
                flashSaleProducts.AddRange(list);
            }
            flashSaleProducts = flashSaleProducts.DistinctBy(p => p.ProductId).ToList();
        }

        ViewBag.FlashSaleProducts = flashSaleProducts;
        if (activeFlashCampaigns.Any())
        {
            var primaryCampaign = activeFlashCampaigns.First();
            ViewBag.FlashSaleEndDate = primaryCampaign.EndDate.ToString("yyyy-MM-ddTHH:mm:ss");
            ViewBag.FlashSaleCampaignName = primaryCampaign.Name;
            ViewBag.FlashSaleCampaignId = primaryCampaign.PromotionCampaignId;
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
                    uv.IsNew = false;
                _context.SaveChanges();
            }
        }
        return Json(new { success = true });
    }

    [HttpGet("Home/Campaign/{id}")]
    public async Task<IActionResult> Campaign(
        int id,
        string sort,
        string[] brands,
        string[] origins,
        string priceRange,
        int page = 1
    )
    {
        var campaign = await _context.PromotionCampaigns
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.PromotionCampaignId == id);

        if (campaign == null)
        {
            return NotFound();
        }

        var query = _context.Products.Where(p => p.IsActive);

        if (campaign.CategoryId.HasValue)
        {
            var catIds = GetCategoryAndDescendants(campaign.CategoryId.Value);
            query = query.Where(p => p.CategoryId.HasValue && catIds.Contains(p.CategoryId.Value));
        }

        if (!string.IsNullOrEmpty(campaign.Brand))
        {
            query = query.Where(p => p.Brand == campaign.Brand);
        }

        // Apply filters
        if (brands != null && brands.Length > 0)
        {
            query = query.Where(p => brands.Contains(p.Brand));
        }

        if (origins != null && origins.Length > 0)
        {
            query = query.Where(p => origins.Contains(p.Origin));
        }

        if (!string.IsNullOrEmpty(priceRange))
        {
            switch (priceRange)
            {
                case "1":
                    query = query.Where(p => p.Price < 200000);
                    break;
                case "2":
                    query = query.Where(p => p.Price >= 200000 && p.Price <= 500000);
                    break;
                case "3":
                    query = query.Where(p => p.Price > 500000);
                    break;
            }
        }

        // Sort
        if (sort == "price_asc")
            query = query.OrderBy(p => p.Price);
        else if (sort == "price_desc")
            query = query.OrderByDescending(p => p.Price);
        else if (sort == "name")
            query = query.OrderBy(p => p.ProductName);
        else
            query = query.OrderByDescending(p => p.SoldQuantity ?? 0).ThenByDescending(p => p.ProductId);

        // Pagination
        int pageSize = 20;
        var totalCount = query.Count();
        var products = await query
            .Include(p => p.ProductImages)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Campaign = campaign;
        ViewBag.Products = products;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        ViewBag.TotalCount = totalCount;

        ViewBag.Brands = await _context.Products.Where(p => p.IsActive).Select(p => p.Brand).Where(b => !string.IsNullOrEmpty(b)).Distinct().OrderBy(b => b).ToListAsync();
        ViewBag.Countries = await _context.Products.Where(p => p.IsActive).Select(p => p.Origin).Where(o => !string.IsNullOrEmpty(o)).Distinct().OrderBy(o => o).ToListAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("~/Views/Categories/_ProductList.cshtml", products);
        }

        return View("~/Views/Home/Campaign.cshtml", products);
    }

    [HttpGet("Home/BannerProducts/{id}")]
    public async Task<IActionResult> BannerProducts(
        int id,
        string sort,
        string[] brands,
        string[] origins,
        string priceRange,
        int page = 1
    )
    {
        var banner = await _context.Banners.FindAsync(id);
        if (banner == null)
        {
            return NotFound();
        }

        var query = _context.Products.Where(p => p.BannerId == id && p.IsActive);

        // Apply filters
        if (brands != null && brands.Length > 0)
        {
            query = query.Where(p => brands.Contains(p.Brand));
        }

        if (origins != null && origins.Length > 0)
        {
            query = query.Where(p => origins.Contains(p.Origin));
        }

        if (!string.IsNullOrEmpty(priceRange))
        {
            switch (priceRange)
            {
                case "1":
                    query = query.Where(p => p.Price < 200000);
                    break;
                case "2":
                    query = query.Where(p => p.Price >= 200000 && p.Price <= 500000);
                    break;
                case "3":
                    query = query.Where(p => p.Price > 500000);
                    break;
            }
        }

        // Sort
        if (sort == "price_asc")
            query = query.OrderBy(p => p.Price);
        else if (sort == "price_desc")
            query = query.OrderByDescending(p => p.Price);
        else if (sort == "name")
            query = query.OrderBy(p => p.ProductName);
        else
            query = query.OrderByDescending(p => p.SoldQuantity ?? 0).ThenByDescending(p => p.ProductId);

        // Pagination
        int pageSize = 20;
        var totalCount = query.Count();
        var products = await query
            .Include(p => p.ProductImages)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Banner = banner;
        ViewBag.Products = products;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        ViewBag.TotalCount = totalCount;

        ViewBag.Brands = await _context.Products.Where(p => p.IsActive).Select(p => p.Brand).Where(b => !string.IsNullOrEmpty(b)).Distinct().OrderBy(b => b).ToListAsync();
        ViewBag.Countries = await _context.Products.Where(p => p.IsActive).Select(p => p.Origin).Where(o => !string.IsNullOrEmpty(o)).Distinct().OrderBy(o => o).ToListAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("~/Views/Categories/_ProductList.cshtml", products);
        }

        return View("~/Views/Home/BannerProducts.cshtml", products);
    }

    private List<int> GetCategoryAndDescendants(int parentCategoryId)
    {
        var categories = _context.Categories.ToList();
        var allCatIds = new List<int> { parentCategoryId };

        var subCatIds = categories
            .Where(c => c.ParentCategoryId == parentCategoryId)
            .Select(c => c.CategoryId)
            .ToList();
        allCatIds.AddRange(subCatIds);

        var subSubCatIds = categories
            .Where(c => c.ParentCategoryId != null && subCatIds.Contains(c.ParentCategoryId.Value))
            .Select(c => c.CategoryId)
            .ToList();
        allCatIds.AddRange(subSubCatIds);

        return allCatIds.Distinct().ToList();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
