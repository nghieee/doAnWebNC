using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminCategoryController : Controller
    {
        private readonly LongChauDbContext _context;
        public AdminCategoryController(LongChauDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.Products)
                .OrderBy(c => c.CategoryLevel)
                .ThenBy(c => c.CategoryName)
                .ToList();
            return View("~/Views/Admin/Category/Index.cshtml", categories);
        }
    }
} 