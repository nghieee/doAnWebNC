namespace web_ban_thuoc.Models;

public static class ShippingCarriers
{
    public const string Ghn = "GHN";
    public const string Ghtk = "GHTK";
    public const string ViettelPost = "ViettelPost";
    public const string Other = "Other";

    public static readonly (string Code, string Name)[] All =
    {
        (Ghn, "Giao Hàng Nhanh"),
        (Ghtk, "Giao Hàng Tiết Kiệm"),
        (ViettelPost, "Viettel Post"),
        (Other, "Khác / Tự giao")
    };

    public static string? GetTrackingUrl(string carrier, string trackingCode)
    {
        if (string.IsNullOrWhiteSpace(trackingCode)) return null;
        return carrier switch
        {
            Ghn => $"https://donhang.ghn.vn/?order_code={Uri.EscapeDataString(trackingCode)}",
            Ghtk => $"https://i.ghtk.vn/{Uri.EscapeDataString(trackingCode)}",
            ViettelPost => $"https://viettelpost.com.vn/tracing?code={Uri.EscapeDataString(trackingCode)}",
            _ => null
        };
    }
}

public class Shipment
{
    public int ShipmentId { get; set; }

    public int OrderId { get; set; }

    public string Carrier { get; set; } = ShippingCarriers.Other;

    public string TrackingCode { get; set; } = null!;

    public decimal? ShippingFee { get; set; }

    public DateTime ShippedAt { get; set; } = DateTime.Now;

    public DateTime? EstimatedDelivery { get; set; }

    public string? Note { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Order Order { get; set; } = null!;
}
