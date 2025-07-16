using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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

        public IActionResult Index(int? parentId1 = null, int? parentId2 = null, int page = 1, string? isFeature = null)
        {
            int pageSize = 12;
            var categories = _context.Categories
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.CategoryLevel)
                .ThenBy(c => c.CategoryName)
                .AsQueryable();
            // Lọc theo cấp 1
            if (parentId1.HasValue)
                categories = categories.Where(c => c.ParentCategoryId == parentId1 || c.CategoryId == parentId1);
            // Lọc theo cấp 2
            if (parentId2.HasValue)
                categories = categories.Where(c => c.ParentCategoryId == parentId2 || c.CategoryId == parentId2);
            // Lọc theo danh mục nổi bật
            if (!string.IsNullOrEmpty(isFeature))
            {
                if (isFeature == "true")
                    categories = categories.Where(c => c.IsFeature);
                else if (isFeature == "false")
                    categories = categories.Where(c => !c.IsFeature);
            }
            // Danh sách danh mục cấp 1 cho filter
            var parentCategories1 = _context.Categories.Where(c => c.CategoryLevel == "1").OrderBy(c => c.CategoryName).ToList();
            // Danh sách danh mục cấp 2 cho filter (theo cấp 1 nếu có)
            List<Category> parentCategories2;
            if (parentId1.HasValue)
                parentCategories2 = _context.Categories.Where(c => c.CategoryLevel == "2" && c.ParentCategoryId == parentId1).OrderBy(c => c.CategoryName).ToList();
            else
                parentCategories2 = new List<Category>();
            int totalCategories = categories.Count();
            int totalPages = (int)Math.Ceiling((double)totalCategories / pageSize);
            var pagedCategories = categories.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.ParentCategories1 = parentCategories1;
            ViewBag.ParentCategories2 = parentCategories2;
            ViewBag.SelectedParentId1 = parentId1;
            ViewBag.SelectedParentId2 = parentId2;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SelectedIsFeature = isFeature;
            return View("~/Views/Admin/Category/Index.cshtml", pagedCategories);
        }

        public IActionResult Create()
        {
            ViewBag.ParentCategories1 = _context.Categories.Where(c => c.CategoryLevel == "1").OrderBy(c => c.CategoryName).ToList();
            ViewBag.ParentCategories2 = _context.Categories.Where(c => c.CategoryLevel == "2").OrderBy(c => c.CategoryName).ToList();
            return View("~/Views/Admin/Category/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category, IFormFile? image)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh nếu có
                if (image != null && image.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/categories", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        image.CopyTo(stream);
                    }
                    category.ImageUrl = fileName;
                }
                _context.Categories.Add(category);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ParentCategories1 = _context.Categories.Where(c => c.CategoryLevel == "1").OrderBy(c => c.CategoryName).ToList();
            ViewBag.ParentCategories2 = _context.Categories.Where(c => c.CategoryLevel == "2").OrderBy(c => c.CategoryName).ToList();
            return View("~/Views/Admin/Category/Create.cshtml", category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkDelete(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                var categories = _context.Categories.Where(c => selectedIds.Contains(c.CategoryId)).ToList();
                _context.Categories.RemoveRange(categories);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
} 