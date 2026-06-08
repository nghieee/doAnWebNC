namespace web_ban_thuoc.Models;

/// <summary>
/// Lô hàng theo HSD — xuất kho theo FEFO (hết hạn sớm trước).
/// </summary>
public class ProductBatch
{
    public int ProductBatchId { get; set; }

    public int ProductId { get; set; }

    public int WarehouseId { get; set; }

    public string BatchNo { get; set; } = null!;

    public DateTime? ExpiryDate { get; set; }

    public int QuantityOnHand { get; set; }

    public decimal? UnitCost { get; set; }

    public int? SupplierId { get; set; }

    public int? GoodsReceiptLineId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Product Product { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;

    public virtual Supplier? Supplier { get; set; }

    public virtual GoodsReceiptLine? GoodsReceiptLine { get; set; }
}
