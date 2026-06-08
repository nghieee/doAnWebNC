namespace web_ban_thuoc.Models;

public class GoodsReceipt
{
    public int GoodsReceiptId { get; set; }

    public string ReceiptCode { get; set; } = null!;

    public int? PurchaseOrderId { get; set; }

    public int SupplierId { get; set; }

    public int WarehouseId { get; set; }

    public DateTime ReceiptDate { get; set; } = DateTime.Now;

    public string? Note { get; set; }

    public string? CreatedByUserId { get; set; }

    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;

    public virtual ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
}

public class GoodsReceiptLine
{
    public int GoodsReceiptLineId { get; set; }

    public int GoodsReceiptId { get; set; }

    public int ProductId { get; set; }

    public int? PurchaseOrderLineId { get; set; }

    public string BatchNo { get; set; } = null!;

    public DateTime? ExpiryDate { get; set; }

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public virtual GoodsReceipt GoodsReceipt { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual PurchaseOrderLine? PurchaseOrderLine { get; set; }

    public virtual ProductBatch? ProductBatch { get; set; }
}
