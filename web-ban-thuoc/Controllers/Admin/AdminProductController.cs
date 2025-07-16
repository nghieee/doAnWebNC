using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
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
            ViewBag.Categories = _context.Categories
                .Where(c => c.CategoryLevel == "2" || c.CategoryLevel == "3")
                .OrderBy(c => c.CategoryName)
                .ToList();
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
            ViewBag.Categories = _context.Categories.Where(c => c.CategoryLevel == "3").OrderBy(c => c.CategoryName).ToList();
            return View("~/Views/Admin/Product/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product, List<IFormFile> images)
        {
            if (ModelState.IsValid)
            {
                product.SoldQuantity = 0;
                _context.Products.Add(product);
                _context.SaveChanges();
                // Xử lý upload ảnh
                if (images != null && images.Count > 0)
                {
                    int sort = 1;
                    foreach (var file in images)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                file.CopyTo(stream);
                            }
                            var img = new ProductImage
                            {
                                ProductId = product.ProductId,
                                ImageUrl = fileName,
                                IsMain = (sort == 1),
                                SortOrder = sort
                            };
                            _context.ProductImages.Add(img);
                            sort++;
                        }
                    }
                    _context.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            ViewBag.Categories = _context.Categories.Where(c => c.CategoryLevel == "2" || c.CategoryLevel == "3").OrderBy(c => c.CategoryName).ToList();
            return View("~/Views/Admin/Product/Create.cshtml", product);
        }

        public IActionResult Edit(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();
            ViewBag.Categories = _context.Categories.Where(c => c.CategoryLevel == "3").OrderBy(c => c.CategoryName).ToList();
            return View("~/Views/Admin/Product/Edit.cshtml", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product product, List<IFormFile> images, int? mainImageId, int[] deleteImageIds)
        {
            if (ModelState.IsValid)
            {
                // Lấy lại giá trị SoldQuantity cũ từ DB để không bị null hoặc bị sửa
                var oldProduct = _context.Products.AsNoTracking().FirstOrDefault(p => p.ProductId == product.ProductId);
                if (oldProduct != null)
                {
                    product.SoldQuantity = oldProduct.SoldQuantity ?? 0;
                }
                _context.Products.Update(product);
                _context.SaveChanges();
                // Xóa ảnh cũ nếu chọn
                if (deleteImageIds != null && deleteImageIds.Length > 0)
                {
                    var imgs = _context.ProductImages.Where(i => deleteImageIds.Contains(i.ProductImageId)).ToList();
                    foreach (var img in imgs)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", img.ImageUrl);
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                        _context.ProductImages.Remove(img);
                    }
                    _context.SaveChanges();
                }
                // Upload ảnh mới
                if (images != null && images.Count > 0)
                {
                    int sort = _context.ProductImages.Count(i => i.ProductId == product.ProductId) + 1;
                    foreach (var file in images)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                file.CopyTo(stream);
                            }
                            var img = new ProductImage
                            {
                                ProductId = product.ProductId,
                                ImageUrl = fileName,
                                IsMain = false,
                                SortOrder = sort
                            };
                            _context.ProductImages.Add(img);
                            sort++;
                        }
                    }
                    _context.SaveChanges();
                }
                // Cập nhật ảnh đại diện
                var allImgs = _context.ProductImages.Where(i => i.ProductId == product.ProductId).ToList();
                foreach (var img in allImgs)
                {
                    img.IsMain = (mainImageId.HasValue && img.ProductImageId == mainImageId);
                }
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Categories = _context.Categories.Where(c => c.CategoryLevel == "2" || c.CategoryLevel == "3").OrderBy(c => c.CategoryName).ToList();
            return View("~/Views/Admin/Product/Edit.cshtml", product);
        }

        public IActionResult Delete(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();
            return View("~/Views/Admin/Product/Delete.cshtml", product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefault(p => p.ProductId == id);
            if (product != null)
            {
                // Xóa ảnh vật lý
                foreach (var img in product.ProductImages)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", img.ImageUrl);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                _context.ProductImages.RemoveRange(product.ProductImages);
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