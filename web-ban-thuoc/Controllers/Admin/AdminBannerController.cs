using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("AdminBanner")]
public class AdminBannerController : Controller
{
    private readonly LongChauDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AdminBannerController(LongChauDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: AdminBanner
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index(string? searchTerm, string? typeFilter, string? statusFilter)
    {
        var query = _context.Banners.AsQueryable();

        // Filter theo search term
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(b => 
                (b.Title != null && b.Title.Contains(searchTerm)) ||
                (b.Description != null && b.Description.Contains(searchTerm))
            );
        }

        // Filter theo loại banner
        if (!string.IsNullOrEmpty(typeFilter))
        {
            query = query.Where(b => b.BannerType == typeFilter);
        }

        // Filter theo trạng thái
        if (!string.IsNullOrEmpty(statusFilter))
        {
            if (statusFilter == "active")
            {
                query = query.Where(b => b.IsActive);
            }
            else if (statusFilter == "inactive")
            {
                query = query.Where(b => !b.IsActive);
            }
        }

        var banners = await query
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.CreatedAt)
            .ToListAsync();

        // Pass filter values to view
        ViewBag.SearchTerm = searchTerm;
        ViewBag.TypeFilter = typeFilter;
        ViewBag.StatusFilter = statusFilter;

        return View("~/Views/Admin/Banner/Index.cshtml", banners);
    }

    // GET: AdminBanner/Create
    [Route("Create")]
    public IActionResult Create()
    {
        return View("~/Views/Admin/Banner/Create.cshtml");
    }

    // POST: AdminBanner/Create
    [HttpPost]
    [Route("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,ImageUrl,LinkUrl,BannerType,SortOrder,IsActive")] Banner banner, IFormFile? imageFile)
    {
        // Validate file upload
        if (imageFile == null || imageFile.Length == 0)
        {
            ModelState.AddModelError("imageFile", "Vui lòng chọn ảnh banner");
        }
        else
        {
            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh: JPG, PNG, GIF");
            }

            // Validate file size (max 5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("imageFile", "Kích thước file không được quá 5MB");
            }
        }
        
        if (ModelState.IsValid)
        {
            try
            {
                // Xử lý upload ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "banners");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    banner.ImageUrl = "/images/banners/" + uniqueFileName;
                }
                else
                {
                    // Nếu không upload ảnh, set default image
                    banner.ImageUrl = "/images/banners/default.png";
                }

                banner.CreatedAt = DateTime.Now;
                _context.Add(banner);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Tạo banner thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo banner: " + ex.Message);
            }
        }
        
        return View("~/Views/Admin/Banner/Create.cshtml", banner);
    }

    // GET: AdminBanner/Edit/5
    [Route("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var banner = await _context.Banners.FindAsync(id);
        if (banner == null)
        {
            return NotFound();
        }
        return View("~/Views/Admin/Banner/Edit.cshtml", banner);
    }

    // POST: AdminBanner/Edit/5
    [HttpPost]
    [Route("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("BannerId,Title,Description,LinkUrl,BannerType,SortOrder,IsActive,CreatedAt")] Banner banner, IFormFile? imageFile)
    {
        if (id != banner.BannerId)
        {
            return NotFound();
        }

        // Lấy banner hiện tại từ database để giữ nguyên ImageUrl
        var existingBanner = await _context.Banners.FindAsync(id);
        if (existingBanner == null)
        {
            return NotFound();
        }

        // Validate file upload if provided
        if (imageFile != null && imageFile.Length > 0)
        {
            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh: JPG, PNG, GIF");
            }

            // Validate file size (max 5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("imageFile", "Kích thước file không được quá 5MB");
            }
        }

        // Validate LinkUrl nếu có
        if (!string.IsNullOrEmpty(banner.LinkUrl) && !Uri.TryCreate(banner.LinkUrl, UriKind.Absolute, out _))
        {
            ModelState.AddModelError("LinkUrl", "Link không đúng định dạng");
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Cập nhật từng field thay vì update toàn bộ object
                existingBanner.Title = banner.Title;
                existingBanner.Description = banner.Description;
                existingBanner.LinkUrl = banner.LinkUrl;
                existingBanner.BannerType = banner.BannerType;
                existingBanner.SortOrder = banner.SortOrder;
                existingBanner.IsActive = banner.IsActive;
                
                // Debug: Log để kiểm tra
                Console.WriteLine($"Original ImageUrl: {existingBanner.ImageUrl}");

                // Xử lý upload ảnh mới nếu có
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "banners");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    existingBanner.ImageUrl = "/images/banners/" + uniqueFileName;
                    Console.WriteLine($"New ImageUrl after upload: {existingBanner.ImageUrl}");
                }

                existingBanner.UpdatedAt = DateTime.Now;
                
                // Debug: Kiểm tra trước khi save
                Console.WriteLine($"Final ImageUrl before save: {existingBanner.ImageUrl}");
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Cập nhật banner thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BannerExists(banner.BannerId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật banner: " + ex.Message);
            }
        }
        else
        {
            // Debug: Log ModelState errors
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"ModelState Error: {error.ErrorMessage}");
            }
        }
        return View("~/Views/Admin/Banner/Edit.cshtml", banner);
    }

    // GET: AdminBanner/Delete/5
    [Route("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var banner = await _context.Banners
            .FirstOrDefaultAsync(m => m.BannerId == id);
        if (banner == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Banner/Delete.cshtml", banner);
    }

    // POST: AdminBanner/Delete/5
    [HttpPost]
    [Route("Delete/{id}")]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                // Xóa file ảnh nếu không phải default
                if (!string.IsNullOrEmpty(banner.ImageUrl) && 
                    !banner.ImageUrl.Contains("default.png") &&
                    banner.ImageUrl.StartsWith("/images/banners/"))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, banner.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Xóa banner thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy banner để xóa!";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa banner: " + ex.Message;
        }
        
        return RedirectToAction(nameof(Index));
    }

    // GET: AdminBanner/Details/5
    [Route("Details/{id}")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var banner = await _context.Banners
            .FirstOrDefaultAsync(m => m.BannerId == id);
        if (banner == null)
        {
            return NotFound();
        }

        return PartialView("~/Views/Admin/Banner/Details.cshtml", banner);
    }

    // POST: AdminBanner/BulkDelete
    [HttpPost]
    [Route("BulkDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelete(int[] selectedIds)
    {
        if (selectedIds == null || selectedIds.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn banner để xóa!";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var bannersToDelete = await _context.Banners
                .Where(b => selectedIds.Contains(b.BannerId))
                .ToListAsync();

            foreach (var banner in bannersToDelete)
            {
                // Xóa file ảnh nếu không phải default
                if (!string.IsNullOrEmpty(banner.ImageUrl) && 
                    !banner.ImageUrl.Contains("default.png") &&
                    banner.ImageUrl.StartsWith("/images/banners/"))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, banner.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
            }

            _context.Banners.RemoveRange(bannersToDelete);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Đã xóa {bannersToDelete.Count} banner thành công!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa banner: " + ex.Message;
        }
        
        return RedirectToAction(nameof(Index));
    }

    private bool BannerExists(int id)
    {
        return _context.Banners.Any(e => e.BannerId == id);
    }
} 