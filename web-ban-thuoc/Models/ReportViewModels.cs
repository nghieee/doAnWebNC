namespace web_ban_thuoc.Models;

public class SupplierDebtItemViewModel
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal IncurredDebt { get; set; } // Phát sinh tăng (Nhập hàng trong kỳ)
    public decimal PaidAmount { get; set; }  // Phát sinh giảm (Đã thanh toán)
    public decimal ClosingBalance { get; set; } // Nợ cuối kỳ
    public DateTime? NextDueDate { get; set; }
    public string Status { get; set; } = "Trong hạn"; // Trong hạn, Sắp đến hạn, Quá hạn
}

public class SupplierDebtReportViewModel
{
    public string Period { get; set; } = "thisMonth";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalOpeningBalance { get; set; }
    public decimal TotalIncurredDebt { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalClosingBalance { get; set; }
    public List<SupplierDebtItemViewModel> Items { get; set; } = new();
}

public class VoucherStatsItemViewModel
{
    public int VoucherId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public int TotalIssued { get; set; }
    public int UsedCount { get; set; }
    public double RedemptionRate => TotalIssued > 0 ? Math.Round((double)UsedCount / TotalIssued * 100, 1) : 0;
    public decimal TotalDiscountGiven { get; set; }
    public decimal TotalRevenueGenerated { get; set; }
}

public class VoucherStatsReportViewModel
{
    public int TotalVouchers { get; set; }
    public int TotalRedemptions { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal TotalRevenueWithVoucher { get; set; }
    public decimal TotalRevenueWithoutVoucher { get; set; }
    public List<VoucherStatsItemViewModel> VoucherItems { get; set; } = new();
}
