using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;
using System.Text;

namespace web_ban_thuoc.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("AdminUser")]
public class AdminUserController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly LongChauDbContext _context;

    public AdminUserController(UserManager<IdentityUser> userManager, LongChauDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // GET: AdminUser
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, string? rankFilter)
    {
        var query = _userManager.Users.AsQueryable();

        // Filter theo search term
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => 
                (u.UserName != null && u.UserName.Contains(searchTerm)) ||
                (u.Email != null && u.Email.Contains(searchTerm)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm))
            );
        }

        // Filter theo trạng thái
        if (!string.IsNullOrEmpty(statusFilter))
        {
            if (statusFilter == "active")
            {
                query = query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTime.Now);
            }
            else if (statusFilter == "locked")
            {
                query = query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTime.Now);
            }
        }

        var users = await query
            .OrderByDescending(u => u.Id)
            .ToListAsync();

        // Lấy thông tin rank
        var userRankInfos = await _context.UserRankInfos
            .ToDictionaryAsync(u => u.UserId);

        var userViewModels = new List<UserAdminViewModel>();
        foreach (var user in users)
        {
            var rankInfo = userRankInfos.GetValueOrDefault(user.Id);
            
            // Filter theo rank nếu có
            if (!string.IsNullOrEmpty(rankFilter) && rankInfo?.Rank != rankFilter)
            {
                continue;
            }

            userViewModels.Add(new UserAdminViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                CreatedDate = DateTime.Now, // Tạm thời dùng DateTime.Now vì IdentityUser không có CreatedDate
                IsLocked = user.LockoutEnd != null && user.LockoutEnd > DateTime.Now,
                LockoutEnd = user.LockoutEnd?.DateTime,
                Rank = rankInfo?.Rank ?? "Chưa xếp hạng",
                TotalSpent = rankInfo?.TotalSpent ?? 0,
                TotalSpent6Months = rankInfo?.TotalSpent6Months ?? 0,
                OrderCount = await _context.Orders.CountAsync(o => o.UserId == user.Id),
                LastOrderDate = await _context.Orders
                    .Where(o => o.UserId == user.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => o.OrderDate)
                    .FirstOrDefaultAsync()
            });
        }

        ViewBag.SearchTerm = searchTerm;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.RankFilter = rankFilter;

        return View("~/Views/Admin/User/Index.cshtml", userViewModels);
    }

    // GET: AdminUser/Details/5
    [Route("Details/{id}")]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var rankInfo = await _context.UserRankInfos.FirstOrDefaultAsync(uri => uri.UserId == id);
        var orders = await _context.Orders
            .Where(o => o.UserId == id)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .ToListAsync();

        var userDetailViewModel = new UserDetailViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber ?? "",
            CreatedDate = DateTime.Now, // Tạm thời dùng DateTime.Now vì IdentityUser không có CreatedDate
            IsLocked = user.LockoutEnd != null && user.LockoutEnd > DateTime.Now,
            LockoutEnd = user.LockoutEnd?.DateTime,
            Rank = rankInfo?.Rank ?? "Chưa xếp hạng",
            TotalSpent = rankInfo?.TotalSpent ?? 0,
            TotalSpent6Months = rankInfo?.TotalSpent6Months ?? 0,
            LastRankReset = rankInfo?.LastRankReset,
            Orders = orders,
            TotalOrders = await _context.Orders.CountAsync(o => o.UserId == id),
            TotalSpentAllTime = await _context.Orders
                .Where(o => o.UserId == id && o.Status == "Đã giao")
                .SumAsync(o => o.TotalAmount ?? 0)
        };

        return View("~/Views/Admin/User/Details.cshtml", userDetailViewModel);
    }

    // POST: AdminUser/ToggleLock/5
    [HttpPost]
    [Route("ToggleLock/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy user!" });
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
            {
                // Mở khóa
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = "Đã mở khóa tài khoản user!";
            }
            else
            {
                // Khóa tài khoản
                await _userManager.SetLockoutEndDateAsync(user, DateTime.MaxValue);
                TempData["SuccessMessage"] = "Đã khóa tài khoản user!";
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    // POST: AdminUser/Delete/5
    [HttpPost]
    [Route("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy user!";
                return RedirectToAction(nameof(Index));
            }

            // Xóa các dữ liệu liên quan
            var userRankInfo = await _context.UserRankInfos.FirstOrDefaultAsync(uri => uri.UserId == id);
            if (userRankInfo != null)
            {
                _context.UserRankInfos.Remove(userRankInfo);
            }

            var userVouchers = await _context.UserVouchers.Where(uv => uv.UserId == id).ToListAsync();
            _context.UserVouchers.RemoveRange(userVouchers);

            // Xóa user
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa user thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa user: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa user: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: AdminUser/Export
    [Route("Export")]
    public async Task<IActionResult> Export()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.Id)
            .ToListAsync();

        var userRankInfos = await _context.UserRankInfos.ToDictionaryAsync(uri => uri.UserId);

        var csv = new StringBuilder();
        csv.AppendLine("ID,Tên đăng nhập,Email,Số điện thoại,Ngày tạo,Trạng thái,Cấp độ,Tổng chi tiêu,Số đơn hàng");

        foreach (var user in users)
        {
            var rankInfo = userRankInfos.GetValueOrDefault(user.Id);
            var isLocked = user.LockoutEnd != null && user.LockoutEnd > DateTime.Now;
            var orderCount = await _context.Orders.CountAsync(o => o.UserId == user.Id);

            csv.AppendLine($"{user.Id}," +
                         $"\"{user.UserName ?? ""}\"," +
                         $"\"{user.Email ?? ""}\"," +
                         $"\"{user.PhoneNumber ?? ""}\"," +
                         $"{DateTime.Now:dd/MM/yyyy}," +
                         $"\"{(isLocked ? "Đã khóa" : "Hoạt động")}\"," +
                         $"\"{rankInfo?.Rank ?? "Chưa xếp hạng"}\"," +
                         $"{rankInfo?.TotalSpent ?? 0}," +
                         $"{orderCount}");
        }

        // Thêm BOM UTF-8 để Excel nhận đúng font tiếng Việt
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
        var bytes = bom.Concat(csvBytes).ToArray();
        
        return File(bytes, "text/csv; charset=utf-8", $"danh_sach_nguoi_dung_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
} 