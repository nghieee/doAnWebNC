namespace web_ban_thuoc.Models;

/// <summary>
/// Lịch sử sử dụng voucher gắn với đơn hàng — phục vụ báo cáo chiến dịch.
/// </summary>
public class VoucherRedemption
{
    public int VoucherRedemptionId { get; set; }

    public int VoucherId { get; set; }

    public string UserId { get; set; } = null!;

    public int OrderId { get; set; }

    public decimal DiscountAmount { get; set; }

    public DateTime RedeemedAt { get; set; } = DateTime.Now;

    public bool IsReverted { get; set; }

    public virtual Voucher Voucher { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
