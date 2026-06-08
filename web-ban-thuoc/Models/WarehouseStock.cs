namespace web_ban_thuoc.Models;

/// <summary>
/// Tồn kho theo từng kho và sản phẩm (nguồn tồn chính).
/// </summary>
public class WarehouseStock
{
    public int WarehouseStockId { get; set; }

    public int WarehouseId { get; set; }

    public int ProductId { get; set; }

    public int QuantityOnHand { get; set; }

    public int QuantityReserved { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public virtual Warehouse Warehouse { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public int AvailableQuantity => Math.Max(0, QuantityOnHand - QuantityReserved);
}
