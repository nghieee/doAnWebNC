namespace web_ban_thuoc.Models;

/// <summary>
/// Phiếu điều chỉnh tồn kho thủ công (nhập thủ công, xuất thủ công, điều chỉnh tăng/giảm).
/// Khác với InventoryTransaction — phiếu này đại diện cho một chứng từ gốc,
/// còn InventoryTransaction là bản ghi chi tiết theo từng sản phẩm/batch.
/// </summary>
public class StockAdjustment
{
    public int StockAdjustmentId { get; set; }

    /// <summary>Mã phiếu tự động, ví dụ: SA20260707001</summary>
    public string AdjustmentCode { get; set; } = null!;

    /// <summary>Loại điều chỉnh: Export / Import / Positive / Negative</summary>
    public string AdjustmentType { get; set; } = null!;

    /// <summary>Lý do xuất / nhập</summary>
    public string? Reason { get; set; }

    /// <summary>Ghi chú thêm</summary>
    public string? Note { get; set; }

    /// <summary>Người yêu cầu</summary>
    public string? RequestedBy { get; set; }

    /// <summary>Người duyệt (Admin / Quản lý kho)</summary>
    public string? ApprovedBy { get; set; }

    /// <summary>Trạng thái: Pending / Approved / Rejected</summary>
    public string Status { get; set; } = StockAdjustmentStatuses.Pending;

    /// <summary>Kho thực hiện điều chỉnh</summary>
    public int WarehouseId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Thời điểm duyệt / từ chối</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Người tạo phiếu (userId từ Identity)</summary>
    public string? CreatedByUserId { get; set; }

    // Navigation
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual ICollection<StockAdjustmentDetail> Details { get; set; } = new List<StockAdjustmentDetail>();
}

/// <summary>
/// Chi tiết từng dòng sản phẩm trong phiếu điều chỉnh.
/// Mỗi dòng gắn với một ProductBatch cụ thể (theo FEFO).
/// </summary>
public class StockAdjustmentDetail
{
    public int StockAdjustmentDetailId { get; set; }
    public int StockAdjustmentId { get; set; }

    /// <summary>Sản phẩm điều chỉnh</summary>
    public int ProductId { get; set; }

    /// <summary>Lô hàng cụ thể — áp dụng khi xuất theo batch</summary>
    public int? ProductBatchId { get; set; }

    /// <summary>Số lượng thay đổi (luôn dương, sign xác định bởi StockAdjustment.AdjustmentType)</summary>
    public int Quantity { get; set; }

    /// <summary>Giá trị đơn vị tại thời điểm điều chỉnh</summary>
    public decimal? UnitCost { get; set; }

    /// <summary>Ghi chú dòng</summary>
    public string? Note { get; set; }

    // Navigation
    public virtual StockAdjustment StockAdjustment { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ProductBatch? ProductBatch { get; set; }
}

/// <summary>
/// Các loại điều chỉnh tồn kho thủ công
/// </summary>
public static class StockAdjustmentTypes
{
    public const string Export = "Export";
    public const string Import = "Import";
    public const string Positive = "Positive";
    public const string Negative = "Negative";

    public static string DisplayName(string type) => type switch
    {
        Export => "Xuất kho thủ công",
        Import => "Nhập kho thủ công",
        Positive => "Điều chỉnh tăng",
        Negative => "Điều chỉnh giảm",
        _ => type
    };

    public static string Icon(string type) => type switch
    {
        Export => "fa-arrow-up-from-bracket text-danger",
        Import => "fa-arrow-up-from-bracket text-success rotate-180",
        Positive => "fa-plus-circle text-success",
        Negative => "fa-minus-circle text-warning",
        _ => "fa-sliders"
    };
}

/// <summary>
/// Trạng thái phiếu điều chỉnh
/// </summary>
public static class StockAdjustmentStatuses
{
    public const string Pending = "Chờ duyệt";
    public const string Approved = "Đã duyệt";
    public const string Rejected = "Từ chối";
}

/// <summary>
/// Lý do điều chỉnh kho (dropdown trên form)
/// </summary>
public static class StockAdjustmentReasons
{
    public static readonly Dictionary<string, string> All = new()
    {
        { "expired", "Hàng hết hạn cần hủy" },
        { "damaged", "Hàng hỏng, vỡ, không sử dụng được" },
        { "return_to_supplier", "Trả lại nhà cung cấp" },
        { "transfer", "Điều chuyển kho nội bộ" },
        { "sample", "Xuất mẫu kiểm nghiệm" },
        { "counter_sale", "Bán trực tiếp tại quầy (không qua đơn online)" },
        { "inventory_tally", "Điều chỉnh theo kiểm kê thực tế" },
        { "other", "Lý do khác" },
    };
}
