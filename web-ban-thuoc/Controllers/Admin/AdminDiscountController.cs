using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("AdminDiscount")]
    public class AdminDiscountController : Controller
    {
        private readonly LongChauDbContext _context;

        public AdminDiscountController(LongChauDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? status = null)
        {
            var nowTime = DateTime.Now;
            var expiredProducts = await _context.Products
                .Where(p => p.IsDiscountActive && p.DiscountEndDate.HasValue && p.DiscountEndDate.Value < nowTime)
                .ToListAsync();

            if (expiredProducts.Any())
            {
                foreach (var p in expiredProducts)
                {
                    p.IsDiscountActive = false;
                    
                    var log = new DbActivityLog
                    {
                        EntityName = "Sản phẩm",
                        EntityId = p.ProductId.ToString(),
                        Action = "Kết thúc giảm giá",
                        Description = $"Hệ thống tự động kết thúc giảm giá của sản phẩm '{p.ProductName}' do hết hạn. (Mức giảm: {p.DiscountPercent}%, End: {p.DiscountEndDate:dd/MM/yyyy HH:mm})",
                        UserEmail = "Hệ thống",
                        CreatedAt = p.DiscountEndDate.GetValueOrDefault(nowTime)
                    };
                    _context.DbActivityLogs.Add(log);
                }
                await _context.SaveChangesAsync();
            }

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Banner)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.ProductName.Contains(search) || (p.Sku != null && p.Sku.Contains(search)));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var allProducts = await query.OrderByDescending(p => p.ProductId).ToListAsync();

            var now = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        allProducts = allProducts.Where(p => p.IsOnDiscount).ToList();
                        break;
                    case "upcoming":
                        allProducts = allProducts.Where(p => p.IsDiscountActive && p.DiscountStartDate.HasValue && p.DiscountStartDate.Value > now).ToList();
                        break;
                    case "expired":
                        allProducts = allProducts.Where(p => p.DiscountEndDate.HasValue && p.DiscountEndDate.Value < now).ToList();
                        break;
                    case "disabled":
                        allProducts = allProducts.Where(p => !p.IsDiscountActive && (p.DiscountPrice.HasValue || p.DiscountPercent.HasValue)).ToList();
                        break;
                }
            }

            var campaigns = await _context.PromotionCampaigns
                .Include(c => c.Category)
                .Include(c => c.Banner)
                .OrderByDescending(c => c.PromotionCampaignId)
                .ToListAsync();

            ViewBag.Campaigns = campaigns;
            ViewBag.Categories = await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();
            ViewBag.Brands = await _context.Products.Where(p => p.Brand != null && p.Brand != "").Select(p => p.Brand!).Distinct().OrderBy(b => b).ToListAsync();
            ViewBag.Banners = await _context.Banners.Where(b => b.IsActive).OrderBy(b => b.Title).ToListAsync();

            var productLogs = await _context.DbActivityLogs
                .Where(log => log.EntityName == "Sản phẩm" && 
                               (log.Description.Contains("DiscountPercent") || 
                                log.Description.Contains("Phần trăm giảm giá") || 
                                log.Description.Contains("DiscountPrice") || 
                                log.Description.Contains("Giá sau giảm") || 
                                log.Description.Contains("DiscountStartDate") || 
                                log.Description.Contains("Ngày bắt đầu giảm") || 
                                log.Description.Contains("DiscountEndDate") || 
                                log.Description.Contains("Ngày kết thúc giảm") || 
                                log.Description.Contains("IsDiscountActive") || 
                                log.Description.Contains("Kích hoạt giảm giá") || 
                                log.Description.Contains("BannerId") || 
                                log.Description.Contains("Liên kết Banner") || 
                                log.Description.Contains("giảm giá") ||
                                log.Description.Contains("khuyến mãi")))
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync();

            var logsByProduct = productLogs
                .GroupBy(l => l.EntityId)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToList();

            var productIdsWithLogs = logsByProduct
                .Select(g => int.TryParse(g.Key, out int id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var productsWithLogs = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => productIdsWithLogs.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId.ToString());

            ViewBag.LogsByProduct = logsByProduct;
            ViewBag.ProductsWithLogs = productsWithLogs;

            ViewBag.TotalCount = allProducts.Count;
            ViewBag.DiscountedCount = allProducts.Count(p => p.IsOnDiscount);
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Status = status;

            return View("~/Views/Admin/Discount/Index.cshtml", allProducts);
        }

        [HttpPost("SetDiscount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDiscount(
            [FromForm] List<int> productIds,
            [FromForm] double? discountPercent,
            [FromForm] decimal? discountPrice,
            [FromForm] DateTime? startDate,
            [FromForm] DateTime? endDate,
            [FromForm] bool isActive = true,
            [FromForm] int? bannerId = null)
        {
            if (productIds == null || !productIds.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm.";
                return RedirectToAction("Index");
            }

            var products = await _context.Products.Where(p => productIds.Contains(p.ProductId)).ToListAsync();
            foreach (var product in products)
            {
                if (discountPercent.HasValue && discountPercent.Value > 0)
                {
                    product.DiscountPercent = Math.Min(99, Math.Max(1, discountPercent.Value));
                    product.DiscountPrice = Math.Round(product.Price * (decimal)(1.0 - product.DiscountPercent.Value / 100.0));
                }
                else if (discountPrice.HasValue && discountPrice.Value > 0 && discountPrice.Value < product.Price)
                {
                    product.DiscountPrice = discountPrice.Value;
                    product.DiscountPercent = Math.Round((double)((product.Price - discountPrice.Value) / product.Price * 100m), 1);
                }

                product.DiscountStartDate = startDate;
                product.DiscountEndDate = endDate;
                product.IsDiscountActive = isActive;
                product.BannerId = bannerId > 0 ? bannerId : null;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật giảm giá cho {products.Count} sản phẩm thành công.";
            return RedirectToAction("Index");
        }

        [HttpPost("CreateCampaign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCampaign(
            [FromForm] string name,
            [FromForm] string? description,
            [FromForm] double discountPercent,
            [FromForm] int? categoryId,
            [FromForm] string? brand,
            [FromForm] DateTime startDate,
            [FromForm] DateTime endDate,
            [FromForm] int? bannerId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Tên chiến dịch không được để trống.";
                return RedirectToAction("Index");
            }

            if (discountPercent <= 0 || discountPercent >= 100)
            {
                TempData["ErrorMessage"] = "Tỷ lệ giảm giá phải từ 1% đến 99%.";
                return RedirectToAction("Index");
            }

            if (startDate >= endDate)
            {
                TempData["ErrorMessage"] = "Thời gian bắt đầu phải trước thời gian kết thúc.";
                return RedirectToAction("Index");
            }

            if (bannerId.HasValue && bannerId.Value > 0)
            {
                var existingLinkedCampaign = await _context.PromotionCampaigns.FirstOrDefaultAsync(c => c.BannerId == bannerId.Value);
                if (existingLinkedCampaign != null)
                {
                    existingLinkedCampaign.BannerId = null;
                }
            }

            var campaign = new PromotionCampaign
            {
                Name = name,
                Description = description,
                DiscountPercent = discountPercent,
                CategoryId = categoryId > 0 ? categoryId : null,
                Brand = !string.IsNullOrWhiteSpace(brand) ? brand : null,
                StartDate = startDate,
                EndDate = endDate,
                IsActive = true,
                CreatedAt = DateTime.Now,
                BannerId = bannerId > 0 ? bannerId : null
            };

            _context.PromotionCampaigns.Add(campaign);
            await _context.SaveChangesAsync();

            // Sync campaign to products
            await SyncCampaignProductsAsync(campaign);

            TempData["SuccessMessage"] = $"Đã tạo và kích hoạt chiến dịch '{campaign.Name}' thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost("ToggleCampaign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCampaign([FromForm] int campaignId)
        {
            var campaign = await _context.PromotionCampaigns.FirstOrDefaultAsync(c => c.PromotionCampaignId == campaignId);
            if (campaign == null) return NotFound();

            campaign.IsActive = !campaign.IsActive;
            await _context.SaveChangesAsync();

            if (campaign.IsActive)
            {
                await SyncCampaignProductsAsync(campaign);
            }
            else
            {
                await ClearCampaignProductsAsync(campaign);
            }

            return Json(new { success = true, isActive = campaign.IsActive });
        }

        [HttpPost("DeleteCampaign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCampaign([FromForm] int campaignId)
        {
            var campaign = await _context.PromotionCampaigns.FirstOrDefaultAsync(c => c.PromotionCampaignId == campaignId);
            if (campaign == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chiến dịch.";
                return RedirectToAction("Index");
            }

            // Clear products affected by this campaign
            await ClearCampaignProductsAsync(campaign);

            _context.PromotionCampaigns.Remove(campaign);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa chiến dịch '{campaign.Name}' và khôi phục giá gốc cho các sản phẩm liên quan.";
            return RedirectToAction("Index");
        }

        [HttpPost("EditCampaign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCampaign(
            [FromForm] int campaignId,
            [FromForm] string name,
            [FromForm] string? description,
            [FromForm] double discountPercent,
            [FromForm] int? categoryId,
            [FromForm] string? brand,
            [FromForm] DateTime startDate,
            [FromForm] DateTime endDate,
            [FromForm] int? bannerId)
        {
            var campaign = await _context.PromotionCampaigns.FirstOrDefaultAsync(c => c.PromotionCampaignId == campaignId);
            if (campaign == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chiến dịch.";
                return RedirectToAction("Index");
            }

            // Clear products affected by this campaign before updating properties
            await ClearCampaignProductsAsync(campaign);

            campaign.Name = name;
            campaign.Description = description;
            campaign.DiscountPercent = discountPercent;
            campaign.CategoryId = categoryId > 0 ? categoryId : null;
            campaign.Brand = !string.IsNullOrWhiteSpace(brand) ? brand : null;
            campaign.StartDate = startDate;
            campaign.EndDate = endDate;
            campaign.BannerId = bannerId > 0 ? bannerId : null;

            await _context.SaveChangesAsync();

            // Re-sync with updated criteria if active
            if (campaign.IsActive)
            {
                await SyncCampaignProductsAsync(campaign);
            }

            TempData["SuccessMessage"] = $"Đã cập nhật chiến dịch '{campaign.Name}' thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost("ToggleDiscount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDiscount([FromForm] int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null) return NotFound();

            product.IsDiscountActive = !product.IsDiscountActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = product.IsDiscountActive, isOnDiscount = product.IsOnDiscount });
        }

        [HttpPost("RemoveDiscount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDiscount([FromForm] int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null) return NotFound();

            product.DiscountPrice = null;
            product.DiscountPercent = null;
            product.DiscountStartDate = null;
            product.DiscountEndDate = null;
            product.IsDiscountActive = false;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã hủy chương trình giảm giá của sản phẩm {product.ProductName}.";
            return RedirectToAction("Index");
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

        private async Task SyncCampaignProductsAsync(PromotionCampaign campaign)
        {
            var query = _context.Products.AsQueryable();

            if (campaign.CategoryId.HasValue)
            {
                var catIds = GetCategoryAndDescendants(campaign.CategoryId.Value);
                query = query.Where(p => p.CategoryId.HasValue && catIds.Contains(p.CategoryId.Value));
            }

            if (!string.IsNullOrWhiteSpace(campaign.Brand))
            {
                query = query.Where(p => p.Brand == campaign.Brand);
            }

            var products = await query.ToListAsync();
            foreach (var p in products)
            {
                p.DiscountPercent = campaign.DiscountPercent;
                p.DiscountPrice = Math.Round(p.Price * (decimal)(1.0 - campaign.DiscountPercent / 100.0));
                p.DiscountStartDate = campaign.StartDate;
                p.DiscountEndDate = campaign.EndDate;
                p.IsDiscountActive = campaign.IsActive;
            }

            await _context.SaveChangesAsync();
        }

        private async Task ClearCampaignProductsAsync(PromotionCampaign campaign)
        {
            var query = _context.Products.AsQueryable();

            if (campaign.CategoryId.HasValue)
            {
                var catIds = GetCategoryAndDescendants(campaign.CategoryId.Value);
                query = query.Where(p => p.CategoryId.HasValue && catIds.Contains(p.CategoryId.Value));
            }

            if (!string.IsNullOrWhiteSpace(campaign.Brand))
            {
                query = query.Where(p => p.Brand == campaign.Brand);
            }

            // Sync down and match exact parameters of the campaign to reset
            var products = await query
                .Where(p => p.DiscountPercent == campaign.DiscountPercent)
                .ToListAsync();

            foreach (var p in products)
            {
                p.DiscountPrice = null;
                p.DiscountPercent = null;
                p.DiscountStartDate = null;
                p.DiscountEndDate = null;
                p.IsDiscountActive = false;
            }

            await _context.SaveChangesAsync();
        }
    }
}
