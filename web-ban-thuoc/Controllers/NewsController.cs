using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers;

public class NewsController : Controller
{
    private readonly LongChauDbContext _context;

    public NewsController(LongChauDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(int page = 1)
    {
        int pageSize = 12;
        var query = _context.News
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.PublishedAt ?? n.CreatedAt);

        var totalItems = query.Count();
        var news = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.PageTitle = "Góc sức khỏe";

        return View(news);
    }

    public IActionResult Details(int id)
    {
        var news = _context.News.FirstOrDefault(n => n.NewsId == id);
        if (news == null)
            return NotFound();

        news.ViewCount++;
        _context.SaveChanges();

        var related = _context.News
            .Where(n => n.IsPublished && n.NewsId != id && n.Category == news.Category)
            .OrderByDescending(n => n.PublishedAt ?? n.CreatedAt)
            .Take(3)
            .ToList();

        ViewBag.RelatedNews = related;
        return View(news);
    }

    public IActionResult Category(string category, int page = 1)
    {
        int pageSize = 12;
        var query = _context.News
            .Where(n => n.IsPublished && n.Category == category)
            .OrderByDescending(n => n.PublishedAt ?? n.CreatedAt);

        var totalItems = query.Count();
        var news = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.Category = category;
        ViewBag.PageTitle = category ?? "Góc sức khỏe";

        return View("Index", news);
    }
}
