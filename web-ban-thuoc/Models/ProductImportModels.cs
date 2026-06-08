namespace web_ban_thuoc.Models;

public class ProductImportRowResult
{
    public int RowNumber { get; set; }
    public string ProductName { get; set; } = "";
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int? ProductId { get; set; }
}

public class ProductImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ProductImportRowResult> Rows { get; set; } = new();
}
