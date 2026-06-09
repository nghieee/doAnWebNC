using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public async Task<IActionResult> Index(
        [FromQuery] string period = "thisMonth",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var today = DateTime.Today;
        DateTime calculatedStart;
        DateTime calculatedEnd;

        switch (period?.ToLower())
        {
            case "today":
                calculatedStart = today;
                calculatedEnd = today.AddDays(1).AddTicks(-1);
                break;
            case "yesterday":
                calculatedStart = today.AddDays(-1);
                calculatedEnd = today.AddTicks(-1);
                break;
            case "last7days":
                calculatedStart = today.AddDays(-6);
                calculatedEnd = today.AddDays(1).AddTicks(-1);
                break;
            case "last30days":
                calculatedStart = today.AddDays(-29);
                calculatedEnd = today.AddDays(1).AddTicks(-1);
                break;
            case "thismonth":
                calculatedStart = new DateTime(today.Year, today.Month, 1);
                calculatedEnd = calculatedStart.AddMonths(1).AddTicks(-1);
                break;
            case "lastmonth":
                var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
                calculatedStart = firstOfThisMonth.AddMonths(-1);
                calculatedEnd = firstOfThisMonth.AddTicks(-1);
                break;
            case "thisyear":
                calculatedStart = new DateTime(today.Year, 1, 1);
                calculatedEnd = new DateTime(today.Year + 1, 1, 1).AddTicks(-1);
                break;
            case "custom":
                calculatedStart = startDate ?? new DateTime(today.Year, today.Month, 1);
                calculatedEnd = endDate ?? today;
                if (calculatedEnd < calculatedStart) calculatedEnd = calculatedStart;
                calculatedEnd = calculatedEnd.Date.AddDays(1).AddTicks(-1);
                break;
            default:
                period = "thisMonth";
                calculatedStart = new DateTime(today.Year, today.Month, 1);
                calculatedEnd = calculatedStart.AddMonths(1).AddTicks(-1);
                break;
        }

        // Keep variables for UI display
        ViewBag.Period = period;
        ViewBag.StartDateStr = calculatedStart.ToString("yyyy-MM-dd");
        ViewBag.EndDateStr = calculatedEnd.ToString("yyyy-MM-dd");
        ViewBag.RangeText = $"{calculatedStart:dd/MM/yyyy} - {calculatedEnd:dd/MM/yyyy}";

        // --- 1. DELIVERED ORDERS IN RANGE ---
        var deliveredOrders = await _context.Orders
            .Where(o => o.Status == OrderStatuses.Delivered && o.OrderDate >= calculatedStart && o.OrderDate <= calculatedEnd)
            .Select(o => new { o.OrderId, o.TotalAmount, o.VoucherDiscount, o.UserId, o.FullName, o.OrderDate })
            .ToListAsync();

        var totalRevenue = deliveredOrders.Sum(o => o.TotalAmount ?? 0);
        var totalVoucherDiscount = deliveredOrders.Sum(o => o.VoucherDiscount ?? 0);

        // --- 2. DYNAMIC COGS CALCULATION (FEFO BATCH AND FALLBACK) ---
        var orderIds = deliveredOrders.Select(o => o.OrderId).ToList();

        var batchSales = await (
            from t in _context.InventoryTransactions
            where t.TransactionType == "BatchSale" && t.OrderId.HasValue && orderIds.Contains(t.OrderId ?? 0)
            join b in _context.ProductBatches on t.ProductBatchId equals b.ProductBatchId
            select new
            {
                OrderId = t.OrderId ?? 0,
                t.ProductId,
                t.Quantity,
                UnitCost = b.UnitCost ?? t.Product.CostPrice ?? 0
            }
        ).ToListAsync();

        var orderItems = await (
            from o in _context.Orders
            where o.Status == OrderStatuses.Delivered && o.OrderDate >= calculatedStart && o.OrderDate <= calculatedEnd
            join oi in _context.OrderItems on o.OrderId equals oi.OrderId
            join p in _context.Products on oi.ProductId equals p.ProductId
            select new
            {
                oi.OrderId,
                oi.ProductId,
                oi.Quantity,
                oi.Price,
                FallbackCost = p.CostPrice ?? (p.Price * 0.6m)
            }
        ).ToListAsync();

        var batchSalesLookup = batchSales.ToLookup(x => (x.OrderId, x.ProductId));
        decimal totalCogs = 0;
        var productCogsMap = new Dictionary<int, decimal>();

        foreach (var item in orderItems)
        {
            if (!item.ProductId.HasValue) continue;
            var pid = item.ProductId.Value;
            var key = (item.OrderId!.Value, pid);
            decimal itemCogs = 0;

            if (batchSalesLookup.Contains(key))
            {
                var sales = batchSalesLookup[key];
                var totalQtyDeducted = sales.Sum(s => s.Quantity);
                var costFromBatches = sales.Sum(s => s.Quantity * s.UnitCost);

                if (totalQtyDeducted >= item.Quantity)
                {
                    itemCogs = costFromBatches;
                }
                else
                {
                    var remainingQty = item.Quantity - totalQtyDeducted;
                    itemCogs = costFromBatches + (remainingQty * item.FallbackCost);
                }
            }
            else
            {
                itemCogs = item.Quantity * item.FallbackCost;
            }

            totalCogs += itemCogs;

            if (productCogsMap.ContainsKey(pid))
                productCogsMap[pid] += itemCogs;
            else
                productCogsMap[pid] = itemCogs;
        }

        var grossProfit = totalRevenue - totalCogs;
        var grossMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

        // --- 3. CASH INFLOW & OUTFLOW ---
        var cashInflow = await _context.Payments
            .Where(p => p.PaymentStatus == PaymentStatuses.Paid && p.PaymentDate >= calculatedStart && p.PaymentDate <= calculatedEnd)
            .SumAsync(p => p.Amount ?? 0);

        var cashOutflow = await _context.GoodsReceiptLines
            .Where(l => l.GoodsReceipt.ReceiptDate >= calculatedStart && l.GoodsReceipt.ReceiptDate <= calculatedEnd)
            .SumAsync(l => l.Quantity * l.UnitCost);

        // --- 4. INVENTORY PERFORMANCE METRICS ---
        var currentStockValue = await _context.ProductBatches
            .Where(pb => pb.QuantityOnHand > 0 && pb.Product.IsActive)
            .SumAsync(pb => pb.QuantityOnHand * (pb.UnitCost ?? pb.Product.CostPrice ?? 0));

        var writeOffTransactions = await _context.InventoryTransactions
            .Where(t => t.TransactionType == "Adjustment" && t.QuantityAfter < t.QuantityBefore && t.TransactionDate >= calculatedStart && t.TransactionDate <= calculatedEnd)
            .Select(t => new { t.QuantityBefore, t.QuantityAfter, Cost = t.Product.CostPrice ?? 0 })
            .ToListAsync();
        var writeOffLoss = writeOffTransactions.Sum(x => (x.QuantityBefore - x.QuantityAfter) * x.Cost);

        var totalImportsValue = cashOutflow; // equals the completed receipts cost
        var beginningStockValue = currentStockValue - totalImportsValue + totalCogs + writeOffLoss;
        if (beginningStockValue < 0) beginningStockValue = currentStockValue;

        var avgStockValue = (beginningStockValue + currentStockValue) / 2;
        if (avgStockValue <= 0) avgStockValue = currentStockValue;

        var turnoverRatio = avgStockValue > 0 ? totalCogs / avgStockValue : 0;
        var daysInPeriod = (calculatedEnd - calculatedStart).TotalDays;
        if (daysInPeriod < 1) daysInPeriod = 1;
        var dio = turnoverRatio > 0 ? daysInPeriod / (double)turnoverRatio : 0;

        // --- 5. EXPIRED & NEAR EXPIRY WARNINGS (CRITICAL PHARMACY BUSINESS) ---
        var activeBatches = await _context.ProductBatches
            .Include(pb => pb.Product)
            .Where(pb => pb.QuantityOnHand > 0 && pb.Product.IsActive)
            .Select(pb => new
            {
                pb.Product.ProductName,
                pb.Product.Sku,
                pb.BatchNo,
                pb.ExpiryDate,
                pb.QuantityOnHand,
                Cost = pb.UnitCost ?? pb.Product.CostPrice ?? 0
            })
            .ToListAsync();

        var expiringRows = new List<ExpiringBatchRow>();
        var expirySummary = new ExpiryWarningSummary();

        foreach (var b in activeBatches)
        {
            if (b.ExpiryDate == null) continue;

            var daysLeft = (b.ExpiryDate.Value.Date - today).TotalDays;
            string? status = null;

            if (daysLeft < 0)
            {
                status = "Expired";
                expirySummary.ExpiredCount++;
                expirySummary.ExpiredQty += b.QuantityOnHand;
                expirySummary.ExpiredValue += b.QuantityOnHand * b.Cost;
            }
            else if (daysLeft <= 30)
            {
                status = "Near30";
                expirySummary.Near30Count++;
                expirySummary.Near30Qty += b.QuantityOnHand;
                expirySummary.Near30Value += b.QuantityOnHand * b.Cost;
            }
            else if (daysLeft <= 90)
            {
                status = "Near90";
                expirySummary.Near90Count++;
                expirySummary.Near90Qty += b.QuantityOnHand;
                expirySummary.Near90Value += b.QuantityOnHand * b.Cost;
            }
            else if (daysLeft <= 180)
            {
                status = "Near180";
                expirySummary.Near180Count++;
                expirySummary.Near180Qty += b.QuantityOnHand;
                expirySummary.Near180Value += b.QuantityOnHand * b.Cost;
            }

            if (status != null)
            {
                expiringRows.Add(new ExpiringBatchRow
                {
                    ProductName = b.ProductName,
                    Sku = b.Sku ?? "",
                    BatchNo = b.BatchNo,
                    ExpiryDate = b.ExpiryDate,
                    QuantityOnHand = b.QuantityOnHand,
                    Cost = b.Cost,
                    Status = status
                });
            }
        }
        expiringRows = expiringRows.OrderBy(x => x.ExpiryDate).ToList();

        // --- 6. PRODUCT SALES PERFORMANCE LIST ---
        var pidsInItems = orderItems.Where(oi => oi.ProductId.HasValue).Select(oi => oi.ProductId!.Value).Distinct().ToList();
        var productInfo = await _context.Products
            .Where(p => pidsInItems.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId, p => new { p.ProductName, p.Sku });

        var productImages = await _context.ProductImages
            .Where(pi => pidsInItems.Contains(pi.ProductId))
            .ToListAsync();
        var productImageLookup = productImages
            .GroupBy(pi => pi.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.FirstOrDefault(pi => pi.IsMain == true)?.ImageUrl 
                    ?? g.FirstOrDefault()?.ImageUrl 
                    ?? "default.png"
            );

        var productRows = orderItems
            .Where(oi => oi.ProductId.HasValue)
            .GroupBy(oi => oi.ProductId!.Value)
            .Select(g => {
                var pid = g.Key;
                var qty = g.Sum(x => x.Quantity);
                var rev = g.Sum(x => x.Quantity * x.Price);
                var cogs = productCogsMap.ContainsKey(pid) ? productCogsMap[pid] : 0;
                var name = productInfo.ContainsKey(pid) ? productInfo[pid].ProductName : "Sản phẩm khác";
                var sku = productInfo.ContainsKey(pid) ? productInfo[pid].Sku ?? "" : "";
                var img = productImageLookup.ContainsKey(pid) ? productImageLookup[pid] : "default.png";
                return new ProductReportRow
                {
                    ProductId = pid,
                    ProductName = name,
                    Sku = sku,
                    QuantitySold = qty,
                    Revenue = rev,
                    Cogs = cogs,
                    ProductImageUrl = img
                };
            })
            .OrderByDescending(x => x.QuantitySold)
            .ToList();

        // --- 7. PAYMENT METHOD BREAKDOWN ---
        var paymentBreakdown = await (
            from o in _context.Orders
            where o.Status == OrderStatuses.Delivered && o.OrderDate >= calculatedStart && o.OrderDate <= calculatedEnd
            join p in _context.Payments on o.OrderId equals p.OrderId into ps
            from p in ps.DefaultIfEmpty()
            select new
            {
                o.OrderId,
                PaymentMethod = p != null ? p.PaymentMethod : "COD",
                Amount = o.TotalAmount ?? 0
            }
        ).ToListAsync();

        var paymentSummaries = paymentBreakdown
            .GroupBy(x => x.PaymentMethod ?? "COD")
            .Select(g => new PaymentMethodSummary
            {
                PaymentMethod = g.Key,
                OrderCount = g.Select(x => x.OrderId).Distinct().Count(),
                TotalAmount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        // --- 8. TOP SPENDING CUSTOMERS ---
        var userIdsInOrders = deliveredOrders.Where(o => o.UserId != null).Select(o => o.UserId!).Distinct().ToList();
        var userEmails = await _context.Users
            .Where(u => userIdsInOrders.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? "");

        var customerRows = deliveredOrders
            .GroupBy(o => o.UserId ?? o.FullName ?? "Khách vãng lai")
            .Select(g => {
                var first = g.First();
                var key = g.Key;
                string email = "";
                if (first.UserId != null && userEmails.ContainsKey(first.UserId))
                {
                    email = userEmails[first.UserId];
                }
                string fullName = first.FullName ?? "Khách vãng lai";
                return new CustomerSpendingRow
                {
                    Email = email,
                    FullName = fullName,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalAmount ?? 0)
                };
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(10)
            .ToList();

        // --- 9. DYNAMIC TREND CHART DATA ---
        var chartPoints = new List<ChartPoint>();
        var totalDays = (calculatedEnd - calculatedStart).TotalDays;

        if (totalDays <= 60)
        {
            for (var date = calculatedStart.Date; date <= calculatedEnd.Date; date = date.AddDays(1))
            {
                var nextDay = date.AddDays(1);
                var dayOrders = deliveredOrders.Where(o => o.OrderDate >= date && o.OrderDate < nextDay).ToList();
                var dayOrderIds = dayOrders.Select(o => o.OrderId).ToList();
                var dayRevenue = dayOrders.Sum(o => o.TotalAmount ?? 0);

                decimal dayCogs = 0;
                var dayItems = orderItems.Where(oi => oi.OrderId.HasValue && dayOrderIds.Contains(oi.OrderId.Value)).ToList();
                foreach (var item in dayItems)
                {
                    var key = (item.OrderId!.Value, item.ProductId!.Value);
                    if (batchSalesLookup.Contains(key))
                    {
                        var sales = batchSalesLookup[key];
                        var totalQtyDeducted = sales.Sum(s => s.Quantity);
                        var costFromBatches = sales.Sum(s => s.Quantity * s.UnitCost);

                        if (totalQtyDeducted >= item.Quantity)
                            dayCogs += costFromBatches;
                        else
                            dayCogs += costFromBatches + ((item.Quantity - totalQtyDeducted) * item.FallbackCost);
                    }
                    else
                    {
                        dayCogs += item.Quantity * item.FallbackCost;
                    }
                }

                chartPoints.Add(new ChartPoint
                {
                    Label = date.ToString("dd/MM"),
                    Revenue = dayRevenue,
                    Cogs = dayCogs,
                    Profit = dayRevenue - dayCogs
                });
            }
        }
        else
        {
            var startMonth = new DateTime(calculatedStart.Year, calculatedStart.Month, 1);
            var endMonth = new DateTime(calculatedEnd.Year, calculatedEnd.Month, 1);

            for (var m = startMonth; m <= endMonth; m = m.AddMonths(1))
            {
                var nextMonth = m.AddMonths(1);
                var monthOrders = deliveredOrders.Where(o => o.OrderDate >= m && o.OrderDate < nextMonth).ToList();
                var monthOrderIds = monthOrders.Select(o => o.OrderId).ToList();
                var monthRevenue = monthOrders.Sum(o => o.TotalAmount ?? 0);

                decimal monthCogs = 0;
                var monthItems = orderItems.Where(oi => oi.OrderId.HasValue && monthOrderIds.Contains(oi.OrderId.Value)).ToList();
                foreach (var item in monthItems)
                {
                    var key = (item.OrderId!.Value, item.ProductId!.Value);
                    if (batchSalesLookup.Contains(key))
                    {
                        var sales = batchSalesLookup[key];
                        var totalQtyDeducted = sales.Sum(s => s.Quantity);
                        var costFromBatches = sales.Sum(s => s.Quantity * s.UnitCost);

                        if (totalQtyDeducted >= item.Quantity)
                            monthCogs += costFromBatches;
                        else
                            monthCogs += costFromBatches + ((item.Quantity - totalQtyDeducted) * item.FallbackCost);
                    }
                    else
                    {
                        monthCogs += item.Quantity * item.FallbackCost;
                    }
                }

                chartPoints.Add(new ChartPoint
                {
                    Label = $"T{m.Month}/{m.Year.ToString().Substring(2)}",
                    Revenue = monthRevenue,
                    Cogs = monthCogs,
                    Profit = monthRevenue - monthCogs
                });
            }
        }

        // Pack values into ViewBag
        ViewBag.Revenue = totalRevenue;
        ViewBag.Cogs = totalCogs;
        ViewBag.GrossProfit = grossProfit;
        ViewBag.GrossMargin = grossMargin;
        ViewBag.VoucherDiscount = totalVoucherDiscount;

        ViewBag.CashInflow = cashInflow;
        ViewBag.CashOutflow = cashOutflow;
        ViewBag.NetCashFlow = cashInflow - cashOutflow;

        ViewBag.CurrentStockValue = currentStockValue;
        ViewBag.TurnoverRatio = turnoverRatio;
        ViewBag.Dio = dio;
        ViewBag.WriteOffLoss = writeOffLoss;

        ViewBag.ExpirySummary = expirySummary;
        ViewBag.ExpiringRows = expiringRows;
        ViewBag.ProductRows = productRows;
        ViewBag.PaymentSummaries = paymentSummaries;
        ViewBag.CustomerRows = customerRows;
        ViewBag.ChartData = chartPoints;

        // Legacy values for safety / sidebar alerts count
        var monthStart = new DateTime(today.Year, today.Month, 1);
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

        return View("~/Views/Admin/Report/Index.cshtml");
    }

    [HttpGet("Export")]
    public async Task<IActionResult> Export(
        [FromQuery] string period = "thisMonth",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var today = DateTime.Today;
        DateTime calculatedStart;
        DateTime calculatedEnd;

        switch (period?.ToLower())
        {
            case "today":
                calculatedStart = today;
                calculatedEnd = today.AddDays(1).AddTicks(-1);
                break;
            case "yesterday":
                calculatedStart = today.AddDays(-1);
                calculatedEnd = today.AddTicks(-1);
                break;
            case "last7days":
                calculatedStart = today.AddDays(-6);
                calculatedEnd = today.AddDays(1).AddTicks(-1);
                break;
            case "last30days":
                calculatedStart = today.AddDays(-29);
                calculatedEnd = today.AddDays(1).AddTicks(-1);
                break;
            case "thismonth":
                calculatedStart = new DateTime(today.Year, today.Month, 1);
                calculatedEnd = calculatedStart.AddMonths(1).AddTicks(-1);
                break;
            case "lastmonth":
                var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
                calculatedStart = firstOfThisMonth.AddMonths(-1);
                calculatedEnd = firstOfThisMonth.AddTicks(-1);
                break;
            case "thisyear":
                calculatedStart = new DateTime(today.Year, 1, 1);
                calculatedEnd = new DateTime(today.Year + 1, 1, 1).AddTicks(-1);
                break;
            case "custom":
                calculatedStart = startDate ?? new DateTime(today.Year, today.Month, 1);
                calculatedEnd = endDate ?? today;
                if (calculatedEnd < calculatedStart) calculatedEnd = calculatedStart;
                calculatedEnd = calculatedEnd.Date.AddDays(1).AddTicks(-1);
                break;
            default:
                period = "thisMonth";
                calculatedStart = new DateTime(today.Year, today.Month, 1);
                calculatedEnd = calculatedStart.AddMonths(1).AddTicks(-1);
                break;
        }

        // Load data queries exactly like Index to ensure parity
        var deliveredOrders = await _context.Orders
            .Where(o => o.Status == OrderStatuses.Delivered && o.OrderDate >= calculatedStart && o.OrderDate <= calculatedEnd)
            .Select(o => new { o.OrderId, o.TotalAmount, o.VoucherDiscount, o.UserId, o.FullName, o.OrderDate })
            .ToListAsync();

        var totalRevenue = deliveredOrders.Sum(o => o.TotalAmount ?? 0);
        var totalVoucherDiscount = deliveredOrders.Sum(o => o.VoucherDiscount ?? 0);

        var orderIds = deliveredOrders.Select(o => o.OrderId).ToList();

        var batchSales = await (
            from t in _context.InventoryTransactions
            where t.TransactionType == "BatchSale" && t.OrderId.HasValue && orderIds.Contains(t.OrderId ?? 0)
            join b in _context.ProductBatches on t.ProductBatchId equals b.ProductBatchId
            select new
            {
                OrderId = t.OrderId ?? 0,
                t.ProductId,
                t.Quantity,
                UnitCost = b.UnitCost ?? t.Product.CostPrice ?? 0
            }
        ).ToListAsync();

        var orderItems = await (
            from o in _context.Orders
            where o.Status == OrderStatuses.Delivered && o.OrderDate >= calculatedStart && o.OrderDate <= calculatedEnd
            join oi in _context.OrderItems on o.OrderId equals oi.OrderId
            join p in _context.Products on oi.ProductId equals p.ProductId
            select new
            {
                oi.OrderId,
                oi.ProductId,
                oi.Quantity,
                oi.Price,
                FallbackCost = p.CostPrice ?? (p.Price * 0.6m)
            }
        ).ToListAsync();

        var batchSalesLookup = batchSales.ToLookup(x => (x.OrderId, x.ProductId));
        decimal totalCogs = 0;
        var productCogsMap = new Dictionary<int, decimal>();

        foreach (var item in orderItems)
        {
            if (!item.ProductId.HasValue) continue;
            var pid = item.ProductId.Value;
            var key = (item.OrderId!.Value, pid);
            decimal itemCogs = 0;

            if (batchSalesLookup.Contains(key))
            {
                var sales = batchSalesLookup[key];
                var totalQtyDeducted = sales.Sum(s => s.Quantity);
                var costFromBatches = sales.Sum(s => s.Quantity * s.UnitCost);

                if (totalQtyDeducted >= item.Quantity)
                {
                    itemCogs = costFromBatches;
                }
                else
                {
                    var remainingQty = item.Quantity - totalQtyDeducted;
                    itemCogs = costFromBatches + (remainingQty * item.FallbackCost);
                }
            }
            else
            {
                itemCogs = item.Quantity * item.FallbackCost;
            }

            totalCogs += itemCogs;

            if (productCogsMap.ContainsKey(pid))
                productCogsMap[pid] += itemCogs;
            else
                productCogsMap[pid] = itemCogs;
        }

        var cashInflow = await _context.Payments
            .Where(p => p.PaymentStatus == PaymentStatuses.Paid && p.PaymentDate >= calculatedStart && p.PaymentDate <= calculatedEnd)
            .SumAsync(p => p.Amount ?? 0);

        var cashOutflow = await _context.GoodsReceiptLines
            .Where(l => l.GoodsReceipt.ReceiptDate >= calculatedStart && l.GoodsReceipt.ReceiptDate <= calculatedEnd)
            .SumAsync(l => l.Quantity * l.UnitCost);

        var currentStockValue = await _context.ProductBatches
            .Where(pb => pb.QuantityOnHand > 0 && pb.Product.IsActive)
            .SumAsync(pb => pb.QuantityOnHand * (pb.UnitCost ?? pb.Product.CostPrice ?? 0));

        var writeOffTransactions = await _context.InventoryTransactions
            .Where(t => t.TransactionType == "Adjustment" && t.QuantityAfter < t.QuantityBefore && t.TransactionDate >= calculatedStart && t.TransactionDate <= calculatedEnd)
            .Select(t => new { t.QuantityBefore, t.QuantityAfter, Cost = t.Product.CostPrice ?? 0 })
            .ToListAsync();
        var writeOffLoss = writeOffTransactions.Sum(x => (x.QuantityBefore - x.QuantityAfter) * x.Cost);

        var pidsInItems = orderItems.Where(oi => oi.ProductId.HasValue).Select(oi => oi.ProductId!.Value).Distinct().ToList();
        var productInfo = await _context.Products
            .Where(p => pidsInItems.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId, p => new { p.ProductName, p.Sku });

        var productImages = await _context.ProductImages
            .Where(pi => pidsInItems.Contains(pi.ProductId))
            .ToListAsync();
        var productImageLookup = productImages
            .GroupBy(pi => pi.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.FirstOrDefault(pi => pi.IsMain == true)?.ImageUrl 
                    ?? g.FirstOrDefault()?.ImageUrl 
                    ?? "default.png"
            );

        var productRows = orderItems
            .Where(oi => oi.ProductId.HasValue)
            .GroupBy(oi => oi.ProductId!.Value)
            .Select(g => {
                var pid = g.Key;
                var qty = g.Sum(x => x.Quantity);
                var rev = g.Sum(x => x.Quantity * x.Price);
                var cogs = productCogsMap.ContainsKey(pid) ? productCogsMap[pid] : 0;
                var name = productInfo.ContainsKey(pid) ? productInfo[pid].ProductName : "Sản phẩm khác";
                var sku = productInfo.ContainsKey(pid) ? productInfo[pid].Sku ?? "" : "";
                var img = productImageLookup.ContainsKey(pid) ? productImageLookup[pid] : "default.png";
                return new ProductReportRow
                {
                    ProductId = pid,
                    ProductName = name,
                    Sku = sku,
                    QuantitySold = qty,
                    Revenue = rev,
                    Cogs = cogs,
                    ProductImageUrl = img
                };
            })
            .OrderByDescending(x => x.QuantitySold)
            .ToList();

        // Generate CSV File
        var sb = new StringBuilder();
        sb.AppendLine("BÁO CÁO TÀI CHÍNH VÀ VẬN HÀNH CHI TIẾT");
        sb.AppendLine($"Khoảng thời gian: {calculatedStart:dd/MM/yyyy HH:mm:ss} - {calculatedEnd:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("1. CHỈ SỐ TÀI CHÍNH CHỦ CHỐT");
        sb.AppendLine("Chỉ số,Giá trị (VNĐ)");
        sb.AppendLine($"Tổng doanh thu,{totalRevenue:F0}");
        sb.AppendLine($"Tổng giá vốn hàng bán (COGS),{totalCogs:F0}");
        sb.AppendLine($"Lợi nhuận gộp,{totalRevenue - totalCogs:F0}");
        sb.AppendLine($"Biên lợi nhuận gộp (%),{(totalRevenue > 0 ? ((totalRevenue - totalCogs) / totalRevenue * 100).ToString("F2") : "0")}%");
        sb.AppendLine($"Chi phí Voucher,{totalVoucherDiscount:F0}");
        sb.AppendLine($"Tổng dòng tiền vào (Inflow),{cashInflow:F0}");
        sb.AppendLine($"Tổng dòng tiền ra (Outflow),{cashOutflow:F0}");
        sb.AppendLine($"Dòng tiền ròng (Net Cash Flow),{cashInflow - cashOutflow:F0}");
        sb.AppendLine();

        sb.AppendLine("2. CHỈ SỐ KHO HÀNG VÀ HIỆU SUẤT");
        sb.AppendLine("Chỉ số,Giá trị");
        sb.AppendLine($"Giá trị tồn kho hiện tại,{currentStockValue:F0} VNĐ");
        sb.AppendLine($"Thất thoát do điều chỉnh kho,{writeOffLoss:F0} VNĐ");
        sb.AppendLine();

        sb.AppendLine("3. HIỆU SUẤT DOANH SỐ THEO SẢN PHẨM");
        sb.AppendLine("Mã SKU,Tên sản phẩm,Số lượng bán,Doanh thu (VNĐ),Giá vốn (VNĐ),Lợi nhuận gộp (VNĐ),Biên lợi nhuận gộp (%)");
        foreach (var r in productRows)
        {
            sb.AppendLine($"\"{r.Sku}\",\"{r.ProductName.Replace("\"", "\"\"")}\",{r.QuantitySold},{r.Revenue:F0},{r.Cogs:F0},{r.GrossProfit:F0},{r.GrossMargin:F2}%");
        }
        sb.AppendLine();

        sb.AppendLine("4. CHI TIẾT CÁC ĐƠN HÀNG HOÀN THÀNH");
        sb.AppendLine("Mã đơn,Ngày hoàn thành,Khách hàng,Tổng tiền (VNĐ),Khấu trừ Voucher (VNĐ)");
        foreach (var o in deliveredOrders.OrderBy(x => x.OrderDate))
        {
            sb.AppendLine($"{o.OrderId},{o.OrderDate:dd/MM/yyyy HH:mm},\"{o.FullName}\",{o.TotalAmount:F0},{o.VoucherDiscount:F0}");
        }

        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = bom.Concat(csvBytes).ToArray();

        return File(bytes, "text/csv", $"BaoCao_QuanLy_{calculatedStart:yyyyMMdd}_{calculatedEnd:yyyyMMdd}.csv");
    }
}

// --- HELPER REPORT MODEL CLASSES ---

public class TopProductReportRow
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class ExpiringBatchRow
{
    public string ProductName { get; set; } = null!;
    public string Sku { get; set; } = "";
    public string BatchNo { get; set; } = null!;
    public DateTime? ExpiryDate { get; set; }
    public int QuantityOnHand { get; set; }
    public decimal Cost { get; set; }
    public decimal TotalValue => QuantityOnHand * Cost;
    public string Status { get; set; } = ""; // "Expired" | "Near30" | "Near90" | "Near180"
}

public class ExpiryWarningSummary
{
    public int ExpiredCount { get; set; }
    public int ExpiredQty { get; set; }
    public decimal ExpiredValue { get; set; }

    public int Near30Count { get; set; }
    public int Near30Qty { get; set; }
    public decimal Near30Value { get; set; }

    public int Near90Count { get; set; }
    public int Near90Qty { get; set; }
    public decimal Near90Value { get; set; }

    public int Near180Count { get; set; }
    public int Near180Qty { get; set; }
    public decimal Near180Value { get; set; }
}

public class ProductReportRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Sku { get; set; } = "";
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cogs { get; set; }
    public decimal GrossProfit => Revenue - Cogs;
    public decimal GrossMargin => Revenue > 0 ? (GrossProfit / Revenue) * 100 : 0;
    public string ProductImageUrl { get; set; } = "";
}

public class PaymentMethodSummary
{
    public string PaymentMethod { get; set; } = "";
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CustomerSpendingRow
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class ChartPoint
{
    public string Label { get; set; } = "";
    public decimal Revenue { get; set; }
    public decimal Cogs { get; set; }
    public decimal Profit { get; set; }
}
