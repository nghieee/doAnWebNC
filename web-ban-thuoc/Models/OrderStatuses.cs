namespace web_ban_thuoc.Models;

/// <summary>
/// Trạng thái đơn hàng chuẩn hóa (lưu dạng chuỗi tiếng Việt trong DB để tương thích UI hiện tại).
/// </summary>
public static class OrderStatuses
{
    public const string PendingPayment = "Chờ thanh toán";
    public const string PendingConfirmation = "Chờ xác nhận";
    public const string Confirmed = "Đã xác nhận";
    public const string Packing = "Đang đóng gói";
    public const string Shipped = "Đang giao";
    public const string Delivered = "Đã giao";
    public const string Cancelled = "Đã hủy";

    public static readonly string[] All =
    {
        PendingPayment, PendingConfirmation, Confirmed, Packing, Shipped, Delivered, Cancelled
    };

    public static readonly string[] AdminManageable =
    {
        PendingConfirmation, Confirmed, Packing, Shipped, Delivered
    };

    public static bool IsTerminal(string status) =>
        status == Delivered || status == Cancelled;

    public static bool CanCustomerCancel(string status) =>
        status == PendingConfirmation || status == Confirmed;

    public static bool CanTransition(string from, string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || from == to)
            return false;

        if (to == Cancelled)
            return !IsTerminal(from);

        return (from, to) switch
        {
            (PendingPayment, Confirmed) => true,
            (PendingConfirmation, Confirmed) => true,
            (Confirmed, Packing) => true,
            (Packing, Shipped) => true,
            (Shipped, Delivered) => true,
            _ => false
        };
    }

    public static bool RequiresStockExport(string status) => status == Confirmed;
}
