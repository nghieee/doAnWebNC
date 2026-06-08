using System;
using Microsoft.AspNetCore.Identity;

namespace web_ban_thuoc.Models;

/// <summary>
/// Phiếu nhập/xuất/điều chỉnh tồn kho.
/// TransactionType: Import (nhập từ NSX), Sale (xuất khi bán), Return (nhập lại khi hủy/trả), Adjustment (kiểm kê).
/// </summary>
public partial class InventoryTransaction
{
    public int TransactionId { get; set; }

    public int ProductId { get; set; }

    public int WarehouseId { get; set; }

    /// <summary>
    /// Import | Sale | Return | Adjustment
    /// </summary>
    public string TransactionType { get; set; } = null!;

    /// <summary>
    /// Số lượng luôn dương; loại phiếu quyết định tăng/giảm tồn.
    /// </summary>
    public int Quantity { get; set; }

    public int QuantityBefore { get; set; }

    public int QuantityAfter { get; set; }

    public int? OrderId { get; set; }

    public int? SupplierId { get; set; }

    public int? ProductBatchId { get; set; }

    public int? GoodsReceiptId { get; set; }

    public string? SupplierName { get; set; }

    public decimal? UnitCost { get; set; }

    public string? Note { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.Now;

    public virtual Product Product { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;

    public virtual Order? Order { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual ProductBatch? ProductBatch { get; set; }

    public virtual GoodsReceipt? GoodsReceipt { get; set; }

    public virtual IdentityUser? CreatedByUser { get; set; }
}
