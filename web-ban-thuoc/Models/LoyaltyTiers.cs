namespace web_ban_thuoc.Models;

/// <summary>
/// Cấu hình hạng thành viên (ngưỡng chi tiêu 6 tháng).
/// Tích điểm đồng nhất mọi hạng; hạng chỉ mở ưu đãi / quà đổi riêng.
/// </summary>
public static class LoyaltyTiers
{
    public const string Silver = "Bạc";
    public const string Gold = "Vàng";
    public const string Platinum = "Bạch kim";

    public static readonly string[] All = { Silver, Gold, Platinum };

    public static readonly (string Rank, decimal MinSpend6Months)[] Definitions =
    {
        (Silver, 0),
        (Gold, 5_000_000),
        (Platinum, 10_000_000)
    };

    /// <summary>1 điểm / 1.000đ giá trị đơn (sau giảm giá), mọi hạng như nhau.</summary>
    public const int PointsPerThousandVnd = 1;

    public static string ResolveRank(decimal totalSpent6Months)
    {
        if (totalSpent6Months >= 10_000_000) return Platinum;
        if (totalSpent6Months >= 5_000_000) return Gold;
        return Silver;
    }

    public static int CalculateEarnedPoints(decimal orderAmount)
    {
        if (orderAmount <= 0) return 0;
        return (int)Math.Floor(orderAmount / 1000m) * PointsPerThousandVnd;
    }
}
