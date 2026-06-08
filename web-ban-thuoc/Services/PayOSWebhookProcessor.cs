using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services;

public interface IPayOSWebhookProcessor
{
    Task<(bool processed, bool duplicate, string? error)> ProcessAsync(string rawBody, PayOSWebhook webhook);
}

public class PayOSWebhookProcessor : IPayOSWebhookProcessor
{
    private readonly LongChauDbContext _context;
    private readonly IOrderService _orderService;
    private readonly IOrderEmailService _orderEmailService;
    private readonly ILogger<PayOSWebhookProcessor> _logger;

    public PayOSWebhookProcessor(
        LongChauDbContext context,
        IOrderService orderService,
        IOrderEmailService orderEmailService,
        ILogger<PayOSWebhookProcessor> logger)
    {
        _context = context;
        _orderService = orderService;
        _orderEmailService = orderEmailService;
        _logger = logger;
    }

    public async Task<(bool processed, bool duplicate, string? error)> ProcessAsync(string rawBody, PayOSWebhook webhook)
    {
        if (webhook.Data == null)
            return (false, false, "Missing webhook data");

        var idempotencyKey = !string.IsNullOrEmpty(webhook.Data.Reference)
            ? webhook.Data.Reference
            : $"{webhook.Data.OrderCode}_{webhook.Data.Code}_{webhook.Data.TransactionDateTime}";

        if (await _context.PayOSWebhookEvents.AnyAsync(e => e.IdempotencyKey == idempotencyKey))
            return (true, true, null);

        int orderId;
        try
        {
            orderId = int.Parse(webhook.Data.OrderCode.ToString().Split('_')[1]);
        }
        catch
        {
            return (false, false, "Invalid order code format");
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

        if (order == null || payment == null)
            return (false, false, "Order or payment not found");

        var success = webhook.Success && webhook.Data.Code == "00";

        _context.PayOSWebhookEvents.Add(new PayOSWebhookEvent
        {
            IdempotencyKey = idempotencyKey,
            OrderId = orderId,
            OrderCode = webhook.Data.OrderCode.ToString(),
            PaymentSuccess = success,
            RawPayload = rawBody.Length > 4000 ? rawBody[..4000] : rawBody
        });

        if (success)
        {
            if (payment.PaymentStatus != PaymentStatuses.Paid)
            {
                payment.PaymentStatus = PaymentStatuses.Paid;
                payment.PaymentDate = DateTime.Now;
                order.PaymentStatus = PaymentStatuses.Paid;

                if (order.Status == OrderStatuses.PendingPayment)
                    await _orderService.ChangeStatusAsync(orderId, OrderStatuses.Confirmed, order.UserId, "Thanh toán PayOS (webhook)");

                var userEmail = order.UserId;
                if (!string.IsNullOrEmpty(order.UserId))
                {
                    var user = await _context.Users.FindAsync(order.UserId);
                    userEmail = user?.Email ?? order.UserId;
                }

                await _orderEmailService.SendPaymentSuccessEmailAsync(new OrderConfirmationEmail
                {
                    OrderCode = webhook.Data.OrderCode.ToString(),
                    CustomerName = order.FullName ?? "",
                    CustomerEmail = userEmail ?? "",
                    CustomerPhone = order.Phone ?? "",
                    ShippingAddress = order.ShippingAddress ?? "",
                    TotalAmount = order.TotalAmount ?? 0,
                    VoucherDiscount = order.VoucherDiscount ?? 0,
                    VoucherCode = order.VoucherCode ?? "",
                    OrderDate = order.OrderDate ?? DateTime.Now,
                    PaymentMethod = "PayOS",
                    OrderItems = order.OrderItems.ToList()
                });
            }
        }
        else if (payment.PaymentStatus != PaymentStatuses.Paid)
        {
            payment.PaymentStatus = PaymentStatuses.Failed;
            order.PaymentStatus = PaymentStatuses.Failed;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("PayOS webhook processed for order {OrderId}, success={Success}", orderId, success);
        return (true, false, null);
    }
}
