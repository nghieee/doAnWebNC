namespace web_ban_thuoc.Models;

public static class LoyaltyRewardTypes
{
    public const string VoucherPercent = "VoucherPercent";
    public const string VoucherFixed = "VoucherFixed";
}

/// <summary>
/// Quà trong cửa hàng đổi điểm — đổi thành công sẽ tạo voucher gán cho user.
/// </summary>
public class LoyaltyReward
{
    public int LoyaltyRewardId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int PointsCost { get; set; }

    /// <summary>VoucherPercent | VoucherFixed</summary>
    public string RewardType { get; set; } = LoyaltyRewardTypes.VoucherFixed;

    public decimal? PercentValue { get; set; }

    public decimal? DiscountAmount { get; set; }

    public int ExpiryDays { get; set; } = 30;

    public decimal? MinOrderAmount { get; set; }

    public string? RequiredRank { get; set; }

    /// <summary>null = không giới hạn tồn quà.</summary>
    public int? StockRemaining { get; set; }

    /// <summary>Số lần tối đa mỗi user đổi quà này (null = không giới hạn).</summary>
    public int? MaxPerUser { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
