using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;
using web_ban_thuoc.Services;

namespace web_ban_thuoc.Controllers.Admin
{
    [Authorize(Roles = "Admin,WarehouseStaff")]
    public class AdminInventoryController : Controller
    {
        private readonly LongChauDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminInventoryController(LongChauDbContext context, IInventoryService inventoryService, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _inventoryService = inventoryService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, string? type, bool hub = false)
        {
            var query = _context.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.Warehouse)
                .Include(t => t.ProductBatch)
                .OrderByDescending(t => t.TransactionDate)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Product.ProductName.Contains(search) || (t.Note != null && t.Note.Contains(search)));

            if (!string.IsNullOrWhiteSpace(type) && type != "Tất cả")
                query = query.Where(t => t.TransactionType == type);

            var transactions = await query
                .Take(200)
                .Select(t => new InventoryTransactionViewModel
                {
                    TransactionId = t.TransactionId,
                    ProductName = t.Product.ProductName,
                    WarehouseName = t.Warehouse.Name,
                    TransactionType = t.TransactionType,
                    Quantity = t.Quantity,
                    QuantityBefore = t.QuantityBefore,
                    QuantityAfter = t.QuantityAfter,
                    OrderId = t.OrderId,
                    SupplierName = t.SupplierName,
                    BatchNo = t.ProductBatch != null ? t.ProductBatch.BatchNo : null,
                    Note = t.Note,
                    TransactionDate = t.TransactionDate
                })
                .ToListAsync();

            var warehouseStocks = await _context.WarehouseStocks
                .Include(ws => ws.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(ws => ws.Warehouse)
                .Where(ws => ws.Product.IsActive)
                .OrderBy(ws => ws.Product.ProductName)
                .Select(ws => new WarehouseStockViewModel
                {
                    ProductId = ws.ProductId,
                    ProductName = ws.Product.ProductName,
                    Sku = ws.Product.Sku,
                    WarehouseName = ws.Warehouse.Name,
                    QuantityOnHand = ws.QuantityOnHand,
                    QuantityReserved = ws.QuantityReserved,
                    ProductImageUrl = ws.Product.ProductImages.Where(pi => pi.IsMain == true).Select(pi => pi.ImageUrl).FirstOrDefault()
                        ?? ws.Product.ProductImages.Select(pi => pi.ImageUrl).FirstOrDefault()
                })
                .ToListAsync();

            var batches = await _context.ProductBatches
                .Include(b => b.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(b => b.Warehouse)
                .Where(b => b.QuantityOnHand > 0)
                .OrderBy(b => b.ExpiryDate == null)
                .ThenBy(b => b.ExpiryDate)
                .Take(50)
                .Select(b => new ProductBatchViewModel
                {
                    ProductBatchId = b.ProductBatchId,
                    ProductName = b.Product.ProductName,
                    BatchNo = b.BatchNo,
                    ExpiryDate = b.ExpiryDate,
                    QuantityOnHand = b.QuantityOnHand,
                    WarehouseName = b.Warehouse.Name,
                    ProductImageUrl = b.Product.ProductImages.Where(pi => pi.IsMain == true).Select(pi => pi.ImageUrl).FirstOrDefault()
                        ?? b.Product.ProductImages.Select(pi => pi.ImageUrl).FirstOrDefault()
                })
                .ToListAsync();

            var lowStock = warehouseStocks
                .GroupBy(ws => new { ws.ProductId, ws.ProductName })
                .Select(g => new LowStockAlertViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Available = g.Sum(x => x.Available)
                })
                .Where(x => x.Available <= 10)
                .OrderBy(x => x.Available)
                .Take(10)
                .ToList();

            var model = new InventoryIndexViewModel
            {
                Transactions = transactions,
                WarehouseStocks = warehouseStocks,
                Batches = batches,
                LowStockAlerts = lowStock,
                PendingPurchaseOrders = await _context.PurchaseOrders.CountAsync(p =>
                    p.Status == PurchaseOrderStatuses.Confirmed || p.Status == PurchaseOrderStatuses.PartiallyReceived),
                PendingOrderConfirmations = await _context.Orders.CountAsync(o => o.Status == OrderStatuses.PendingConfirmation),
                Search = search,
                Type = type
            };

            ViewBag.ShowHub = hub;

            return View("~/Views/Admin/Inventory/Index.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(InventoryImportViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu nhập kho không hợp lệ!" });

            try
            {
                var adminId = _userManager.GetUserId(User);
                string? supplierName = model.SupplierName;
                if (model.SupplierId.HasValue)
                {
                    var sup = await _context.Suppliers.FindAsync(model.SupplierId.Value);
                    supplierName = sup?.Name ?? supplierName;
                }

                await _inventoryService.ImportStockAsync(
                    model.ProductId, model.Quantity, supplierName, model.UnitCost, model.Note,
                    adminId, model.WarehouseId, model.BatchNo, model.ExpiryDate, model.SupplierId);

                return Json(new { success = true, message = "Nhập kho thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(InventoryAdjustViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu điều chỉnh không hợp lệ!" });

            try
            {
                var adminId = _userManager.GetUserId(User);
                await _inventoryService.AdjustStockAsync(model.ProductId, model.NewQuantity, model.Note, adminId, model.WarehouseId);
                return Json(new { success = true, message = "Điều chỉnh tồn kho thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
