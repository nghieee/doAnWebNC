namespace web_ban_thuoc.Models;

public static class PurchaseOrderStatuses
{
    public const string Draft = "Nháp";
    public const string Confirmed = "Đã xác nhận";
    public const string PartiallyReceived = "Nhận một phần";
    public const string Received = "Đã nhận đủ";
    public const string Cancelled = "Đã hủy";
}

public class PurchaseOrder
{
    public int PurchaseOrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public int SupplierId { get; set; }

    public int WarehouseId { get; set; }

    public string Status { get; set; } = PurchaseOrderStatuses.Draft;

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public DateTime? ExpectedDate { get; set; }

    public string? Note { get; set; }

    public string? CreatedByUserId { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;

    public virtual ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
}

public class PurchaseOrderLine
{
    public int PurchaseOrderLineId { get; set; }

    public int PurchaseOrderId { get; set; }

    public int ProductId { get; set; }

    public int QuantityOrdered { get; set; }

    public int QuantityReceived { get; set; }

    public decimal UnitCost { get; set; }

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public int RemainingQuantity => Math.Max(0, QuantityOrdered - QuantityReceived);
}
