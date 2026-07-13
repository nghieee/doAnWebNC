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

        public async Task<IActionResult> Index(
            string? search,
            string? type,
            int? warehouseId = null,
            bool hub = false)
        {
            var allWarehouses = await _context.Warehouses
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();

            var transactionsQuery = _context.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.Warehouse)
                .Include(t => t.ProductBatch)
                .AsQueryable();

            if (warehouseId.HasValue)
                transactionsQuery = transactionsQuery.Where(t => t.WarehouseId == warehouseId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                transactionsQuery = transactionsQuery.Where(t => t.Product.ProductName.Contains(search) || (t.Note != null && t.Note.Contains(search)));

            if (!string.IsNullOrWhiteSpace(type) && type != "Tất cả")
                transactionsQuery = transactionsQuery.Where(t => t.TransactionType == type);

            var transactions = await transactionsQuery
                .OrderByDescending(t => t.TransactionDate)
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

            var stockQuery = _context.WarehouseStocks
                .Include(ws => ws.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(ws => ws.Warehouse)
                .Where(ws => ws.Product.IsActive)
                .AsQueryable();

            if (warehouseId.HasValue)
                stockQuery = stockQuery.Where(ws => ws.WarehouseId == warehouseId.Value);

            var warehouseStocks = await stockQuery
                .OrderBy(ws => ws.Product.ProductName)
                .Select(ws => new WarehouseStockViewModel
                {
                    ProductId = ws.ProductId,
                    ProductName = ws.Product.ProductName,
                    Sku = ws.Product.Sku,
                    WarehouseName = ws.Warehouse.Name,
                    WarehouseId = ws.WarehouseId,
                    QuantityOnHand = ws.QuantityOnHand,
                    QuantityReserved = ws.QuantityReserved,
                    ProductImageUrl = ws.Product.ProductImages.Where(pi => pi.IsMain == true).Select(pi => pi.ImageUrl).FirstOrDefault()
                        ?? ws.Product.ProductImages.Select(pi => pi.ImageUrl).FirstOrDefault()
                })
                .ToListAsync();

            var batchQuery = _context.ProductBatches
                .Include(b => b.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(b => b.Warehouse)
                .Where(b => b.QuantityOnHand > 0)
                .AsQueryable();

            if (warehouseId.HasValue)
                batchQuery = batchQuery.Where(b => b.WarehouseId == warehouseId.Value);

            var batches = await batchQuery
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
            ViewBag.AllWarehouses = allWarehouses;
            ViewBag.SelectedWarehouseId = warehouseId;

            return View("~/Views/Admin/Inventory/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductsForWarehouse(int? warehouseId = null, string? search = null)
        {
            var query = _context.Products
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.ProductName.Contains(search) || (p.Sku != null && p.Sku.Contains(search)));

            var products = await query
                .Select(p => new
                {
                    id = p.ProductId,
                    name = p.ProductName,
                    sku = p.Sku,
                    stockQty = warehouseId.HasValue
                        ? _context.WarehouseStocks.Where(ws => ws.ProductId == p.ProductId && ws.WarehouseId == warehouseId.Value).Select(ws => (int?)ws.QuantityOnHand - ws.QuantityReserved).FirstOrDefault() ?? 0
                        : _context.WarehouseStocks.Where(ws => ws.ProductId == p.ProductId).Sum(ws => ws.QuantityOnHand - ws.QuantityReserved),
                    costPrice = p.CostPrice ?? 0
                })
                .OrderBy(p => p.name)
                .Take(100)
                .ToListAsync();

            return Json(products);
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


        // ═══════════════════════════════════════════════════════════════
        // STOCK ADJUSTMENT – Phiếu điều chỉnh tồn kho thủ công (FEFO)
        // ═══════════════════════════════════════════════════════════════

        public async Task<IActionResult> StockAdjustments(
            string? search,
            string? status,
            string? type,
            int? warehouseId)
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();

            var query = _context.StockAdjustments
                .Include(sa => sa.Warehouse)
                .AsQueryable();

            if (warehouseId.HasValue)
                query = query.Where(sa => sa.WarehouseId == warehouseId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(sa => sa.AdjustmentCode.Contains(search)
                    || (sa.Note != null && sa.Note.Contains(search))
                    || (sa.RequestedBy != null && sa.RequestedBy.Contains(search)));

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
                query = query.Where(sa => sa.Status == status);

            if (!string.IsNullOrWhiteSpace(type) && type != "Tất cả")
                query = query.Where(sa => sa.AdjustmentType == type);

            var adjustments = await query
                .OrderByDescending(sa => sa.CreatedAt)
                .Select(sa => new StockAdjustmentListViewModel
                {
                    StockAdjustmentId = sa.StockAdjustmentId,
                    AdjustmentCode = sa.AdjustmentCode,
                    AdjustmentType = sa.AdjustmentType,
                    Reason = sa.Reason,
                    Status = sa.Status,
                    WarehouseName = sa.Warehouse.Name,
                    RequestedBy = sa.RequestedBy,
                    ApprovedBy = sa.ApprovedBy,
                    CreatedAt = sa.CreatedAt,
                    ProcessedAt = sa.ProcessedAt,
                    TotalLines = sa.Details.Count,
                    TotalQuantity = sa.Details.Sum(d => d.Quantity)
                })
                .ToListAsync();

            var pendingCount = await _context.StockAdjustments
                .CountAsync(sa => sa.Status == StockAdjustmentStatuses.Pending);

            var model = new StockAdjustmentIndexViewModel
            {
                Adjustments = adjustments,
                Warehouses = warehouses,
                SelectedWarehouseId = warehouseId,
                Search = search,
                StatusFilter = status,
                TypeFilter = type,
                PendingCount = pendingCount
            };

            return View("~/Views/Admin/Inventory/StockAdjustments.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateStockAdjustment()
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();

            ViewBag.Warehouses = warehouses;
            ViewBag.AdjustmentTypes = new[] {
                StockAdjustmentTypes.Export,
                StockAdjustmentTypes.Import,
                StockAdjustmentTypes.Positive,
                StockAdjustmentTypes.Negative
            };
            ViewBag.Reasons = StockAdjustmentReasons.All;

            return View("~/Views/Admin/Inventory/CreateStockAdjustment.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStockAdjustment(CreateStockAdjustmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(CreateStockAdjustment));
            }

            if (model.Lines == null || model.Lines.Count == 0 || model.Lines.All(l => l.ProductId == 0))
            {
                TempData["ErrorMessage"] = "Phải thêm ít nhất một sản phẩm vào phiếu.";
                return RedirectToAction(nameof(CreateStockAdjustment));
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                model.RequestedBy = User.Identity?.Name;

                var adjustment = await _inventoryService.CreateStockAdjustmentAsync(model, userId);

                // Theo nghiệp vụ: WarehouseStaff chỉ "đề xuất" -> Status=Pending.
                // Admin có quyền tạo và duyệt luôn trong cùng thao tác.
                if (User.IsInRole("Admin"))
                {
                    await _inventoryService.ApproveStockAdjustmentAsync(adjustment.StockAdjustmentId, userId);
                    TempData["SuccessMessage"] = $"Phiếu {adjustment.AdjustmentCode} đã được tạo và duyệt thành công! Tồn kho đã được cập nhật.";
                }
                else if (User.IsInRole("WarehouseStaff"))
                {
                    TempData["SuccessMessage"] = $"Phiếu {adjustment.AdjustmentCode} đã được tạo và đang chờ Admin duyệt.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Phiếu {adjustment.AdjustmentCode} đã được tạo và đang chờ duyệt.";
                }

                return RedirectToAction(nameof(StockAdjustments));
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(CreateStockAdjustment));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(CreateStockAdjustment));
            }
        }

        [HttpGet]
        public async Task<IActionResult> StockAdjustmentDetails(int id)
        {
            var adjustment = await _context.StockAdjustments
                .Include(sa => sa.Warehouse)
                .Include(sa => sa.Details)
                    .ThenInclude(d => d.Product)
                .Include(sa => sa.Details)
                    .ThenInclude(d => d.ProductBatch)
                .FirstOrDefaultAsync(sa => sa.StockAdjustmentId == id);

            if (adjustment == null)
                return NotFound();

            var details = adjustment.Details.Select(d => new StockAdjustmentDetailViewModel
            {
                StockAdjustmentDetailId = d.StockAdjustmentDetailId,
                ProductId = d.ProductId,
                ProductName = d.Product.ProductName,
                Sku = d.Product.Sku,
                ProductBatchId = d.ProductBatchId,
                BatchNo = d.ProductBatch?.BatchNo,
                ExpiryDate = d.ProductBatch?.ExpiryDate,
                Quantity = d.Quantity,
                UnitCost = d.UnitCost,
                Note = d.Note
            }).ToList();

            var model = new StockAdjustmentDetailPageViewModel
            {
                Adjustment = adjustment,
                Details = details
            };

            return View("~/Views/Admin/Inventory/StockAdjustmentDetails.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveStockAdjustment(int id)
        {
            try
            {
                var approvedByUserId = _userManager.GetUserId(User) ?? "";
                await _inventoryService.ApproveStockAdjustmentAsync(id, approvedByUserId);
                TempData["SuccessMessage"] = "Phiếu đã được duyệt. Tồn kho đã được cập nhật.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(StockAdjustmentDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectStockAdjustment(int id, string? reason)
        {
            try
            {
                var rejectedByUserId = _userManager.GetUserId(User) ?? "";
                await _inventoryService.RejectStockAdjustmentAsync(id, rejectedByUserId, reason);
                TempData["SuccessMessage"] = "Phiếu đã bị từ chối.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction(nameof(StockAdjustmentDetails), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetFefoBatches(int productId, int warehouseId, int? excludeBatchId = null)
        {
            var batches = await _inventoryService.GetFefoBatchesAsync(productId, warehouseId, excludeBatchId);
            return Json(batches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStockAdjustment(int id)
        {
            var adjustment = await _context.StockAdjustments.FindAsync(id);
            if (adjustment == null)
                return Json(new { success = false, message = "Không tìm thấy phiếu." });

            if (adjustment.Status != StockAdjustmentStatuses.Pending)
                return Json(new { success = false, message = "Chỉ có thể xóa phiếu đang chờ duyệt." });

            _context.StockAdjustmentDetails.RemoveRange(adjustment.Details);
            _context.StockAdjustments.Remove(adjustment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa phiếu thành công." });
        }

        [HttpGet]
        public async Task<IActionResult> PrintStockAdjustment(int id)
        {
            var adjustment = await _context.StockAdjustments
                .Include(sa => sa.Warehouse)
                .Include(sa => sa.Details)
                    .ThenInclude(d => d.Product)
                .Include(sa => sa.Details)
                    .ThenInclude(d => d.ProductBatch)
                .FirstOrDefaultAsync(sa => sa.StockAdjustmentId == id);

            if (adjustment == null)
                return NotFound();

            var details = adjustment.Details.Select(d => new StockAdjustmentDetailViewModel
            {
                StockAdjustmentDetailId = d.StockAdjustmentDetailId,
                ProductId = d.ProductId,
                ProductName = d.Product.ProductName,
                Sku = d.Product.Sku,
                ProductBatchId = d.ProductBatchId,
                BatchNo = d.ProductBatch?.BatchNo,
                ExpiryDate = d.ProductBatch?.ExpiryDate,
                Quantity = d.Quantity,
                UnitCost = d.UnitCost,
                Note = d.Note
            }).ToList();

            var model = new StockAdjustmentDetailPageViewModel
            {
                Adjustment = adjustment,
                Details = details
            };

            return View("~/Views/Admin/Inventory/PrintStockAdjustment.cshtml", model);
        }
    }
}
