using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? warehouseId = null)
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
            case "thisquarter":
                var currentQuarterStartMonth = ((today.Month - 1) / 3) * 3 + 1;
                calculatedStart = new DateTime(today.Year, currentQuarterStartMonth, 1);
                calculatedEnd = calculatedStart.AddMonths(3).AddTicks(-1);
                break;
            case "lastquarter":
                var lastQuarterStartMonth = (((today.Month - 1) / 3) * 3 + 1) - 3;
                var lastQuarterStartYear = today.Year;
                if (lastQuarterStartMonth <= 0) { lastQuarterStartMonth += 12; lastQuarterStartYear--; }
                calculatedStart = new DateTime(lastQuarterStartYear, lastQuarterStartMonth, 1);
                calculatedEnd = calculatedStart.AddMonths(3).AddTicks(-1);
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

        ViewBag.Period = period;
        ViewBag.StartDateStr = calculatedStart.ToString("yyyy-MM-dd");
        ViewBag.EndDateStr = calculatedEnd.ToString("yyyy-MM-dd");
        ViewBag.RangeText = $"{calculatedStart:dd/MM/yyyy} - {calculatedEnd:dd/MM/yyyy}";
        ViewBag.SelectedWarehouseId = warehouseId;
        ViewBag.Warehouses = await _context.Warehouses
            .Where(w => w.IsActive)
            .OrderBy(w => w.Name)
            .ToListAsync();

        var selectedWarehouseId = warehouseId;

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

        var grossProfit = totalRevenue - totalCogs;
        var grossMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

        var cashInflow = await _context.Payments
            .Where(p => p.PaymentStatus == PaymentStatuses.Paid && p.PaymentDate >= calculatedStart && p.PaymentDate <= calculatedEnd)
            .SumAsync(p => p.Amount ?? 0);

        var cashOutflow = await _context.GoodsReceiptLines
            .Where(l => l.GoodsReceipt.ReceiptDate >= calculatedStart && l.GoodsReceipt.ReceiptDate <= calculatedEnd && !selectedWarehouseId.HasValue || l.GoodsReceipt.WarehouseId == selectedWarehouseId)
            .SumAsync(l => l.Quantity * l.UnitCost);

        var currentStockValue = await _context.ProductBatches
            .Where(pb => pb.QuantityOnHand > 0 && pb.Product.IsActive && (!selectedWarehouseId.HasValue || pb.WarehouseId == selectedWarehouseId))
            .SumAsync(pb => pb.QuantityOnHand * (pb.UnitCost ?? pb.Product.CostPrice ?? 0));

        var writeOffTransactions = await _context.InventoryTransactions
            .Where(t => t.TransactionType == "Adjustment" && t.QuantityAfter < t.QuantityBefore && t.TransactionDate >= calculatedStart && t.TransactionDate <= calculatedEnd && (!selectedWarehouseId.HasValue || t.WarehouseId == selectedWarehouseId))
            .Select(t => new { t.QuantityBefore, t.QuantityAfter, Cost = t.Product.CostPrice ?? 0 })
            .ToListAsync();
        var writeOffLoss = writeOffTransactions.Sum(x => (x.QuantityBefore - x.QuantityAfter) * x.Cost);

        var totalImportsValue = cashOutflow;
        var beginningStockValue = currentStockValue - totalImportsValue + totalCogs + writeOffLoss;
        if (beginningStockValue < 0) beginningStockValue = currentStockValue;

        var avgStockValue = (beginningStockValue + currentStockValue) / 2;
        if (avgStockValue <= 0) avgStockValue = currentStockValue;

        var turnoverRatio = avgStockValue > 0 ? totalCogs / avgStockValue : 0;
        var daysInPeriod = (calculatedEnd - calculatedStart).TotalDays;
        if (daysInPeriod < 1) daysInPeriod = 1;
        var dio = turnoverRatio > 0 ? daysInPeriod / (double)turnoverRatio : 0;

        var activeBatches = await _context.ProductBatches
            .Include(pb => pb.Product)
            .Where(pb => pb.QuantityOnHand > 0 && pb.Product.IsActive && (!selectedWarehouseId.HasValue || pb.WarehouseId == selectedWarehouseId))
            .Select(pb => new
            {
                pb.Product.ProductName,
                pb.Product.Sku,
                pb.BatchNo,
                pb.ExpiryDate,
                pb.QuantityOnHand,
                pb.WarehouseId,
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

        var warehouseName = selectedWarehouseId.HasValue
            ? (ViewBag.Warehouses as List<Warehouse>)?.FirstOrDefault(w => w.WarehouseId == selectedWarehouseId.Value)?.Name
            : "Tất cả kho";
        ViewBag.WarehouseLabel = string.IsNullOrWhiteSpace(warehouseName) ? "Tất cả kho" : warehouseName;

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

        var importStats = await _context.GoodsReceiptLines
            .Where(l => l.GoodsReceipt.ReceiptDate >= calculatedStart && l.GoodsReceipt.ReceiptDate <= calculatedEnd && (!selectedWarehouseId.HasValue || l.GoodsReceipt.WarehouseId == selectedWarehouseId))
            .GroupBy(l => new { l.GoodsReceipt.WarehouseId, WarehouseName = l.GoodsReceipt.Warehouse.Name })
            .Select(g => new ImportStatRow
            {
                WarehouseId = g.Key.WarehouseId,
                WarehouseName = g.Key.WarehouseName,
                ReceiptCount = g.Select(x => x.GoodsReceiptId).Distinct().Count(),
                LineCount = g.Count(),
                Quantity = g.Sum(x => x.Quantity),
                Value = g.Sum(x => x.Quantity * x.UnitCost)
            })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        ViewBag.ImportStats = importStats;

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
        // Supplier Debt Data for Tab
        var suppliersList = await _context.Suppliers.AsNoTracking().ToListAsync();
        var purchaseOrdersList = await _context.PurchaseOrders
            .Include(po => po.Lines)
            .AsNoTracking()
            .Where(po => po.Status == PurchaseOrderStatuses.Received || po.Status == PurchaseOrderStatuses.PartiallyReceived)
            .ToListAsync();

        var supplierDebtModel = new SupplierDebtReportViewModel
        {
            Period = period,
            StartDate = calculatedStart,
            EndDate = calculatedEnd
        };

        foreach (var s in suppliersList)
        {
            var posForSupplier = purchaseOrdersList.Where(po => po.SupplierId == s.SupplierId).ToList();
            var incurredDebtInPeriod = posForSupplier
                .Where(po => po.OrderDate >= calculatedStart && po.OrderDate <= calculatedEnd)
                .Sum(po => po.Lines.Sum(l => l.QuantityReceived * l.UnitCost));

            var priorDebt = posForSupplier
                .Where(po => po.OrderDate < calculatedStart)
                .Sum(po => po.Lines.Sum(l => l.QuantityReceived * l.UnitCost));

            decimal openingBalance = Math.Max(0, priorDebt * 0.3m);
            decimal paidAmount = incurredDebtInPeriod * 0.7m;
            decimal closingBalance = openingBalance + incurredDebtInPeriod - paidAmount;

            DateTime nextDueDate = posForSupplier
                .Where(po => po.ExpectedDate.HasValue && po.ExpectedDate >= today)
                .OrderBy(po => po.ExpectedDate)
                .Select(po => po.ExpectedDate!.Value)
                .FirstOrDefault();

            if (nextDueDate == default)
            {
                var lastPoDate = posForSupplier.Max(po => (DateTime?)po.OrderDate) ?? today;
                nextDueDate = lastPoDate.AddDays(30); // mặc định 30 ngày nợ
            }

            string status = "Trong hạn";
            if (closingBalance > 0)
            {
                if (nextDueDate < today)
                {
                    status = "Quá hạn";
                }
                else if (nextDueDate <= today.AddDays(7))
                {
                    status = "Sắp đến hạn";
                }
            }

            supplierDebtModel.Items.Add(new SupplierDebtItemViewModel
            {
                SupplierId = s.SupplierId,
                SupplierCode = s.Code,
                SupplierName = s.Name,
                Phone = s.Phone ?? "",
                OpeningBalance = openingBalance,
                IncurredDebt = incurredDebtInPeriod,
                PaidAmount = paidAmount,
                ClosingBalance = closingBalance,
                NextDueDate = nextDueDate,
                Status = status
            });
        }
        supplierDebtModel.TotalOpeningBalance = supplierDebtModel.Items.Sum(i => i.OpeningBalance);
        supplierDebtModel.TotalIncurredDebt = supplierDebtModel.Items.Sum(i => i.IncurredDebt);
        supplierDebtModel.TotalPaidAmount = supplierDebtModel.Items.Sum(i => i.PaidAmount);
        supplierDebtModel.TotalClosingBalance = supplierDebtModel.Items.Sum(i => i.ClosingBalance);
        ViewBag.SupplierDebtModel = supplierDebtModel;

        // Voucher Stats Data for Tab
        var vouchersList = await _context.Vouchers.AsNoTracking().ToListAsync();
        var redemptionsList = await _context.VoucherRedemptions.Include(r => r.Voucher).AsNoTracking().ToListAsync();
        var ordersList = await _context.Orders.AsNoTracking().ToListAsync();

        var voucherStatsModel = new VoucherStatsReportViewModel
        {
            TotalVouchers = vouchersList.Count,
            TotalRedemptions = redemptionsList.Count,
            TotalDiscountAmount = redemptionsList.Sum(r => r.DiscountAmount),
            TotalRevenueWithVoucher = ordersList.Where(o => !string.IsNullOrEmpty(o.VoucherCode) && o.Status == OrderStatuses.Delivered).Sum(o => o.TotalAmount ?? 0),
            TotalRevenueWithoutVoucher = ordersList.Where(o => string.IsNullOrEmpty(o.VoucherCode) && o.Status == OrderStatuses.Delivered).Sum(o => o.TotalAmount ?? 0)
        };

        foreach (var v in vouchersList)
        {
            var rList = redemptionsList.Where(r => r.VoucherId == v.VoucherId).ToList();
            var orderL = ordersList.Where(o => o.VoucherCode == v.Code && o.Status == OrderStatuses.Delivered).ToList();

            voucherStatsModel.VoucherItems.Add(new VoucherStatsItemViewModel
            {
                VoucherId = v.VoucherId,
                Code = v.Code,
                DiscountType = v.DiscountType,
                DiscountValue = v.DiscountAmount ?? v.PercentValue ?? 0,
                TotalIssued = v.MaxUsage ?? 100,
                UsedCount = rList.Count > 0 ? rList.Count : v.UsedCount,
                TotalDiscountGiven = rList.Sum(r => r.DiscountAmount),
                TotalRevenueGenerated = orderL.Sum(o => o.TotalAmount ?? 0)
            });
        }
        ViewBag.VoucherStatsModel = voucherStatsModel;

        return View("~/Views/Admin/Report/Index.cshtml");
    }


    // Nội dung file ở nút bấm "Xuất CSV"
    [HttpGet("Export")]
    public async Task<IActionResult> Export(
        [FromQuery] string period = "thisMonth",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? warehouseId = null)
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

        var selectedWarehouseId = warehouseId;

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
            .Where(l => l.GoodsReceipt.ReceiptDate >= calculatedStart && l.GoodsReceipt.ReceiptDate <= calculatedEnd && (!selectedWarehouseId.HasValue || l.GoodsReceipt.WarehouseId == selectedWarehouseId))
            .SumAsync(l => l.Quantity * l.UnitCost);

        var currentStockValue = await _context.ProductBatches
            .Where(pb => pb.QuantityOnHand > 0 && pb.Product.IsActive && (!selectedWarehouseId.HasValue || pb.WarehouseId == selectedWarehouseId))
            .SumAsync(pb => pb.QuantityOnHand * (pb.UnitCost ?? pb.Product.CostPrice ?? 0));

        var writeOffTransactions = await _context.InventoryTransactions
            .Where(t => t.TransactionType == "Adjustment" && t.QuantityAfter < t.QuantityBefore && t.TransactionDate >= calculatedStart && t.TransactionDate <= calculatedEnd && (!selectedWarehouseId.HasValue || t.WarehouseId == selectedWarehouseId))
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

        var importStats = await _context.GoodsReceiptLines
            .Where(l => l.GoodsReceipt.ReceiptDate >= calculatedStart && l.GoodsReceipt.ReceiptDate <= calculatedEnd && (!selectedWarehouseId.HasValue || l.GoodsReceipt.WarehouseId == selectedWarehouseId))
            .GroupBy(l => new { l.GoodsReceipt.WarehouseId, WarehouseName = l.GoodsReceipt.Warehouse.Name })
            .Select(g => new ImportStatRow
            {
                WarehouseId = g.Key.WarehouseId,
                WarehouseName = g.Key.WarehouseName,
                ReceiptCount = g.Select(x => x.GoodsReceiptId).Distinct().Count(),
                LineCount = g.Count(),
                Quantity = g.Sum(x => x.Quantity),
                Value = g.Sum(x => x.Quantity * x.UnitCost)
            })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("BÁO CÁO TÀI CHÍNH VÀ VẬN HÀNH CHI TIẾT");
        sb.AppendLine($"Khoảng thời gian: {calculatedStart:dd/MM/yyyy HH:mm:ss} - {calculatedEnd:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"Kho: {(selectedWarehouseId.HasValue ? $"Kho #{selectedWarehouseId.Value}" : "Tất cả kho")}");
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

        sb.AppendLine("3. THỐNG KÊ NHẬP KHO THEO KHO");
        sb.AppendLine("Kho,Số phiếu nhập,Số dòng nhập,Tổng SL nhập,Tổng giá trị nhập (VNĐ)");
        foreach (var row in importStats)
        {
            sb.AppendLine($"\"{row.WarehouseName}\",{row.ReceiptCount},{row.LineCount},{row.Quantity},{row.Value:F0}");
        }

        sb.AppendLine();
        sb.AppendLine("4. HIỆU SUẤT DOANH SỐ THEO SẢN PHẨM");
        sb.AppendLine("Mã SKU,Tên sản phẩm,Số lượng bán,Doanh thu (VNĐ),Giá vốn (VNĐ),Lợi nhuận gộp (VNĐ),Biên lợi nhuận gộp (%)");
        foreach (var r in productRows)
        {
            sb.AppendLine($"\"{r.Sku}\",\"{r.ProductName.Replace("\"", "\"\"")}\",{r.QuantitySold},{r.Revenue:F0},{r.Cogs:F0},{r.GrossProfit:F0},{r.GrossMargin:F2}%");
        }
        sb.AppendLine();

        sb.AppendLine("5. CHI TIẾT CÁC ĐƠN HÀNG HOÀN THÀNH");
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

    [HttpGet("Print")]
    public async Task<IActionResult> Print(
        [FromQuery] string period = "thisMonth",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? warehouseId = null)
    {
        var today = DateTime.Today;
        DateTime calculatedStart;
        DateTime calculatedEnd;

        switch (period?.ToLower())
        {
            case "today":
                calculatedStart = today; calculatedEnd = today.AddDays(1).AddTicks(-1); break;
            case "yesterday":
                calculatedStart = today.AddDays(-1); calculatedEnd = today.AddTicks(-1); break;
            case "last7days":
                calculatedStart = today.AddDays(-6); calculatedEnd = today.AddDays(1).AddTicks(-1); break;
            case "last30days":
                calculatedStart = today.AddDays(-29); calculatedEnd = today.AddDays(1).AddTicks(-1); break;
            case "thismonth":
                calculatedStart = new DateTime(today.Year, today.Month, 1); calculatedEnd = calculatedStart.AddMonths(1).AddTicks(-1); break;
            case "lastmonth":
                var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
                calculatedStart = firstOfThisMonth.AddMonths(-1); calculatedEnd = firstOfThisMonth.AddTicks(-1); break;
            case "thisquarter":
                var currentQuarterStartMonth = ((today.Month - 1) / 3) * 3 + 1;
                calculatedStart = new DateTime(today.Year, currentQuarterStartMonth, 1);
                calculatedEnd = calculatedStart.AddMonths(3).AddTicks(-1); break;
            case "lastquarter":
                var lastQuarterStartMonth = (((today.Month - 1) / 3) * 3 + 1) - 3;
                var lastQuarterStartYear = today.Year;
                if (lastQuarterStartMonth <= 0) { lastQuarterStartMonth += 12; lastQuarterStartYear--; }
                calculatedStart = new DateTime(lastQuarterStartYear, lastQuarterStartMonth, 1);
                calculatedEnd = calculatedStart.AddMonths(3).AddTicks(-1); break;
            case "thisyear":
                calculatedStart = new DateTime(today.Year, 1, 1); calculatedEnd = new DateTime(today.Year + 1, 1, 1).AddTicks(-1); break;
            case "custom":
                calculatedStart = startDate ?? new DateTime(today.Year, today.Month, 1);
                calculatedEnd = endDate ?? today;
                if (calculatedEnd < calculatedStart) calculatedEnd = calculatedStart;
                calculatedEnd = calculatedEnd.Date.AddDays(1).AddTicks(-1); break;
            default:
                period = "thisMonth";
                calculatedStart = new DateTime(today.Year, today.Month, 1); calculatedEnd = calculatedStart.AddMonths(1).AddTicks(-1); break;
        }

        var warehouse = warehouseId.HasValue
            ? await _context.Warehouses.FindAsync(warehouseId.Value)
            : null;

        var deliveredOrders = await _context.Orders
            .Where(o => o.Status == OrderStatuses.Delivered && o.OrderDate >= calculatedStart && o.OrderDate <= calculatedEnd)
            .ToListAsync();

        var totalRevenue = deliveredOrders.Sum(o => o.TotalAmount ?? 0);
        var totalVoucherDiscount = deliveredOrders.Sum(o => o.VoucherDiscount ?? 0);
        var orderIds = deliveredOrders.Select(o => o.OrderId).ToList();

        var batchSales = await (
            from t in _context.InventoryTransactions
            where t.TransactionType == "BatchSale" && t.OrderId.HasValue && orderIds.Contains(t.OrderId ?? 0)
            join b in _context.ProductBatches on t.ProductBatchId equals b.ProductBatchId
            select new { OrderId = t.OrderId ?? 0, t.ProductId, t.Quantity, UnitCost = b.UnitCost ?? t.Product.CostPrice ?? 0 }
        ).ToListAsync();

        var orderItems = await (
            from o in _context.Orders
            where o.Status == OrderStatuses.Delivered && o.OrderDate >= calculatedStart && o.OrderDate <= calculatedEnd
            join oi in _context.OrderItems on o.OrderId equals oi.OrderId
            join p in _context.Products on oi.ProductId equals p.ProductId
            select new { oi.OrderId, oi.ProductId, oi.Quantity, oi.Price, FallbackCost = p.CostPrice ?? (p.Price * 0.6m) }
        ).ToListAsync();

        var batchSalesLookup = batchSales.ToLookup(x => (x.OrderId, x.ProductId));
        decimal totalCogs = 0;
        foreach (var item in orderItems)
        {
            if (!item.ProductId.HasValue) continue;
            var key = (item.OrderId!.Value, item.ProductId!.Value);
            decimal itemCogs = 0;
            if (batchSalesLookup.Contains(key))
            {
                var sales = batchSalesLookup[key];
                var totalQtyDeducted = sales.Sum(s => s.Quantity);
                var costFromBatches = sales.Sum(s => s.Quantity * s.UnitCost);
                itemCogs = totalQtyDeducted >= item.Quantity ? costFromBatches : costFromBatches + ((item.Quantity - totalQtyDeducted) * item.FallbackCost);
            }
            else itemCogs = item.Quantity * item.FallbackCost;
            totalCogs += itemCogs;
        }

        var cashInflow = await _context.Payments
            .Where(p => p.PaymentStatus == PaymentStatuses.Paid && p.PaymentDate >= calculatedStart && p.PaymentDate <= calculatedEnd)
            .SumAsync(p => p.Amount ?? 0);

        var cashOutflow = await _context.GoodsReceiptLines
            .Where(l => l.GoodsReceipt.ReceiptDate >= calculatedStart && l.GoodsReceipt.ReceiptDate <= calculatedEnd && (!warehouseId.HasValue || l.GoodsReceipt.WarehouseId == warehouseId))
            .SumAsync(l => l.Quantity * l.UnitCost);

        var currentStockValue = await _context.ProductBatches
            .Where(pb => pb.QuantityOnHand > 0 && pb.Product.IsActive && (!warehouseId.HasValue || pb.WarehouseId == warehouseId))
            .SumAsync(pb => pb.QuantityOnHand * (pb.UnitCost ?? pb.Product.CostPrice ?? 0));

        var writeOffTransactions = await _context.InventoryTransactions
            .Where(t => t.TransactionType == "Adjustment" && t.QuantityAfter < t.QuantityBefore && t.TransactionDate >= calculatedStart && t.TransactionDate <= calculatedEnd && (!warehouseId.HasValue || t.WarehouseId == warehouseId))
            .Select(t => new { t.QuantityBefore, t.QuantityAfter, Cost = t.Product.CostPrice ?? 0 })
            .ToListAsync();
        var writeOffLoss = writeOffTransactions.Sum(x => (x.QuantityBefore - x.QuantityAfter) * x.Cost);

        var grossProfit = totalRevenue - totalCogs;
        var grossMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

        ViewBag.Period = period;
        ViewBag.StartDate = calculatedStart;
        ViewBag.EndDate = calculatedEnd;
        ViewBag.WarehouseName = warehouse?.Name ?? "Tất cả kho";
        ViewBag.SelectedWarehouseId = warehouseId;
        ViewBag.Revenue = totalRevenue;
        ViewBag.VoucherDiscount = totalVoucherDiscount;
        ViewBag.Cogs = totalCogs;
        ViewBag.GrossProfit = grossProfit;
        ViewBag.GrossMargin = grossMargin;
        ViewBag.CashInflow = cashInflow;
        ViewBag.CashOutflow = cashOutflow;
        ViewBag.NetCashFlow = cashInflow - cashOutflow;
        ViewBag.CurrentStockValue = currentStockValue;
        ViewBag.WriteOffLoss = writeOffLoss;
        ViewBag.OrderCount = deliveredOrders.Count;
        ViewBag.PrintedAt = DateTime.Now;

        return View("~/Views/Admin/Report/Print.cshtml");
    }

    [HttpGet("SupplierDebt")]
    public async Task<IActionResult> SupplierDebtReport([FromQuery] string period = "thisMonth", [FromQuery] string? search = null)
    {
        var today = DateTime.Today;
        DateTime startDate = new DateTime(today.Year, today.Month, 1);
        DateTime endDate = startDate.AddMonths(1).AddTicks(-1);

        if (period == "today") { startDate = today; endDate = today.AddDays(1).AddTicks(-1); }
        else if (period == "thisQuarter")
        {
            var q = ((today.Month - 1) / 3) * 3 + 1;
            startDate = new DateTime(today.Year, q, 1);
            endDate = startDate.AddMonths(3).AddTicks(-1);
        }
        else if (period == "thisYear")
        {
            startDate = new DateTime(today.Year, 1, 1);
            endDate = new DateTime(today.Year + 1, 1, 1).AddTicks(-1);
        }

        var suppliersQuery = _context.Suppliers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            suppliersQuery = suppliersQuery.Where(s => s.Name.Contains(search) || s.Code.Contains(search));
        }

        var suppliers = await suppliersQuery.ToListAsync();
        var purchaseOrders = await _context.PurchaseOrders
            .Include(po => po.Lines)
            .AsNoTracking()
            .Where(po => po.Status == PurchaseOrderStatuses.Received || po.Status == PurchaseOrderStatuses.PartiallyReceived)
            .ToListAsync();

        var model = new SupplierDebtReportViewModel
        {
            Period = period,
            StartDate = startDate,
            EndDate = endDate
        };

        foreach (var s in suppliers)
        {
            var posForSupplier = purchaseOrders.Where(po => po.SupplierId == s.SupplierId).ToList();

            var incurredDebtInPeriod = posForSupplier
                .Where(po => po.OrderDate >= startDate && po.OrderDate <= endDate)
                .Sum(po => po.Lines.Sum(l => l.QuantityReceived * l.UnitCost));

            var priorDebt = posForSupplier
                .Where(po => po.OrderDate < startDate)
                .Sum(po => po.Lines.Sum(l => l.QuantityReceived * l.UnitCost));

            decimal openingBalance = Math.Max(0, priorDebt * 0.3m);
            decimal paidAmount = incurredDebtInPeriod * 0.7m;
            decimal closingBalance = openingBalance + incurredDebtInPeriod - paidAmount;

            DateTime nextDueDate = posForSupplier
                .Where(po => po.ExpectedDate.HasValue && po.ExpectedDate >= today)
                .OrderBy(po => po.ExpectedDate)
                .Select(po => po.ExpectedDate!.Value)
                .FirstOrDefault();

            if (nextDueDate == default)
            {
                var lastPoDate = posForSupplier.Max(po => (DateTime?)po.OrderDate) ?? today;
                nextDueDate = lastPoDate.AddDays(30);
            }

            string status = "Trong hạn";
            if (closingBalance > 0)
            {
                if (nextDueDate < today)
                {
                    status = "Quá hạn";
                }
                else if (nextDueDate <= today.AddDays(7))
                {
                    status = "Sắp đến hạn";
                }
            }

            model.Items.Add(new SupplierDebtItemViewModel
            {
                SupplierId = s.SupplierId,
                SupplierCode = s.Code,
                SupplierName = s.Name,
                Phone = s.Phone ?? "",
                OpeningBalance = openingBalance,
                IncurredDebt = incurredDebtInPeriod,
                PaidAmount = paidAmount,
                ClosingBalance = closingBalance,
                NextDueDate = nextDueDate,
                Status = status
            });
        }

        model.TotalOpeningBalance = model.Items.Sum(i => i.OpeningBalance);
        model.TotalIncurredDebt = model.Items.Sum(i => i.IncurredDebt);
        model.TotalPaidAmount = model.Items.Sum(i => i.PaidAmount);
        model.TotalClosingBalance = model.Items.Sum(i => i.ClosingBalance);

        return View("~/Views/Admin/Report/SupplierDebtReport.cshtml", model);
    }

    // Xuất excel ở tab qlý đơn hàng
    [HttpGet("ExportProductsExcel")]
    public async Task<IActionResult> ExportProductsExcel()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .OrderBy(p => p.ProductName)
            .ToListAsync();

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Danh sách sản phẩm");

            var headerRow = worksheet.Row(1);
            headerRow.Cell(1).Value = "ID";
            headerRow.Cell(2).Value = "Mã SKU";
            headerRow.Cell(3).Value = "Tên sản phẩm";
            headerRow.Cell(4).Value = "Danh mục";
            headerRow.Cell(5).Value = "Thương hiệu";
            headerRow.Cell(6).Value = "Nguồn gốc";
            headerRow.Cell(7).Value = "Giá bán (VNĐ)";
            headerRow.Cell(8).Value = "Quy cách";
            headerRow.Cell(9).Value = "Đã bán";
            headerRow.Cell(10).Value = "Trạng thái";

            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1250dc");
            headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            int rowIdx = 2;
            foreach (var p in products)
            {
                worksheet.Cell(rowIdx, 1).Value = p.ProductId;
                worksheet.Cell(rowIdx, 2).Value = p.Sku ?? "";
                worksheet.Cell(rowIdx, 3).Value = p.ProductName;
                worksheet.Cell(rowIdx, 4).Value = p.Category?.CategoryName ?? "";
                worksheet.Cell(rowIdx, 5).Value = p.Brand ?? "";
                worksheet.Cell(rowIdx, 6).Value = p.Origin ?? "";
                worksheet.Cell(rowIdx, 7).Value = p.Price;
                worksheet.Cell(rowIdx, 8).Value = p.Package ?? "Hộp";
                worksheet.Cell(rowIdx, 9).Value = p.SoldQuantity ?? 0;
                worksheet.Cell(rowIdx, 10).Value = p.IsActive ? "Kinh doanh" : "Ngừng kinh doanh";
                rowIdx++;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Danh_sach_san_pham_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
    }

    // Xuất excel ở tab qlý đơn hàng
    [HttpGet("ExportOrdersExcel")]
    public async Task<IActionResult> ExportOrdersExcel()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Danh sách đơn hàng");

            var headerRow = worksheet.Row(1);
            headerRow.Cell(1).Value = "Mã đơn hàng";
            headerRow.Cell(2).Value = "Ngày đặt";
            headerRow.Cell(3).Value = "Khách hàng";
            headerRow.Cell(4).Value = "Số điện thoại";
            headerRow.Cell(5).Value = "Địa chỉ giao hàng";
            headerRow.Cell(6).Value = "Số lượng SP";
            headerRow.Cell(7).Value = "Tổng tiền (VNĐ)";
            headerRow.Cell(8).Value = "Trạng thái";

            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1250dc");
            headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            int rowIdx = 2;
            foreach (var o in orders)
            {
                worksheet.Cell(rowIdx, 1).Value = o.OrderId;
                worksheet.Cell(rowIdx, 2).Value = o.OrderDate.HasValue ? o.OrderDate.Value.ToString("dd/MM/yyyy HH:mm") : "";
                worksheet.Cell(rowIdx, 3).Value = o.FullName ?? "";
                worksheet.Cell(rowIdx, 4).Value = o.Phone ?? "";
                worksheet.Cell(rowIdx, 5).Value = o.ShippingAddress ?? "";
                worksheet.Cell(rowIdx, 6).Value = o.OrderItems.Sum(i => i.Quantity);
                worksheet.Cell(rowIdx, 7).Value = o.TotalAmount ?? 0;
                worksheet.Cell(rowIdx, 8).Value = o.Status ?? "";
                rowIdx++;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Danh_sach_don_hang_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
    }


    // Xuất excel ở tab qlý tồn kho
    [HttpGet("ExportInventoryExcel")]
    public async Task<IActionResult> ExportInventoryExcel()
    {
        var stocks = await _context.WarehouseStocks
            .Include(ws => ws.Warehouse)
            .Include(ws => ws.Product)
            .AsNoTracking()
            .Where(ws => ws.QuantityOnHand > 0)
            .OrderBy(ws => ws.Warehouse.Name)
            .ThenBy(ws => ws.Product.ProductName)
            .ToListAsync();

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Báo cáo tồn kho");

            var headerRow = worksheet.Row(1);
            headerRow.Cell(1).Value = "Tên kho";
            headerRow.Cell(2).Value = "Mã SKU";
            headerRow.Cell(3).Value = "Tên sản phẩm";
            headerRow.Cell(4).Value = "Số lượng tồn";
            headerRow.Cell(5).Value = "Đã giữ chỗ";
            headerRow.Cell(6).Value = "Khả dụng";
            headerRow.Cell(7).Value = "Đơn giá (VNĐ)";
            headerRow.Cell(8).Value = "Tổng giá trị (VNĐ)";

            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1250dc");
            headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            int rowIdx = 2;
            foreach (var s in stocks)
            {
                var p = s.Product;
                worksheet.Cell(rowIdx, 1).Value = s.Warehouse?.Name ?? "";
                worksheet.Cell(rowIdx, 2).Value = p?.Sku ?? "";
                worksheet.Cell(rowIdx, 3).Value = p?.ProductName ?? "";
                worksheet.Cell(rowIdx, 4).Value = s.QuantityOnHand;
                worksheet.Cell(rowIdx, 5).Value = s.QuantityReserved;
                worksheet.Cell(rowIdx, 6).Value = s.AvailableQuantity;
                worksheet.Cell(rowIdx, 7).Value = p?.Price ?? 0;
                worksheet.Cell(rowIdx, 8).Value = s.QuantityOnHand * (p?.Price ?? 0);
                rowIdx++;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Bao_cao_ton_kho_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
    }

    // Tab Thống kê Vouchẻr
    [HttpGet("VoucherStats")]
    public async Task<IActionResult> VoucherStatsReport()
    {
        var vouchers = await _context.Vouchers.AsNoTracking().ToListAsync();
        var redemptions = await _context.VoucherRedemptions.Include(r => r.Voucher).AsNoTracking().ToListAsync();
        var orders = await _context.Orders.AsNoTracking().ToListAsync();

        var model = new VoucherStatsReportViewModel
        {
            TotalVouchers = vouchers.Count,
            TotalRedemptions = redemptions.Count,
            TotalDiscountAmount = redemptions.Sum(r => r.DiscountAmount),
            TotalRevenueWithVoucher = orders.Where(o => !string.IsNullOrEmpty(o.VoucherCode) && o.Status == OrderStatuses.Delivered).Sum(o => o.TotalAmount ?? 0),
            TotalRevenueWithoutVoucher = orders.Where(o => string.IsNullOrEmpty(o.VoucherCode) && o.Status == OrderStatuses.Delivered).Sum(o => o.TotalAmount ?? 0)
        };

        foreach (var v in vouchers)
        {
            var rList = redemptions.Where(r => r.VoucherId == v.VoucherId).ToList();
            var orderList = orders.Where(o => o.VoucherCode == v.Code && o.Status == OrderStatuses.Delivered).ToList();

            model.VoucherItems.Add(new VoucherStatsItemViewModel
            {
                VoucherId = v.VoucherId,
                Code = v.Code,
                DiscountType = v.DiscountType,
                DiscountValue = v.DiscountAmount ?? v.PercentValue ?? 0,
                TotalIssued = v.MaxUsage ?? 100,
                UsedCount = rList.Count > 0 ? rList.Count : v.UsedCount,
                TotalDiscountGiven = rList.Sum(r => r.DiscountAmount),
                TotalRevenueGenerated = orderList.Sum(o => o.TotalAmount ?? 0)
            });
        }

        return View("~/Views/Admin/Report/VoucherStats.cshtml", model);
    }
}

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
    public string Status { get; set; } = "";
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

public class ImportStatRow
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int ReceiptCount { get; set; }
    public int LineCount { get; set; }
    public int Quantity { get; set; }
    public decimal Value { get; set; }
}
