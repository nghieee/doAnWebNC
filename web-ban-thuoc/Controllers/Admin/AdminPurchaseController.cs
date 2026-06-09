using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;
using web_ban_thuoc.Services;

namespace web_ban_thuoc.Controllers.Admin;

[Authorize(Roles = "Admin,WarehouseStaff")]
[Route("AdminPurchase")]
public class AdminPurchaseController : Controller
{
    private readonly LongChauDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly UserManager<IdentityUser> _userManager;

    public AdminPurchaseController(LongChauDbContext context, IInventoryService inventoryService, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _inventoryService = inventoryService;
        _userManager = userManager;
    }

    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index(string? status, int page = 1)
    {
        var query = _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Warehouse)
            .Include(po => po.Lines)
            .OrderByDescending(po => po.OrderDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            query = query.Where(po => po.Status == status);

        const int pageSize = 10;
        if (page < 1) page = 1;
        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var list = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(po => new PurchaseOrderListViewModel
        {
            PurchaseOrderId = po.PurchaseOrderId,
            OrderCode = po.OrderCode,
            SupplierName = po.Supplier.Name,
            WarehouseName = po.Warehouse.Name,
            Status = po.Status,
            OrderDate = po.OrderDate,
            TotalOrdered = po.Lines.Sum(l => l.QuantityOrdered),
            TotalReceived = po.Lines.Sum(l => l.QuantityReceived)
        }).ToListAsync();

        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        return View("~/Views/Admin/Purchase/Index.cshtml", list);
    }

    [HttpGet]
    [Route("Create")]
    public async Task<IActionResult> Create()
    {
        await LoadFormDataAsync();
        return View("~/Views/Admin/Purchase/Create.cshtml", new CreatePurchaseOrderViewModel());
    }

    [HttpPost]
    [Route("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePurchaseOrderViewModel model)
    {
        var lines = model.Lines?.Where(l => l.ProductId > 0 && l.QuantityOrdered > 0).ToList() ?? new();
        if (lines.Count == 0)
            ModelState.AddModelError(string.Empty, "Thêm ít nhất một dòng sản phẩm.");

        if (lines.Count > 0)
        {
            var productIds = lines.Select(l => l.ProductId).ToList();
            var mismatched = await _context.Products
                .Where(p => productIds.Contains(p.ProductId)
                    && (p.SupplierId == null || p.SupplierId != model.SupplierId))
                .Select(p => p.ProductName)
                .ToListAsync();
            if (mismatched.Count > 0)
                ModelState.AddModelError(string.Empty,
                    "Các sản phẩm sau không thuộc NCC đã chọn: " + string.Join(", ", mismatched));
        }

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync();
            return View("~/Views/Admin/Purchase/Create.cshtml", model);
        }

        try
        {
            var userId = _userManager.GetUserId(User);
            await _inventoryService.CreatePurchaseOrderAsync(new PurchaseOrderInput
            {
                SupplierId = model.SupplierId,
                WarehouseId = model.WarehouseId,
                ExpectedDate = model.ExpectedDate,
                Note = model.Note,
                Lines = lines.Select(l => new PurchaseOrderLineInput
                {
                    ProductId = l.ProductId,
                    QuantityOrdered = l.QuantityOrdered,
                    UnitCost = l.UnitCost
                }).ToList()
            }, userId);

            TempData["SuccessMessage"] = "Đã tạo đơn đặt hàng.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadFormDataAsync();
            return View("~/Views/Admin/Purchase/Create.cshtml", model);
        }
    }

    [HttpGet]
    [Route("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Lines)
            .ThenInclude(l => l.Product)
                .ThenInclude(p => p.ProductImages)
            .Include(p => p.GoodsReceipts)
            .ThenInclude(r => r.Lines)
            .ThenInclude(rl => rl.Product)
                .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == id);

        if (po == null) return NotFound();
        return View("~/Views/Admin/Purchase/Details.cshtml", po);
    }

    [HttpPost]
    [Route("Confirm/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        try
        {
            await _inventoryService.ConfirmPurchaseOrderAsync(id);
            TempData["SuccessMessage"] = "Đã xác nhận đơn đặt hàng.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Route("Receive/{id}")]
    public async Task<IActionResult> Receive(int id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Lines)
            .ThenInclude(l => l.Product)
                .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == id);

        if (po == null) return NotFound();
        if (po.Status == PurchaseOrderStatuses.Draft || po.Status == PurchaseOrderStatuses.Cancelled)
        {
            TempData["ErrorMessage"] = "Đơn chưa xác nhận hoặc đã hủy.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new CreateGoodsReceiptViewModel
        {
            PurchaseOrderId = po.PurchaseOrderId,
            SupplierId = po.SupplierId,
            WarehouseId = po.WarehouseId,
            Lines = po.Lines.Where(l => l.RemainingQuantity > 0).Select(l => new GoodsReceiptLineForm
            {
                ProductId = l.ProductId,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                BatchNo = $"LOT-{l.ProductId}-{DateTime.Now:yyyyMMdd}",
                Quantity = l.RemainingQuantity,
                UnitCost = l.UnitCost
            }).ToList()
        };

        if (model.Lines.Count == 0)
        {
            TempData["ErrorMessage"] = "Đơn đã nhận đủ hàng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ViewBag.PurchaseOrder = po;
        ViewBag.Products = po.Lines.Select(l => l.Product).Distinct().ToList();
        return View("~/Views/Admin/Purchase/Receive.cshtml", model);
    }

    [HttpPost]
    [Route("Receive/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int id, CreateGoodsReceiptViewModel model)
    {
        var lines = model.Lines?.Where(l => l.ProductId > 0 && l.Quantity > 0).ToList() ?? new();
        if (lines.Count == 0)
        {
            TempData["ErrorMessage"] = "Thêm ít nhất một dòng nhập.";
            return RedirectToAction(nameof(Receive), new { id });
        }

        try
        {
            var userId = _userManager.GetUserId(User);
            var receipt = await _inventoryService.ReceiveGoodsAsync(new GoodsReceiptInput
            {
                PurchaseOrderId = id,
                SupplierId = model.SupplierId,
                WarehouseId = model.WarehouseId,
                Note = model.Note,
                Lines = lines.Select(l => new GoodsReceiptLineInput
                {
                    ProductId = l.ProductId,
                    PurchaseOrderLineId = l.PurchaseOrderLineId,
                    BatchNo = l.BatchNo,
                    ExpiryDate = l.ExpiryDate,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost
                }).ToList()
            }, userId);

            TempData["SuccessMessage"] = $"Nhập kho thành công — phiếu {receipt.ReceiptCode}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Receive), new { id });
        }
    }

    [HttpGet]
    [Route("ProductsBySupplier")]
    public async Task<IActionResult> ProductsBySupplier(int supplierId)
    {
        if (supplierId <= 0)
            return Json(Array.Empty<object>());

        var products = await _context.Products
            .Where(p => p.IsActive && p.SupplierId == supplierId)
            .OrderBy(p => p.ProductName)
            .Select(p => new { id = p.ProductId, name = p.ProductName, costPrice = p.CostPrice ?? 0 })
            .ToListAsync();

        return Json(products);
    }

    [HttpGet]
    [Route("Replenishment")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Replenishment()
    {
        // Calculate available stock per product (on hand - reserved - expired)
        var today = DateTime.Today;
        // Get stock quantities per product from all warehouses
        var stockRows = await _context.WarehouseStocks
            .Select(ws => new { ws.ProductId, ws.QuantityOnHand, ws.QuantityReserved })
            .ToListAsync();
        var stockMap = stockRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g =>
                {
                    var onHand = g.Sum(x => x.QuantityOnHand);
                    var reserved = g.Sum(x => x.QuantityReserved);
                    return onHand - reserved; // expired will be subtracted later
                });
        // Get expired quantities per product from batches with expiry <= today
        var expiredRows = await _context.ProductBatches
            .Where(b => b.ExpiryDate != null && b.ExpiryDate <= today && b.QuantityOnHand > 0)
            .GroupBy(b => b.ProductId)
            .Select(g => new { ProductId = g.Key, ExpiredQty = g.Sum(b => b.QuantityOnHand) })
            .ToListAsync();
        var expiredMap = expiredRows.ToDictionary(x => x.ProductId, x => x.ExpiredQty);
        // Adjust stockMap by subtracting expired quantities
        foreach (var kvp in expiredMap)
        {
            if (stockMap.ContainsKey(kvp.Key))
                stockMap[kvp.Key] = Math.Max(0, stockMap[kvp.Key] - kvp.Value);
        }
        // Load all active products with supplier and images
        var allProducts = await _context.Products
            .Include(p => p.Supplier)
            .Include(p => p.ProductImages)
            .Where(p => p.IsActive && p.SupplierId != null)
            .ToListAsync();
        // Identify low‑stock products (available <= threshold)
        var lowStock = allProducts
            .Where(p =>
            {
                var available = stockMap.GetValueOrDefault(p.ProductId, 0);
                var threshold = p.MinStockLevel > 0 ? p.MinStockLevel : 10;
                return available <= threshold;
            })
            .OrderBy(p => stockMap.GetValueOrDefault(p.ProductId, 0))
            .ToList();
        // Group by supplier for view model
        var grouped = lowStock
            .GroupBy(p => p.Supplier!)
            .Select(g => new ReplenishmentGroupViewModel
            {
                Supplier = g.Key,
                Products = g.Select(p => new ReplenishmentItemViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName ?? string.Empty,
                    Sku = p.Sku ?? string.Empty,
                    StockQuantity = stockMap.GetValueOrDefault(p.ProductId, 0),
                    CostPrice = p.CostPrice ?? 0,
                    ImageUrl = p.ProductImages?.FirstOrDefault(i => i.IsMain == true)?.ImageUrl
                               ?? p.ProductImages?.FirstOrDefault()?.ImageUrl
                               ?? "sanpham.png",
                    MinStockLevel = p.MinStockLevel > 0 ? p.MinStockLevel : 10
                }).ToList()
            })
            .ToList();
        return View("~/Views/Admin/Purchase/Replenishment.cshtml", grouped);
    }

    private async Task LoadFormDataAsync()
    {
        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
        ViewBag.Products = await _context.Products
            .Include(p => p.ProductImages)
            .Where(p => p.IsActive)
            .OrderBy(p => p.ProductName)
            .ToListAsync();

        var stockRows = await _context.WarehouseStocks
            .Select(ws => new { ws.ProductId, ws.QuantityOnHand, ws.QuantityReserved })
            .ToListAsync();
        ViewBag.StockByProduct = stockRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => Math.Max(0, x.QuantityOnHand - x.QuantityReserved)));
    }
}
