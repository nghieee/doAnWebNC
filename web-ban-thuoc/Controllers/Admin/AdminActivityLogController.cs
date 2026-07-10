using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminActivityLogController : Controller
    {
        private readonly LongChauDbContext _context;

        public AdminActivityLogController(LongChauDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? actionType = null,
            string? entityName = null,
            string? search = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1)
        {
            // 1. Tự động dọn dẹp các log cũ hơn 90 ngày
            var ninetyDaysAgo = DateTime.Today.AddDays(-90);
            var oldLogs = _context.DbActivityLogs.Where(l => l.CreatedAt < ninetyDaysAgo);
            if (await oldLogs.AnyAsync())
            {
                _context.DbActivityLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();
            }

            // 2. Thiết lập truy vấn cơ bản - Ràng buộc cứng tối đa 90 ngày
            var query = _context.DbActivityLogs
                .Where(l => l.CreatedAt >= ninetyDaysAgo)
                .AsNoTracking();

            // 3. Áp dụng các bộ lọc
            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(l => l.Action == actionType);
            }

            if (!string.IsNullOrEmpty(entityName))
            {
                query = query.Where(l => l.EntityName == entityName);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(l => 
                    (l.Description != null && l.Description.ToLower().Contains(s)) ||
                    (l.UserEmail != null && l.UserEmail.ToLower().Contains(s)) ||
                    (l.EntityId != null && l.EntityId.Contains(s))
                );
            }

            if (startDate.HasValue)
            {
                // Tránh chọn ngày xa hơn 90 ngày
                var filterStart = startDate.Value < ninetyDaysAgo ? ninetyDaysAgo : startDate.Value;
                query = query.Where(l => l.CreatedAt >= filterStart);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.CreatedAt <= endOfDay);
            }

            // 4. Phân trang
            int pageSize = 20;
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 5. Chuẩn bị dữ liệu cho View
            ViewBag.ActionType = actionType;
            ViewBag.EntityName = entityName;
            ViewBag.Search = search;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Danh sách các danh mục thực thể để hiển thị trong bộ lọc
            ViewBag.EntityList = new[] { "Sản phẩm", "Danh mục sản phẩm", "Banner", "Voucher", "Nhà cung cấp", "Kho hàng", "Quà đổi điểm" };
            ViewBag.ActionList = new[] { "Thêm", "Sửa", "Xóa" };

            return View("~/Views/Admin/ActivityLog/Index.cshtml", logs);
        }
    }
}
