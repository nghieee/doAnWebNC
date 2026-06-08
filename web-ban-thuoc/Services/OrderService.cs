using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services;

public interface IOrderService
{
    Task<(bool success, string message)> ChangeStatusAsync(int orderId, string newStatus, string? changedByUserId, string? note = null);
    Task CancelByCustomerAsync(int orderId, string userId);
}

public class OrderService : IOrderService
{
    private readonly LongChauDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly UserRankService _userRankService;
    private readonly IOrderNotificationService _notificationService;

    public OrderService(
        LongChauDbContext context,
        IInventoryService inventoryService,
        ILoyaltyService loyaltyService,
        UserRankService userRankService,
        IOrderNotificationService notificationService)
    {
        _context = context;
        _inventoryService = inventoryService;
        _loyaltyService = loyaltyService;
        _userRankService = userRankService;
        _notificationService = notificationService;
    }

    public async Task<(bool success, string message)> ChangeStatusAsync(
        int orderId, string newStatus, string? changedByUserId, string? note = null)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return (false, "Không tìm thấy đơn hàng!");

        var fromStatus = order.Status ?? "";
        if (!OrderStatuses.CanTransition(fromStatus, newStatus))
            return (false, $"Không thể chuyển từ '{fromStatus}' sang '{newStatus}'.");

        if (newStatus == OrderStatuses.Confirmed)
        {
            try
            {
                await _inventoryService.ExportStockForOrderAsync(orderId, changedByUserId);
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }

            if (!string.IsNullOrEmpty(order.VoucherCode) && !string.IsNullOrEmpty(order.UserId))
                VoucherHelper.MarkUsed(_context, order.UserId, order.VoucherCode, orderId, order.VoucherDiscount ?? 0);
        }

        if (newStatus == OrderStatuses.Cancelled)
        {
            if (await _inventoryService.HasExportedOrderAsync(orderId))
                await _inventoryService.ReturnStockForOrderAsync(orderId, changedByUserId);

            if (!string.IsNullOrEmpty(order.VoucherCode) && !string.IsNullOrEmpty(order.UserId)
                && (fromStatus == OrderStatuses.Confirmed || fromStatus == OrderStatuses.Packing || fromStatus == OrderStatuses.Shipped))
                VoucherHelper.RevertUsage(_context, order.UserId, order.VoucherCode, orderId);
        }

        if (newStatus == OrderStatuses.Delivered)
        {
            await ApplyCodPaymentOnDeliveredAsync(order);
            await _loyaltyService.EarnPointsForDeliveredOrderAsync(orderId);
            if (!string.IsNullOrEmpty(order.UserId))
                await _userRankService.UpdateUserRankAndSendMailAsync(order.UserId);
        }

        order.Status = newStatus;
        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            FromStatus = fromStatus,
            ToStatus = newStatus,
            ChangedByUserId = changedByUserId,
            Note = note
        });

        await _context.SaveChangesAsync();
        await _notificationService.NotifyStatusChangedAsync(orderId, fromStatus, newStatus);
        return (true, "Đã cập nhật trạng thái đơn hàng!");
    }

    public async Task CancelByCustomerAsync(int orderId, string userId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId)
            ?? throw new InvalidOperationException("Không tìm thấy đơn hàng!");

        if (!OrderStatuses.CanCustomerCancel(order.Status ?? ""))
            throw new InvalidOperationException("Không thể hủy đơn ở trạng thái hiện tại!");

        var result = await ChangeStatusAsync(orderId, OrderStatuses.Cancelled, userId, "Khách hàng hủy đơn");
        if (!result.success)
            throw new InvalidOperationException(result.message);
    }

    private async Task ApplyCodPaymentOnDeliveredAsync(Order order)
    {
        if (order.PaymentStatus == PaymentStatuses.Paid)
            return;

        var payments = order.Payments?.Where(p =>
            (p.PaymentMethod ?? "").Equals("COD", StringComparison.OrdinalIgnoreCase)).ToList()
            ?? await _context.Payments.Where(p => p.OrderId == order.OrderId).ToListAsync();

        foreach (var payment in payments.Where(p =>
                     (p.PaymentMethod ?? "").Equals("COD", StringComparison.OrdinalIgnoreCase)))
        {
            payment.PaymentStatus = PaymentStatuses.Paid;
            payment.PaymentDate ??= DateTime.Now;
        }

        order.PaymentStatus = PaymentStatuses.Paid;
    }
}
