using System.ComponentModel.DataAnnotations;

namespace web_ban_thuoc.Models;

public class InventoryImportViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitCost { get; set; }

    public string? BatchNo { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Note { get; set; }
    public int? WarehouseId { get; set; }
}

public class InventoryAdjustViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Range(0, int.MaxValue)]
    public int NewQuantity { get; set; }

    public string? Note { get; set; }
    public int? WarehouseId { get; set; }
}

public class InventoryTransactionViewModel
{
    public int TransactionId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public int? OrderId { get; set; }
    public string? SupplierName { get; set; }
    public string? BatchNo { get; set; }
    public string? Note { get; set; }
    public DateTime TransactionDate { get; set; }
}

public class WarehouseStockViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int Available => Math.Max(0, QuantityOnHand - QuantityReserved);
}

public class ProductBatchViewModel
{
    public int ProductBatchId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int QuantityOnHand { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
}

public class SupplierViewModel
{
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Mã NCC không được để trống")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên NCC không được để trống")]
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreatePurchaseOrderViewModel
{
    [Required]
    public int SupplierId { get; set; }

    [Required]
    public int WarehouseId { get; set; }

    public DateTime? ExpectedDate { get; set; }
    public string? Note { get; set; }

    public List<PurchaseOrderLineForm> Lines { get; set; } = new() { new PurchaseOrderLineForm() };
}

public class PurchaseOrderLineForm
{
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int QuantityOrdered { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }
}

public class CreateGoodsReceiptViewModel
{
    public int? PurchaseOrderId { get; set; }

    [Required]
    public int SupplierId { get; set; }

    [Required]
    public int WarehouseId { get; set; }

    public string? Note { get; set; }

    public List<GoodsReceiptLineForm> Lines { get; set; } = new() { new GoodsReceiptLineForm() };
}

public class GoodsReceiptLineForm
{
    public int ProductId { get; set; }
    public int? PurchaseOrderLineId { get; set; }

    [Required]
    public string BatchNo { get; set; } = string.Empty;

    public DateTime? ExpiryDate { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }
}

public class LowStockAlertViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Available { get; set; }
}

public class InventoryIndexViewModel
{
    public List<InventoryTransactionViewModel> Transactions { get; set; } = new();
    public List<WarehouseStockViewModel> WarehouseStocks { get; set; } = new();
    public List<ProductBatchViewModel> Batches { get; set; } = new();
    public List<LowStockAlertViewModel> LowStockAlerts { get; set; } = new();
    public int PendingPurchaseOrders { get; set; }
    public int PendingOrderConfirmations { get; set; }
    public string? Search { get; set; }
    public string? Type { get; set; }
}

public class PurchaseOrderListViewModel
{
    public int PurchaseOrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int TotalOrdered { get; set; }
    public int TotalReceived { get; set; }
}
