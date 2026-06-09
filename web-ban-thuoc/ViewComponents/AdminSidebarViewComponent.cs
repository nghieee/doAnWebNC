using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.ViewComponents;

public class AdminSidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var user = HttpContext.User;
        var roles = StaffRoles.All.Where(user.IsInRole).ToList();
        var primaryRole = roles.FirstOrDefault() ?? "";

        var model = new AdminSidebarViewModel
        {
            UserName = user.Identity?.Name ?? "Nhân viên",
            RoleDisplayName = StaffRoles.GetDisplayName(primaryRole),
            MenuGroups = BuildMenuGroups(user)
        };

        return View(model);
    }

    private static List<AdminNavGroup> BuildMenuGroups(ClaimsPrincipal user)
    {
        var groups = new List<AdminNavGroup>();

        void AddGroup(string title, string icon, params AdminNavItem[] items)
        {
            var navItems = items.Where(i => i.Roles.Any(user.IsInRole)).ToList();
            if (navItems.Count > 0)
                groups.Add(new AdminNavGroup { Title = title, IconClass = icon, Items = navItems });
        }

        AddGroup("Tổng quan", "fa-solid fa-chart-pie",
            new AdminNavItem { Title = "Báo cáo thống kê", Url = "/AdminReport", IconClass = "fa-solid fa-chart-line", PathPrefix = "/AdminReport", Roles = [StaffRoles.Admin] });

        AddGroup("Bán hàng", "fa-solid fa-store",
            new AdminNavItem { Title = "Đơn hàng", Url = "/AdminOrder", IconClass = "fa-solid fa-cart-shopping", PathPrefix = "/AdminOrder", Roles = [StaffRoles.Admin, StaffRoles.WarehouseStaff, StaffRoles.CustomerSupport] },
            new AdminNavItem { Title = "Tin nhắn", Url = "/admin/chat", IconClass = "fa-regular fa-message", PathPrefix = "/admin/chat", Roles = [StaffRoles.Admin, StaffRoles.CustomerSupport] });

        AddGroup("Sản phẩm", "fa-solid fa-pills",
            new AdminNavItem { Title = "Danh sách SP", Url = "/AdminProduct", IconClass = "fa-solid fa-box", PathPrefix = "/AdminProduct", Roles = [StaffRoles.Admin], ExcludePaths = ["/AdminProduct/Import"] },
            new AdminNavItem { Title = "Nhập từ Excel", Url = "/AdminProduct/Import", IconClass = "fa-solid fa-file-excel", PathPrefix = "/AdminProduct/Import", Roles = [StaffRoles.Admin], IsSubItem = true },
            new AdminNavItem { Title = "Danh mục", Url = "/AdminCategory", IconClass = "fa-solid fa-tags", PathPrefix = "/AdminCategory", Roles = [StaffRoles.Admin] });

        AddGroup("Kho & NCC", "fa-solid fa-warehouse",
            new AdminNavItem { Title = "Quản lý kho", Url = "/AdminInventory", IconClass = "fa-solid fa-boxes-stacked", PathPrefix = "/AdminInventory", Roles = [StaffRoles.Admin, StaffRoles.WarehouseStaff] },
            new AdminNavItem { Title = "Nhà cung cấp", Url = "/AdminSupplier", IconClass = "fa-solid fa-truck", PathPrefix = "/AdminSupplier", Roles = [StaffRoles.Admin, StaffRoles.WarehouseStaff] },
            new AdminNavItem { Title = "Đặt hàng NCC", Url = "/AdminPurchase", IconClass = "fa-solid fa-file-invoice", PathPrefix = "/AdminPurchase", Roles = [StaffRoles.Admin, StaffRoles.WarehouseStaff] });

        AddGroup("Marketing", "fa-solid fa-bullhorn",
            new AdminNavItem { Title = "Voucher", Url = "/admin/Voucher", IconClass = "fa-solid fa-ticket", PathPrefix = "/admin/Voucher", Roles = [StaffRoles.Admin], ExcludePaths = ["/admin/VoucherRedemptions"] },
            new AdminNavItem { Title = "Loyalty & Điểm", Url = "/AdminLoyalty", IconClass = "fa-solid fa-medal", PathPrefix = "/AdminLoyalty", Roles = [StaffRoles.Admin], ExcludePaths = ["/AdminLoyalty/Rewards"] },
            new AdminNavItem { Title = "Cửa hàng đổi quà", Url = "/AdminLoyalty/Rewards", IconClass = "fa-solid fa-gift", PathPrefix = "/AdminLoyalty/Rewards", Roles = [StaffRoles.Admin], IsSubItem = true },
            new AdminNavItem { Title = "Banner", Url = "/AdminBanner", IconClass = "fa-solid fa-images", PathPrefix = "/AdminBanner", Roles = [StaffRoles.Admin] });

        AddGroup("Hệ thống", "fa-solid fa-gear",
            new AdminNavItem { Title = "Khách hàng", Url = "/AdminUser", IconClass = "fa-solid fa-users", PathPrefix = "/AdminUser", Roles = [StaffRoles.Admin] },
            new AdminNavItem { Title = "Nhân viên", Url = "/AdminStaff", IconClass = "fa-solid fa-user-shield", PathPrefix = "/AdminStaff", Roles = [StaffRoles.Admin] });

        return groups;
    }
}

public class AdminSidebarViewModel
{
    public string UserName { get; set; } = "";
    public string RoleDisplayName { get; set; } = "";
    public List<AdminNavGroup> MenuGroups { get; set; } = new();
}

public class AdminNavGroup
{
    public string Title { get; set; } = "";
    public string IconClass { get; set; } = "";
    public List<AdminNavItem> Items { get; set; } = new();
}

public class AdminNavItem
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string IconClass { get; set; } = "";
    public string PathPrefix { get; set; } = "";
    public string[] Roles { get; set; } = [];
    public bool IsSubItem { get; set; }
    public bool ExactMatch { get; set; }
    public string[] ExcludePaths { get; set; } = [];
}
