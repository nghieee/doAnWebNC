using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services;

public interface IOrderNotificationService
{
    Task NotifyStatusChangedAsync(int orderId, string fromStatus, string toStatus);
}

public class OrderNotificationService : IOrderNotificationService
{
    private readonly LongChauDbContext _context;
    private readonly IOrderEmailService _emailService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<OrderNotificationService> _logger;

    public OrderNotificationService(
        LongChauDbContext context,
        IOrderEmailService emailService,
        UserManager<IdentityUser> userManager,
        ILogger<OrderNotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task NotifyStatusChangedAsync(int orderId, string fromStatus, string toStatus)
    {
        if (fromStatus == toStatus)
            return;

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null || string.IsNullOrEmpty(order.UserId))
            return;

        var user = await _userManager.FindByIdAsync(order.UserId);
        var email = user?.Email;
        if (string.IsNullOrEmpty(email))
            return;

        try
        {
            await _emailService.SendOrderStatusUpdateEmailAsync(new OrderStatusEmail
            {
                OrderId = order.OrderId,
                CustomerName = order.FullName ?? user.UserName ?? "Khách hàng",
                CustomerEmail = email,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                TotalAmount = order.TotalAmount ?? 0,
                TrackingCode = order.Shipment?.TrackingCode,
                Carrier = order.Shipment?.Carrier,
                TrackingUrl = order.Shipment != null
                    ? ShippingCarriers.GetTrackingUrl(order.Shipment.Carrier, order.Shipment.TrackingCode)
                    : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status email for order {OrderId}", orderId);
        }
    }
}

public class OrderStatusEmail
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? TrackingCode { get; set; }
    public string? Carrier { get; set; }
    public string? TrackingUrl { get; set; }
}
