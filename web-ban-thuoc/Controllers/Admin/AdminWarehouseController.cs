using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin;

[Authorize(Roles = "Admin,WarehouseStaff")]
[Route("AdminWarehouse")]
public class AdminWarehouseController : Controller
{
    private readonly LongChauDbContext _context;

    public AdminWarehouseController(LongChauDbContext context)
    {
        _context = context;
    }

    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        var warehouses = await _context.Warehouses
            .Where(w => w.IsActive)
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseListViewModel
            {
                WarehouseId = w.WarehouseId,
                Name = w.Name,
                Address = w.Address,
                IsDefault = w.IsDefault,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();

        // Tính tồn kho cho từng kho
        var stockData = await _context.WarehouseStocks
            .Include(ws => ws.Product)
            .Where(ws => ws.Product.IsActive)
            .ToListAsync();

        var batchData = await _context.ProductBatches
            .Include(b => b.Product)
            .Where(b => b.QuantityOnHand > 0 && b.Product.IsActive)
            .ToListAsync();

        var lowStock = await _context.Products
            .Where(p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= 10)
            .CountAsync();

        var outOfStock = await _context.Products
            .CountAsync(p => p.IsActive && p.StockQuantity <= 0);

        var warehouseSummaries = new List<WarehouseSummaryViewModel>();

        foreach (var wh in warehouses)
        {
            var whStocks = stockData.Where(s => s.WarehouseId == wh.WarehouseId).ToList();
            var whBatches = batchData.Where(b => b.WarehouseId == wh.WarehouseId).ToList();

            var totalOnHand = whStocks.Sum(s => s.QuantityOnHand);
            var totalReserved = whStocks.Sum(s => s.QuantityReserved);
            var stockValue = whBatches.Sum(b => b.QuantityOnHand * (b.UnitCost ?? b.Product.CostPrice ?? 0));
            var activeBatchCount = whBatches.Count;
            var expiringBatches = whBatches
                .Count(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value <= DateTime.Today.AddDays(90));

            warehouseSummaries.Add(new WarehouseSummaryViewModel
            {
                WarehouseId = wh.WarehouseId,
                Name = wh.Name,
                Address = wh.Address,
                IsDefault = wh.IsDefault,
                TotalOnHand = totalOnHand,
                TotalReserved = totalReserved,
                Available = totalOnHand - totalReserved,
                StockValue = stockValue,
                ActiveBatchCount = activeBatchCount,
                ExpiringBatchCount = expiringBatches,
                ProductCount = whStocks.Select(s => s.ProductId).Distinct().Count()
            });
        }

        var model = new WarehouseIndexViewModel
        {
            Warehouses = warehouseSummaries,
            LowStockCount = lowStock,
            OutOfStockCount = outOfStock
        };

        return View("~/Views/Admin/Warehouse/Index.cshtml", model);
    }

    [HttpGet]
    [Route("Create")]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View("~/Views/Admin/Warehouse/Create.cshtml", new WarehouseCreateViewModel());
    }

    [HttpPost]
    [Route("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(WarehouseCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Warehouse/Create.cshtml", model);

        var exists = await _context.Warehouses.AnyAsync(w => w.Name == model.Name.Trim());
        if (exists)
        {
            ModelState.AddModelError("Name", "Tên kho đã tồn tại.");
            return View("~/Views/Admin/Warehouse/Create.cshtml", model);
        }

        var isDefault = !await _context.Warehouses.AnyAsync(w => w.IsActive && w.IsDefault);

        var warehouse = new Warehouse
        {
            Name = model.Name.Trim(),
            Address = model.Address?.Trim(),
            IsActive = true,
            IsDefault = isDefault || model.IsDefault,
            CreatedAt = DateTime.Now
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã tạo kho \"{warehouse.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("Edit/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse == null) return NotFound();

        var model = new WarehouseCreateViewModel
        {
            Name = warehouse.Name,
            Address = warehouse.Address,
            IsDefault = warehouse.IsDefault
        };

        ViewBag.WarehouseId = id;
        ViewBag.WarehouseName = warehouse.Name;
        return View("~/Views/Admin/Warehouse/Edit.cshtml", model);
    }

    [HttpPost]
    [Route("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, WarehouseCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.WarehouseId = id;
            return View("~/Views/Admin/Warehouse/Edit.cshtml", model);
        }

        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse == null) return NotFound();

        var exists = await _context.Warehouses.AnyAsync(w => w.WarehouseId != id && w.Name == model.Name.Trim());
        if (exists)
        {
            ModelState.AddModelError("Name", "Tên kho đã tồn tại.");
            ViewBag.WarehouseId = id;
            return View("~/Views/Admin/Warehouse/Edit.cshtml", model);
        }

        warehouse.Name = model.Name.Trim();
        warehouse.Address = model.Address?.Trim();

        if (model.IsDefault && !warehouse.IsDefault)
        {
            var others = await _context.Warehouses.Where(w => w.WarehouseId != id && w.IsActive).ToListAsync();
            foreach (var w in others) w.IsDefault = false;
            warehouse.IsDefault = true;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật kho.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("Delete/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse == null) return NotFound();

        var hasStock = await _context.WarehouseStocks.AnyAsync(ws => ws.WarehouseId == id && ws.QuantityOnHand > 0);
        if (hasStock)
        {
            TempData["ErrorMessage"] = "Kho còn tồn kho. Không thể xóa.";
            return RedirectToAction(nameof(Index));
        }

        var hasBatch = await _context.ProductBatches.AnyAsync(b => b.WarehouseId == id && b.QuantityOnHand > 0);
        if (hasBatch)
        {
            TempData["ErrorMessage"] = "Kho còn lô hàng. Không thể xóa.";
            return RedirectToAction(nameof(Index));
        }

        var wasDefault = warehouse.IsDefault;
        _context.Warehouses.Remove(warehouse);
        await _context.SaveChangesAsync();

        if (wasDefault)
        {
            var another = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsActive);
            if (another != null) another.IsDefault = true;
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Đã xóa kho.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("SetDefault/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse == null) return NotFound();

        var all = await _context.Warehouses.Where(w => w.IsActive).ToListAsync();
        foreach (var w in all) w.IsDefault = (w.WarehouseId == id);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã đặt \"{warehouse.Name}\" làm kho mặc định.";
        return RedirectToAction(nameof(Index));
    }
}
