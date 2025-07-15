using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminProductController : Controller
    {
        private readonly LongChauDbContext _context;
        public AdminProductController(LongChauDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int page = 1, int? categoryId = null, string priceRange = null, string origin = null, string search = null)
        {
            int pageSize = 12;
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            // Lọc theo danh mục
            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(p => p.CategoryId == categoryId);

            // Lọc theo giá
            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "1": query = query.Where(p => p.Price < 200000); break;
                    case "2": query = query.Where(p => p.Price >= 200000 && p.Price <= 500000); break;
                    case "3": query = query.Where(p => p.Price > 500000); break;
                }
            }

            // Lọc theo nguồn gốc
            if (!string.IsNullOrEmpty(origin))
                query = query.Where(p => p.Origin == origin);

            // Lọc theo tên sản phẩm
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search));

            int totalProducts = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            var products = query.OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Truyền dữ liệu filter cho view
            ViewBag.Categories = _context.Categories.OrderBy(c => c.CategoryName).ToList();
            ViewBag.Origins = _context.Products.Where(p => !string.IsNullOrEmpty(p.Origin)).Select(p => p.Origin).Distinct().OrderBy(o => o).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SelectedPrice = priceRange;
            ViewBag.SelectedOrigin = origin;
            ViewBag.Search = search;
            return View("~/Views/Admin/Product/Index.cshtml", products);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();
            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Update(product);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Action trả về partial view chi tiết sản phẩm cho modal Quick View
        public IActionResult GetProductDetail(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();
            return PartialView("~/Views/Admin/Product/_ProductDetailPartial.cshtml", product);
        }
    }
} 