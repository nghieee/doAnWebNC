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
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TodayOrders = todayOrders;
            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.ChartYear = year;
            return View("~/Views/Admin/Index.cshtml");
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