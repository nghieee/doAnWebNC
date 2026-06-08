using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using web_ban_thuoc.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace web_ban_thuoc.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminHomeController : Controller
    {
        private readonly LongChauDbContext _context;
        public AdminHomeController(LongChauDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.Where(o => o.Status == OrderStatuses.Delivered).SumAsync(o => o.TotalAmount ?? 0);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatuses.PendingConfirmation);
            var unreadMessages = await _context.ChatMessages.CountAsync(m => m.ReceiverId == "admin" && !m.IsRead);
            var totalProducts = await _context.Products.CountAsync();
            var totalCustomers = await _context.Users.CountAsync();
            var today = DateTime.Today;
            var todayOrders = await _context.Orders.CountAsync(o => o.OrderDate >= today);
            var todayRevenue = await _context.Orders.Where(o => o.OrderDate >= today && o.Status == OrderStatuses.Delivered).SumAsync(o => o.TotalAmount ?? 0);
            // Doanh thu theo tháng trong năm hiện tại
            int year = DateTime.Now.Year;
            var monthlyRevenue = new List<decimal>();
            for (int month = 1; month <= 12; month++)
            {
                var start = new DateTime(year, month, 1);
                var end = (month < 12) ? new DateTime(year, month + 1, 1) : new DateTime(year + 1, 1, 1);
                var revenue = await _context.Orders
                    .Where(o => o.Status == OrderStatuses.Delivered && o.OrderDate >= start && o.OrderDate < end)
                    .SumAsync(o => o.TotalAmount ?? 0);
                monthlyRevenue.Add(revenue);
            }
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.UnreadMessages = unreadMessages;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TodayOrders = todayOrders;
            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.ChartYear = year;
            return View("~/Views/Admin/Index.cshtml");
        }

        public async Task<IActionResult> Voucher()
        {
            var vouchers = await _context.Vouchers.ToListAsync();
            var userVouchers = await _context.UserVouchers.ToListAsync();
            var users = await _context.Users.ToListAsync();
            var categories = await _context.Categories.ToDictionaryAsync(c => c.CategoryId, c => c.CategoryName);

            var voucherList = vouchers.Select(v =>
            {
                var related = userVouchers.Where(uv => uv.VoucherId == v.VoucherId).ToList();
                string userEmail = v.IsPublic ? "Mã dùng chung (mọi user)" : "Gán theo user";
                if (!v.IsPublic && related.Count > 0)
                {
                    userEmail = string.Join(", ", related
                        .Select(uv => users.FirstOrDefault(u => u.Id == uv.UserId)?.Email ?? uv.UserId)
                        .Distinct());
                }
                int conLai = v.MaxUsage.HasValue
                    ? Math.Max(0, v.MaxUsage.Value - v.UsedCount)
                    : -1;

                return new VoucherAdminViewModel
                {
                    VoucherId = v.VoucherId,
                    Code = v.Code,
                    Description = v.Description,
                    DiscountType = v.DiscountType,
                    PercentValue = v.PercentValue,
                    DiscountAmount = v.DiscountAmount,
                    ExpiryDate = v.ExpiryDate,
                    UserId = userEmail,
                    DaDung = v.IsPublic ? v.UsedCount : related.Count(uv => uv.IsUsed),
                    ConLai = conLai,
                    IsActive = v.IsActive,
                    IsPublic = v.IsPublic,
                    MinOrderAmount = v.MinOrderAmount,
                    RequiredRank = v.RequiredRank,
                    CategoryId = v.CategoryId,
                    CategoryName = v.CategoryId.HasValue && categories.ContainsKey(v.CategoryId.Value)
                        ? categories[v.CategoryId.Value] : v.CategoryName
                };
            }).ToList();

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();
            return View("~/Views/Admin/Voucher/Index.cshtml", voucherList);
        }

        [HttpGet]
        public async Task<IActionResult> GetVoucher(string code)
        {
            var v = await _context.Vouchers.FirstOrDefaultAsync(x => x.Code == code);
            if (v == null)
                return Json(new { success = false, message = "Không tìm thấy voucher!" });

            return Json(new
            {
                success = true,
                data = new
                {
                    v.Code,
                    v.Description,
                    v.DiscountType,
                    v.PercentValue,
                    v.DiscountAmount,
                    ExpiryDate = v.ExpiryDate.ToString("yyyy-MM-dd"),
                    v.Detail,
                    v.MaxUsage,
                    v.MinOrderAmount,
                    v.RequiredRank,
                    v.CategoryId,
                    v.IsActive
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateVoucher([FromForm] VoucherEditModel model)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == model.Code);
            if (voucher == null)
                return Json(new { success = false, message = "Không tìm thấy voucher!" });

            if (voucher.UsedCount > 0 && model.DiscountType != voucher.DiscountType)
                return Json(new { success = false, message = "Voucher đã được dùng — không đổi loại giảm giá." });

            voucher.Description = model.Description;
            voucher.DiscountType = model.DiscountType;
            voucher.PercentValue = model.PercentValue;
            voucher.DiscountAmount = model.DiscountAmount;
            voucher.ExpiryDate = model.ExpiryDate;
            voucher.Detail = model.Detail;
            voucher.MaxUsage = model.MaxUsage;
            voucher.MinOrderAmount = model.MinOrderAmount;
            voucher.RequiredRank = string.IsNullOrWhiteSpace(model.RequiredRank) ? null : model.RequiredRank;
            voucher.CategoryId = model.CategoryId;
            voucher.IsActive = model.IsActive;

            if (model.CategoryId.HasValue)
            {
                var cat = await _context.Categories.FindAsync(model.CategoryId.Value);
                voucher.CategoryName = cat?.CategoryName;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVoucherActive(string code)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
            if (voucher == null)
                return Json(new { success = false, message = "Không tìm thấy voucher!" });

            voucher.IsActive = !voucher.IsActive;
            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = voucher.IsActive });
        }

        [HttpGet]
        public async Task<IActionResult> VoucherRedemptions(string code)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
            if (voucher == null)
                return NotFound();

            var redemptions = await _context.VoucherRedemptions
                .Where(r => r.VoucherId == voucher.VoucherId && !r.IsReverted)
                .OrderByDescending(r => r.RedeemedAt)
                .ToListAsync();

            var userIds = redemptions.Select(r => r.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? u.Id);

            ViewBag.Voucher = voucher;
            ViewBag.Users = users;
            return View("~/Views/Admin/Voucher/Redemptions.cshtml", redemptions);
        }

        [HttpPost]
        public async Task<IActionResult> CreateVoucher([FromForm] VoucherCreateModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Description) || model.ExpiryDate == null)
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin!" });
            if ((model.PercentValue.HasValue && model.PercentValue > 0 && model.DiscountAmount.HasValue && model.DiscountAmount > 0) ||
                (!model.PercentValue.HasValue && !model.DiscountAmount.HasValue))
                return Json(new { success = false, message = "Chỉ nhập % giảm hoặc số tiền giảm!" });

            List<string> userIds = new();
            if (model.ForAllUsers)
            {
                userIds = await _context.Users.Select(u => u.Id).ToListAsync();
            }
            else if (model.Ranks != null && model.Ranks.Any())
            {
                userIds = await _context.UserRankInfos.Where(u => model.Ranks.Contains(u.Rank)).Select(u => u.UserId).ToListAsync();
            }

            if (await _context.Vouchers.AnyAsync(v => v.Code == model.Code))
                return Json(new { success = false, message = "Mã voucher đã tồn tại!" });

            var isPublic = model.IsPublicCode || userIds.Count == 0;
            string? categoryName = null;
            if (model.CategoryId.HasValue)
            {
                categoryName = await _context.Categories
                    .Where(c => c.CategoryId == model.CategoryId)
                    .Select(c => c.CategoryName)
                    .FirstOrDefaultAsync();
            }

            var voucher = new Voucher
            {
                Code = model.Code,
                Description = model.Description,
                DiscountType = model.DiscountType,
                PercentValue = model.PercentValue,
                DiscountAmount = model.DiscountAmount,
                ExpiryDate = model.ExpiryDate,
                Detail = model.Detail,
                IsActive = true,
                MaxUsage = model.MaxUsage,
                IsPublic = isPublic,
                MinOrderAmount = model.MinOrderAmount,
                RequiredRank = string.IsNullOrWhiteSpace(model.RequiredRank) ? null : model.RequiredRank,
                CategoryId = model.CategoryId,
                CategoryName = categoryName
            };
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            foreach (var userId in userIds.Distinct())
            {
                if (await _context.UserVouchers.AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucher.VoucherId))
                    continue;

                _context.UserVouchers.Add(new UserVoucher
                {
                    UserId = userId,
                    VoucherId = voucher.VoucherId,
                    IsUsed = false,
                    IsNew = true
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVoucher(string code)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
            if (voucher == null)
                return Json(new { success = false, message = "Không tìm thấy voucher!" });
            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport()
        {
            int year = DateTime.Now.Year;
            var sb = new StringBuilder();
            sb.AppendLine("Tháng,Doanh thu,Đơn đã giao,Đơn chờ xác nhận,Đơn đã hủy,Sản phẩm bán ra");
            var startYear = new DateTime(year, 1, 1);
            var endYear = new DateTime(year + 1, 1, 1);
            // Lấy toàn bộ đơn đã giao trong năm
            var ordersGiao = await _context.Orders
                .Where(o => o.Status == OrderStatuses.Delivered && o.OrderDate >= startYear && o.OrderDate < endYear)
                .Select(o => new { o.OrderId, o.OrderDate, o.TotalAmount, o.FullName })
                .ToListAsync();
            var orderIdGiao = ordersGiao.Select(o => o.OrderId).ToList();
            // Lấy toàn bộ OrderItem của đơn đã giao trong năm (theo OrderId)
            var orderItemData = await _context.OrderItems
                .Where(oi => oi.OrderId.HasValue && orderIdGiao.Contains(oi.OrderId.Value))
                .Select(oi => new { oi.OrderId, oi.Quantity })
                .ToListAsync();
            // Lấy toàn bộ đơn chờ xác nhận và đã hủy trong năm
            var ordersCho = await _context.Orders
                .Where(o => o.Status == OrderStatuses.PendingConfirmation && o.OrderDate >= startYear && o.OrderDate < endYear)
                .Select(o => new { o.OrderDate })
                .ToListAsync();
            var ordersHuy = await _context.Orders
                .Where(o => o.Status == OrderStatuses.Cancelled && o.OrderDate >= startYear && o.OrderDate < endYear)
                .Select(o => new { o.OrderDate })
                .ToListAsync();
            for (int month = 1; month <= 12; month++)
            {
                var start = new DateTime(year, month, 1);
                var end = (month < 12) ? new DateTime(year, month + 1, 1) : new DateTime(year + 1, 1, 1);
                var giaoThang = ordersGiao.Where(o => o.OrderDate >= start && o.OrderDate < end).ToList();
                var revenue = giaoThang.Sum(o => o.TotalAmount ?? 0);
                var countGiao = giaoThang.Count;
                var countCho = ordersCho.Count(o => o.OrderDate >= start && o.OrderDate < end);
                var countHuy = ordersHuy.Count(o => o.OrderDate >= start && o.OrderDate < end);
                // Sản phẩm bán ra: tổng quantity của OrderItem thuộc các đơn đã giao trong tháng
                var orderIdsThang = giaoThang.Select(o => o.OrderId).ToHashSet();
                var productSold = orderItemData.Where(oi => oi.OrderId.HasValue && orderIdsThang.Contains(oi.OrderId.Value)).Sum(oi => oi.Quantity);
                sb.AppendLine($"{month},{revenue},{countGiao},{countCho},{countHuy},{productSold}");
                // Thêm chi tiết đơn hàng từng tháng
                if (giaoThang.Count > 0)
                {
                    sb.AppendLine($"Chi tiết đơn hàng tháng {month}");
                    sb.AppendLine("Mã đơn,Ngày,Khách hàng,Tổng tiền,Số sản phẩm");
                    foreach (var order in giaoThang.OrderBy(o => o.OrderDate))
                    {
                        var itemCount = orderItemData.Where(oi => oi.OrderId == order.OrderId).Sum(oi => oi.Quantity);
                        sb.AppendLine($"{order.OrderId},{order.OrderDate:dd/MM/yyyy},{order.FullName},{order.TotalAmount},{itemCount}");
                    }
                }
            }
            // Thêm BOM UTF-8 để Excel nhận đúng font
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var bytes = bom.Concat(csvBytes).ToArray();
            return File(bytes, "text/csv", $"BaoCaoDoanhThu_{year}.csv");
        }
    }
} 