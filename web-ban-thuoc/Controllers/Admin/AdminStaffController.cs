using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin;

[Authorize(Roles = StaffRoles.Admin)]
[Route("AdminStaff")]
public class AdminStaffController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly LongChauDbContext _context;

    public AdminStaffController(
        UserManager<IdentityUser> userManager,
        LongChauDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var staffIds = await GetStaffUserIdsAsync();
        var query = _userManager.Users.Where(u => staffIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(search)) ||
                (u.UserName != null && u.UserName.Contains(search)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
        }

        const int pageSize = 10;
        if (page < 1) page = 1;
        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var list = new List<StaffListItemViewModel>();

        foreach (var user in users)
        {
            var role = await GetPrimaryStaffRoleAsync(user);
            list.Add(new StaffListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "",
                PhoneNumber = user.PhoneNumber,
                Role = role ?? "",
                RoleDisplayName = StaffRoles.GetDisplayName(role ?? ""),
                IsLocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow,
                LockoutEnd = user.LockoutEnd?.UtcDateTime
            });
        }

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        return View("~/Views/Admin/Staff/Index.cshtml", list);
    }

    [HttpGet]
    [Route("Create")]
    public IActionResult Create() => View("~/Views/Admin/Staff/Create.cshtml", new CreateStaffViewModel());

    [HttpPost]
    [Route("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffViewModel model)
    {
        if (!StaffRoles.All.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ.");

        if (!ModelState.IsValid)
            return View("~/Views/Admin/Staff/Create.cshtml", model);

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
            return View("~/Views/Admin/Staff/Create.cshtml", model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            PhoneNumber = model.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View("~/Views/Admin/Staff/Create.cshtml", model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        TempData["SuccessMessage"] = $"Đã tạo tài khoản nhân viên {StaffRoles.GetDisplayName(model.Role)}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("Edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await IsStaffUserAsync(user))
            return NotFound();

        var role = await GetPrimaryStaffRoleAsync(user);
        return View("~/Views/Admin/Staff/Edit.cshtml", new EditStaffViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            Role = role ?? StaffRoles.WarehouseStaff
        });
    }

    [HttpPost]
    [Route("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditStaffViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!StaffRoles.All.Contains(model.Role))
            ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ.");

        if (!string.IsNullOrEmpty(model.NewPassword) && model.NewPassword != model.ConfirmNewPassword)
            ModelState.AddModelError(nameof(model.ConfirmNewPassword), "Mật khẩu xác nhận không khớp.");

        if (!ModelState.IsValid)
            return View("~/Views/Admin/Staff/Edit.cshtml", model);

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await IsStaffUserAsync(user))
            return NotFound();

        var currentRole = await GetPrimaryStaffRoleAsync(user);
        if (currentRole == StaffRoles.Admin && model.Role != StaffRoles.Admin)
        {
            if (!await CanRemoveAdminRoleAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Không thể đổi vai trò của quản trị viên cuối cùng.");
                return View("~/Views/Admin/Staff/Edit.cshtml", model);
            }
        }

        user.PhoneNumber = model.PhoneNumber;
        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var pwdResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!pwdResult.Succeeded)
            {
                foreach (var error in pwdResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View("~/Views/Admin/Staff/Edit.cshtml", model);
            }
        }

        if (currentRole != model.Role)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var toRemove = roles.Where(StaffRoles.IsStaffRole).ToList();
            if (toRemove.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, toRemove);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        TempData["SuccessMessage"] = "Đã cập nhật thông tin nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("ToggleLock/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (id == currentUserId)
            return Json(new { success = false, message = "Không thể khóa tài khoản của chính bạn." });

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await IsStaffUserAsync(user))
            return Json(new { success = false, message = "Không tìm thấy nhân viên!" });

        if (await _userManager.IsInRoleAsync(user, StaffRoles.Admin) && !await CanRemoveAdminRoleAsync(user))
            return Json(new { success = false, message = "Không thể khóa quản trị viên cuối cùng." });

        if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
            await _userManager.SetLockoutEndDateAsync(user, null);
        else
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        return Json(new { success = true });
    }

    [HttpPost]
    [Route("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (id == currentUserId)
        {
            TempData["ErrorMessage"] = "Không thể xóa tài khoản của chính bạn.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await IsStaffUserAsync(user))
        {
            TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(user, StaffRoles.Admin) && !await CanRemoveAdminRoleAsync(user))
        {
            TempData["ErrorMessage"] = "Không thể xóa quản trị viên cuối cùng.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Đã xóa tài khoản nhân viên."
            : "Không thể xóa: " + string.Join(", ", result.Errors.Select(e => e.Description));

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<string>> GetStaffUserIdsAsync()
    {
        var roleIds = await _context.Roles
            .Where(r => StaffRoles.All.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        return await _context.UserRoles
            .Where(ur => roleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<bool> IsStaffUserAsync(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Any(StaffRoles.IsStaffRole);
    }

    private async Task<string?> GetPrimaryStaffRoleAsync(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return StaffRoles.All.FirstOrDefault(roles.Contains);
    }

    private async Task<bool> CanRemoveAdminRoleAsync(IdentityUser user)
    {
        var admins = await _userManager.GetUsersInRoleAsync(StaffRoles.Admin);
        return admins.Count > 1 || !admins.Any(a => a.Id == user.Id);
    }
}
