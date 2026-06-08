namespace web_ban_thuoc.Models;

public static class StaffRoles
{
    public const string Admin = "Admin";
    public const string WarehouseStaff = "WarehouseStaff";
    public const string CustomerSupport = "CustomerSupport";

    public static readonly string[] All = { Admin, WarehouseStaff, CustomerSupport };

    public static string GetDisplayName(string role) => role switch
    {
        Admin => "Quản trị viên",
        WarehouseStaff => "Nhân viên kho",
        CustomerSupport => "Chăm sóc khách hàng",
        _ => role
    };

    public static string GetLandingUrl(string role) => role switch
    {
        Admin => "/admin",
        WarehouseStaff => "/AdminInventory?hub=1",
        CustomerSupport => "/admin/chat",
        _ => "/"
    };

    public static bool IsStaffRole(string? role) =>
        !string.IsNullOrEmpty(role) && All.Contains(role);
}
