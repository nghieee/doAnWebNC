namespace web_ban_thuoc.Models;

public class OrderStatusHistory
{
    public int OrderStatusHistoryId { get; set; }

    public int OrderId { get; set; }

    public string? FromStatus { get; set; }

    public string ToStatus { get; set; } = null!;

    public string? ChangedByUserId { get; set; }

    public string? Note { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.Now;

    public virtual Order Order { get; set; } = null!;
}
