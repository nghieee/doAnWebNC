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
    public async Task<IActionResult> Index(string? status)
    {
        var query = _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Warehouse)
            .Include(po => po.Lines)
            .OrderByDescending(po => po.OrderDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            query = query.Where(po => po.Status == status);

        var list = await query.Select(po => new PurchaseOrderListViewModel
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

    private async Task LoadFormDataAsync()
    {
        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        ViewBag.Warehouses = await _context.Warehouses.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
        ViewBag.Products = await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.ProductName)
            .ToListAsync();
    }
}
