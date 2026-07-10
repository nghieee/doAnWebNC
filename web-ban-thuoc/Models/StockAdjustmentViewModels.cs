namespace web_ban_thuoc.Models;

// ─── ViewModels cho StockAdjustment ────────────────────────────────────────────

public class StockAdjustmentListViewModel
{
    public int StockAdjustmentId { get; set; }
    public string AdjustmentCode { get; set; } = "";
    public string AdjustmentType { get; set; } = "";
    public string? Reason { get; set; }
    public string Status { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string? RequestedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int TotalLines { get; set; }
    public int TotalQuantity { get; set; }
}

public class StockAdjustmentDetailViewModel
{
    public int StockAdjustmentDetailId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? Sku { get; set; }
    public int? ProductBatchId { get; set; }
    public string? BatchNo { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Note { get; set; }
}

public class StockAdjustmentIndexViewModel
{
    public List<StockAdjustmentListViewModel> Adjustments { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
    public int? SelectedWarehouseId { get; set; }
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
    public string? TypeFilter { get; set; }
    public int PendingCount { get; set; }
}

public class StockAdjustmentDetailPageViewModel
{
    public StockAdjustment Adjustment { get; set; } = null!;
    public List<StockAdjustmentDetailViewModel> Details { get; set; } = new();
}

public class CreateStockAdjustmentViewModel
{
    public int WarehouseId { get; set; }
    public string AdjustmentType { get; set; } = StockAdjustmentTypes.Export;
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public string? RequestedBy { get; set; }
    public List<CreateStockAdjustmentLineViewModel> Lines { get; set; } = new();
}

public class CreateStockAdjustmentLineViewModel
{
    public int ProductId { get; set; }
    public int? ProductBatchId { get; set; }
    public int Quantity { get; set; }
}

public class FefoBatchViewModel
{
    public int ProductBatchId { get; set; }
    public string BatchNo { get; set; } = "";
    public DateTime? ExpiryDate { get; set; }
    public int QuantityOnHand { get; set; }
    public decimal? UnitCost { get; set; }
    public int DaysUntilExpiry => ExpiryDate.HasValue
        ? (int)(ExpiryDate.Value - DateTime.Today).TotalDays
        : int.MaxValue;
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Today;
    public bool IsExpiringSoon => !IsExpired && ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.Today.AddDays(30);
}
