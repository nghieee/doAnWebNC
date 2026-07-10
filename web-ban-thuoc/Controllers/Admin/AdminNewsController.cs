using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin;

[Authorize(Roles = "Admin")]
public class AdminNewsController : Controller
{
    private readonly LongChauDbContext _context;
    private readonly IWebHostEnvironment _env;

    public AdminNewsController(LongChauDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public IActionResult Index(string? search, string? category, int page = 1)
    {
        int pageSize = 15;
        var query = _context.News.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(n => n.Title.Contains(search) || (n.Summary != null && n.Summary.Contains(search)));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(n => n.Category == category);

        var totalItems = query.Count();
        var news = query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.Categories = _context.News
            .Where(n => n.Category != null)
            .Select(n => n.Category)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        return View("~/Views/Admin/AdminNews/Index.cshtml", news);
    }

    public IActionResult Create()
    {
        return View("~/Views/Admin/AdminNews/Create.cshtml", new News());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(News news, IFormFile? CoverImage)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/AdminNews/Create.cshtml", news);

        news.Slug = GenerateSlug(news.Title);
        news.CreatedAt = DateTime.Now;
        if (news.IsPublished && !news.PublishedAt.HasValue)
            news.PublishedAt = DateTime.Now;

        if (CoverImage != null && CoverImage.Length > 0)
        {
            news.ImageUrl = SaveUpload(CoverImage, "news");
        }

        _context.News.Add(news);
        _context.SaveChanges();

        TempData["Success"] = "Bài viết đã được tạo thành công.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var news = _context.News.Find(id);
        if (news == null)
            return NotFound();
        return View("~/Views/Admin/AdminNews/Edit.cshtml", news);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(News news, IFormFile? CoverImage, bool RemoveImage = false)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/AdminNews/Edit.cshtml", news);

        var existing = _context.News.Find(news.NewsId);
        if (existing == null)
            return NotFound();

        existing.Title = news.Title;
        existing.Slug = GenerateSlug(news.Title);
        existing.Summary = news.Summary;
        existing.Content = news.Content;
        existing.Category = news.Category;
        existing.IsFeature = news.IsFeature;
        existing.IsPublished = news.IsPublished;
        existing.Author = news.Author;
        existing.Source = news.Source;

        if (news.IsPublished && !existing.PublishedAt.HasValue)
            existing.PublishedAt = DateTime.Now;

        existing.UpdatedAt = DateTime.Now;

        if (RemoveImage)
        {
            existing.ImageUrl = null;
        }
        else if (CoverImage != null && CoverImage.Length > 0)
        {
            existing.ImageUrl = SaveUpload(CoverImage, "news");
        }

        _context.SaveChanges();

        TempData["Success"] = "Bài viết đã được cập nhật.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var news = _context.News.Find(id);
        if (news == null)
            return NotFound();

        _context.News.Remove(news);
        _context.SaveChanges();

        TempData["Success"] = "Bài viết đã được xóa.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult ToggleFeature(int id)
    {
        var news = _context.News.Find(id);
        if (news == null)
            return NotFound();

        news.IsFeature = !news.IsFeature;
        _context.SaveChanges();

        return Json(new { success = true, isFeature = news.IsFeature });
    }

    public IActionResult TogglePublish(int id)
    {
        var news = _context.News.Find(id);
        if (news == null)
            return NotFound();

        news.IsPublished = !news.IsPublished;
        if (news.IsPublished && !news.PublishedAt.HasValue)
            news.PublishedAt = DateTime.Now;

        _context.SaveChanges();

        return Json(new { success = true, isPublished = news.IsPublished });
    }

    private string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";

        var slug = title.ToLowerInvariant().Trim();
        slug = slug.Replace("đ", "d").Replace("Đ", "d");

        var sb = new System.Text.StringBuilder();
        foreach (char c in slug)
        {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                sb.Append(c);
            else if (c == ' ' || c == '-' || c == '_')
                sb.Append('-');
        }

        slug = sb.ToString();
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    private string SaveUpload(IFormFile file, string folder)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "images", folder);
        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!allowedExts.Contains(ext)) ext = ".jpg";

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        using var stream = new FileStream(fullPath, FileMode.Create);
        file.CopyTo(stream);

        return $"/images/{folder}/{fileName}";
    }
}
