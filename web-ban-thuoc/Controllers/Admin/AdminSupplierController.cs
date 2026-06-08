using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin;

[Authorize(Roles = "Admin,WarehouseStaff")]
[Route("AdminSupplier")]
public class AdminSupplierController : Controller
{
    private readonly LongChauDbContext _context;

    public AdminSupplierController(LongChauDbContext context)
    {
        _context = context;
    }

    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var query = _context.Suppliers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.Name.Contains(search) ||
                s.Code.Contains(search) ||
                (s.Phone != null && s.Phone.Contains(search)));
        }

        const int pageSize = 10;
        if (page < 1) page = 1;
        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var suppliers = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        return View("~/Views/Admin/Supplier/Index.cshtml", suppliers);
    }

    [HttpGet]
    [Route("Create")]
    public IActionResult Create() => View("~/Views/Admin/Supplier/Create.cshtml", new SupplierViewModel());

    [HttpPost]
    [Route("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierViewModel model)
    {
        if (await _context.Suppliers.AnyAsync(s => s.Code == model.Code))
            ModelState.AddModelError(nameof(model.Code), "Mã NCC đã tồn tại.");

        if (!ModelState.IsValid)
            return View("~/Views/Admin/Supplier/Create.cshtml", model);

        _context.Suppliers.Add(new Supplier
        {
            Code = model.Code.Trim(),
            Name = model.Name.Trim(),
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address,
            TaxCode = model.TaxCode,
            IsActive = model.IsActive
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã thêm nhà cung cấp.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await _context.Suppliers.FindAsync(id);
        if (s == null) return NotFound();

        return View("~/Views/Admin/Supplier/Edit.cshtml", new SupplierViewModel
        {
            SupplierId = s.SupplierId,
            Code = s.Code,
            Name = s.Name,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            TaxCode = s.TaxCode,
            IsActive = s.IsActive
        });
    }

    [HttpPost]
    [Route("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierViewModel model)
    {
        if (id != model.SupplierId) return BadRequest();

        if (await _context.Suppliers.AnyAsync(s => s.Code == model.Code && s.SupplierId != id))
            ModelState.AddModelError(nameof(model.Code), "Mã NCC đã tồn tại.");

        if (!ModelState.IsValid)
            return View("~/Views/Admin/Supplier/Edit.cshtml", model);

        var s = await _context.Suppliers.FindAsync(id);
        if (s == null) return NotFound();

        s.Code = model.Code.Trim();
        s.Name = model.Name.Trim();
        s.Phone = model.Phone;
        s.Email = model.Email;
        s.Address = model.Address;
        s.TaxCode = model.TaxCode;
        s.IsActive = model.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật nhà cung cấp.";
        return RedirectToAction(nameof(Index));
    }
}
