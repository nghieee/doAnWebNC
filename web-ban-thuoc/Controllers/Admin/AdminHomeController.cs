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
            var totalRevenue = await _context.Orders.Where(o => o.Status == "Đã giao").SumAsync(o => o.TotalAmount ?? 0);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Chờ xác nhận");
            var unreadMessages = await _context.ChatMessages.CountAsync(m => m.ReceiverId == "admin" && !m.IsRead);
            var totalProducts = await _context.Products.CountAsync();
            var totalCustomers = await _context.Users.CountAsync();
            var today = DateTime.Today;
            var todayOrders = await _context.Orders.CountAsync(o => o.OrderDate >= today);
            var todayRevenue = await _context.Orders.Where(o => o.OrderDate >= today && o.Status == "Đã giao").SumAsync(o => o.TotalAmount ?? 0);
            // Doanh thu theo tháng trong năm hiện tại
            int year = DateTime.Now.Year;
            var monthlyRevenue = new List<decimal>();
            for (int month = 1; month <= 12; month++)
            {
                var start = new DateTime(year, month, 1);
                var end = (month < 12) ? new DateTime(year, month + 1, 1) : new DateTime(year + 1, 1, 1);
                var revenue = await _context.Orders
                    .Where(o => o.Status == "Đã giao" && o.OrderDate >= start && o.OrderDate < end)
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
            // Lấy tất cả voucher và uservoucher liên quan
            var vouchers = await _context.Vouchers.ToListAsync();
            var userVouchers = await _context.UserVouchers.ToListAsync();
            var users = await _context.Users.ToListAsync();
            // Lấy danh sách email cho từng voucher
            var voucherList = vouchers.Select(v => {
                var relatedUserVouchers = userVouchers.Where(uv => uv.VoucherId == v.VoucherId).ToList();
                string userEmail = "Tất cả";
                if (relatedUserVouchers.Count > 0)
                {
                    var emails = relatedUserVouchers
                        .Select(uv => users.FirstOrDefault(u => u.Id == uv.UserId)?.Email ?? uv.UserId)
                        .Distinct();
                    userEmail = string.Join(", ", emails);
                }
                int daDung = relatedUserVouchers.Count(uv => uv.IsUsed);
                int conLai;
                if (v.MaxUsage.HasValue)
                {
                    conLai = v.MaxUsage.Value - v.UsedCount;
                    if (conLai < 0) conLai = 0;
                }
                else
                {
                    conLai = -1; // Không giới hạn
                }
                return new VoucherAdminViewModel {
                    Code = v.Code,
                    Description = v.Description,
                    DiscountType = v.DiscountType,
                    PercentValue = v.PercentValue,
                    DiscountAmount = v.DiscountAmount,
                    ExpiryDate = v.ExpiryDate,
                    UserId = userEmail,
                    DaDung = v.UsedCount,
                    ConLai = conLai,
                    IsActive = v.IsActive,
                };
            }).ToList();
            return View("~/Views/Admin/Voucher/Index.cshtml", voucherList);
        }

        [HttpPost]
        public async Task<IActionResult> CreateVoucher([FromForm] VoucherCreateModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Description) || model.ExpiryDate == null)
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin!" });
            if ((model.PercentValue.HasValue && model.PercentValue > 0 && model.DiscountAmount.HasValue && model.DiscountAmount > 0) ||
                (!model.PercentValue.HasValue && !model.DiscountAmount.HasValue))
                return Json(new { success = false, message = "Chỉ nhập % giảm hoặc số tiền giảm!" });
            // Kiểm tra mã voucher trùng
            if (await _context.Vouchers.AnyAsync(v => v.Code == model.Code))
                return Json(new { success = false, message = "Mã voucher đã tồn tại!" });
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
                MaxUsage = model.MaxUsage
            };
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
            // Phát cho user
            List<string> userIds = new();
            if (model.ForAllUsers)
            {
                userIds = await _context.Users.Select(u => u.Id).ToListAsync();
            }
            else if (model.Ranks != null && model.Ranks.Any())
            {
                var rankUsers = await _context.UserRankInfos.Where(u => model.Ranks.Contains(u.Rank)).Select(u => u.UserId).ToListAsync();
                userIds = rankUsers;
            }
            // Nếu có user cụ thể thì phát, còn không thì chỉ tạo voucher chung
            foreach (var userId in userIds.Distinct())
            {
                var userVoucher = new UserVoucher
                {
                    UserId = userId,
                    VoucherId = voucher.VoucherId,
                    IsUsed = false,
                    IsNew = true // Đánh dấu là voucher mới tặng
                };
                _context.UserVouchers.Add(userVoucher);
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
            // Xóa các UserVoucher liên quan
            var userVouchers = _context.UserVouchers.Where(uv => uv.VoucherId == voucher.VoucherId);
            _context.UserVouchers.RemoveRange(userVouchers);
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
                .Where(o => o.Status == "Đã giao" && o.OrderDate >= startYear && o.OrderDate < endYear)
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
                .Where(o => o.Status == "Chờ xác nhận" && o.OrderDate >= startYear && o.OrderDate < endYear)
                .Select(o => new { o.OrderDate })
                .ToListAsync();
            var ordersHuy = await _context.Orders
                .Where(o => o.Status == "Đã hủy" && o.OrderDate >= startYear && o.OrderDate < endYear)
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