using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin;

[Authorize(Roles = StaffRoles.Admin)]
[Route("AdminReport")]
public class AdminReportController : Controller
{
    private readonly LongChauDbContext _context;

    public AdminReportController(LongChauDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var yearStart = new DateTime(today.Year, 1, 1);

        var delivered = _context.Orders.Where(o => o.Status == OrderStatuses.Delivered);

        ViewBag.RevenueToday = await delivered.Where(o => o.OrderDate >= today).SumAsync(o => o.TotalAmount ?? 0);
        ViewBag.RevenueMonth = await delivered.Where(o => o.OrderDate >= monthStart).SumAsync(o => o.TotalAmount ?? 0);
        ViewBag.RevenueYear = await delivered.Where(o => o.OrderDate >= yearStart).SumAsync(o => o.TotalAmount ?? 0);

        ViewBag.OrdersByStatus = await _context.Orders
            .GroupBy(o => o.Status ?? "Không xác định")
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        ViewBag.TopProducts = await (
            from oi in _context.OrderItems
            join o in _context.Orders on oi.OrderId equals o.OrderId
            join p in _context.Products on oi.ProductId equals p.ProductId
            where o.Status == OrderStatuses.Delivered
            group oi by new { p.ProductId, p.ProductName } into g
            select new TopProductReportRow
            {
                ProductName = g.Key.ProductName,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Price * x.Quantity)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(8)
            .ToListAsync();

        ViewBag.VoucherRedemptionsMonth = await _context.VoucherRedemptions
            .CountAsync(r => !r.IsReverted && r.RedeemedAt >= monthStart);

        ViewBag.LoyaltyRedeemsMonth = await _context.LoyaltyPointTransactions
            .CountAsync(t => t.TransactionType == LoyaltyPointTypes.Redeem && t.CreatedAt >= monthStart);

        ViewBag.LowStock = await _context.Products
            .Where(p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= 10)
            .OrderBy(p => p.StockQuantity)
            .Take(10)
            .Select(p => new { p.ProductName, p.Sku, p.StockQuantity })
            .ToListAsync();

        ViewBag.OutOfStock = await _context.Products.CountAsync(p => p.IsActive && p.StockQuantity <= 0);

        var year = today.Year;
        var monthly = new List<decimal>();
        for (int m = 1; m <= 12; m++)
        {
            var start = new DateTime(year, m, 1);
            var end = m < 12 ? new DateTime(year, m + 1, 1) : new DateTime(year + 1, 1, 1);
            monthly.Add(await delivered.Where(o => o.OrderDate >= start && o.OrderDate < end).SumAsync(o => o.TotalAmount ?? 0));
        }
        ViewBag.MonthlyRevenue = monthly;
        ViewBag.ChartYear = year;

        return View("~/Views/Admin/Report/Index.cshtml");
    }
}

public class TopProductReportRow
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}
