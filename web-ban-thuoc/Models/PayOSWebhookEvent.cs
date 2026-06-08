namespace web_ban_thuoc.Models;

/// <summary>
/// Ghi nhận webhook PayOS đã xử lý — tránh xử lý trùng (idempotency).
/// </summary>
public class PayOSWebhookEvent
{
    public int PayOSWebhookEventId { get; set; }

    /// <summary>Mã tham chiếu duy nhất từ PayOS (reference hoặc orderCode+status).</summary>
    public string IdempotencyKey { get; set; } = null!;

    public int? OrderId { get; set; }

    public string? OrderCode { get; set; }

    public bool PaymentSuccess { get; set; }

    public string? RawPayload { get; set; }

    public DateTime ProcessedAt { get; set; } = DateTime.Now;
}
