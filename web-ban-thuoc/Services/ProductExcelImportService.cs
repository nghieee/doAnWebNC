using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services;

public interface IProductExcelImportService
{
    byte[] BuildTemplate();
    Task<ProductImportResult> ImportAsync(Stream excelStream, string? userId, CancellationToken ct = default);
}

public class ProductExcelImportService : IProductExcelImportService
{
    private readonly LongChauDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly IWebHostEnvironment _env;

    private static readonly Dictionary<string, string[]> ColumnAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TenSanPham"] = ["TenSanPham", "Tên sản phẩm", "ProductName", "Ten SP"],
        ["SKU"] = ["SKU", "MaSKU", "Mã SKU"],
        ["MaVach"] = ["MaVach", "Mã vạch", "Barcode"],
        ["SoDangKy"] = ["SoDangKy", "Số đăng ký", "RegistrationNumber"],
        ["ThuongHieu"] = ["ThuongHieu", "Thương hiệu", "Brand"],
        ["GiaBan"] = ["GiaBan", "Giá bán", "Price"],
        ["GiaNhap"] = ["GiaNhap", "Giá nhập", "CostPrice"],
        ["QuyCach"] = ["QuyCach", "Quy cách", "Package"],
        ["TenDanhMuc"] = ["TenDanhMuc", "Tên danh mục", "CategoryName", "DanhMuc"],
        ["TenNCC"] = ["TenNCC", "Tên NCC", "NhaCungCap", "SupplierName"],
        ["MaNCC"] = ["MaNCC", "Mã NCC", "SupplierCode"],
        ["ThanhPhan"] = ["ThanhPhan", "Thành phần", "Ingredients"],
        ["CongDung"] = ["CongDung", "Công dụng", "Uses"],
        ["LieuDung"] = ["LieuDung", "Liều dùng", "Dosage"],
        ["DoiTuong"] = ["DoiTuong", "Đối tượng", "TargetUsers"],
        ["ChongChiDinh"] = ["ChongChiDinh", "Chống chỉ định", "Contraindications"],
        ["XuatXu"] = ["XuatXu", "Xuất xứ", "Origin"],
        ["DonViTP"] = ["DonViTP", "Đơn vị thành phần", "IngredientUnit"],
        ["CanDonThuoc"] = ["CanDonThuoc", "Cần đơn thuốc", "RequiresPrescription"],
        ["NoiBat"] = ["NoiBat", "Nổi bật", "IsFeature"],
        ["SoLuongTon"] = ["SoLuongTon", "Số lượng tồn", "StockQuantity", "TonKho"],
        ["TenAnh"] = ["TenAnh", "Tên ảnh", "ImageFile", "Anh"]
    };

    public ProductExcelImportService(LongChauDbContext context, IInventoryService inventoryService, IWebHostEnvironment env)
    {
        _context = context;
        _inventoryService = inventoryService;
        _env = env;
    }

    public byte[] BuildTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("SanPham");

        var headers = new[]
        {
            "TenSanPham", "SKU", "MaVach", "SoDangKy", "ThuongHieu", "GiaBan", "GiaNhap", "QuyCach",
            "TenDanhMuc", "TenNCC", "MaNCC", "ThanhPhan", "CongDung", "LieuDung", "DoiTuong", "ChongChiDinh", "XuatXu",
            "DonViTP", "CanDonThuoc", "NoiBat", "SoLuongTon", "TenAnh"
        };

        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976d2");
        headerRow.Style.Font.FontColor = XLColor.White;

        ws.Cell(2, 1).Value = "Paracetamol 500mg";
        ws.Cell(2, 2).Value = "SKU-PARA500";
        ws.Cell(2, 5).Value = "Pharmedic";
        ws.Cell(2, 6).Value = 25000;
        ws.Cell(2, 7).Value = 18000;
        ws.Cell(2, 8).Value = "Hộp 10 vỉ x 10 viên";
        ws.Cell(2, 9).Value = "Thuốc giảm đau, hạ sốt";
        ws.Cell(2, 10).Value = "Công ty Dược ABC";
        ws.Cell(2, 11).Value = "NCC001";
        ws.Cell(2, 17).Value = "Việt Nam";
        ws.Cell(2, 19).Value = "Khong";
        ws.Cell(2, 20).Value = "Co";
        ws.Cell(2, 21).Value = 100;
        ws.Cell(2, 22).Value = "default.png";

        var guide = wb.Worksheets.Add("HuongDan");
        guide.Cell(1, 1).Value = "Hướng dẫn nhập Excel";
        guide.Cell(1, 1).Style.Font.Bold = true;
        guide.Cell(3, 1).Value = "• TenSanPham và GiaBan là bắt buộc.";
        guide.Cell(4, 1).Value = "• TenDanhMuc: nhập đúng tên danh mục cấp 3 đã có trong hệ thống.";
        guide.Cell(5, 1).Value = "• TenNCC hoặc MaNCC: gán nhà cung cấp (ưu tiên MaNCC nếu có).";
        guide.Cell(6, 1).Value = "• CanDonThuoc / NoiBat: Co/Khong hoặc 1/0.";
        guide.Cell(7, 1).Value = "• SoLuongTon > 0: tự tạo lô nhập kho ban đầu.";
        guide.Cell(8, 1).Value = "• TenAnh: tên file trong wwwroot/images/products (vd: default.png).";
        guide.Cell(9, 1).Value = "• SKU trùng sẽ bỏ qua dòng đó.";

        ws.Columns().AdjustToContents();
        guide.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<ProductImportResult> ImportAsync(Stream excelStream, string? userId, CancellationToken ct = default)
    {
        var result = new ProductImportResult();
        var categories = await _context.Categories.AsNoTracking().ToListAsync(ct);
        var suppliers = await _context.Suppliers.AsNoTracking().ToListAsync(ct);
        var existingSkus = await _context.Products
            .Where(p => p.Sku != null && p.Sku != "")
            .Select(p => p.Sku!)
            .ToListAsync(ct);
        var skuSet = new HashSet<string>(existingSkus, StringComparer.OrdinalIgnoreCase);

        using var wb = new XLWorkbook(excelStream);
        var ws = wb.Worksheet(1);
        var usedRange = ws.RangeUsed();
        if (usedRange == null)
            return result;

        var colMap = BuildColumnMap(ws.Row(1));
        if (!colMap.ContainsKey("TenSanPham") || !colMap.ContainsKey("GiaBan"))
            throw new InvalidOperationException("File thiếu cột TenSanPham hoặc GiaBan. Hãy tải file mẫu.");

        var lastRow = usedRange.LastRow().RowNumber();
        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            ct.ThrowIfCancellationRequested();
            var row = ws.Row(rowNum);
            var name = GetCell(row, colMap, "TenSanPham");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            result.TotalRows++;
            var rowResult = new ProductImportRowResult { RowNumber = rowNum, ProductName = name.Trim() };

            try
            {
                if (!TryParseDecimal(GetCell(row, colMap, "GiaBan"), out var price) || price <= 0)
                    throw new InvalidOperationException("Giá bán không hợp lệ.");

                var sku = NullIfEmpty(GetCell(row, colMap, "SKU"));
                if (!string.IsNullOrEmpty(sku) && skuSet.Contains(sku))
                {
                    rowResult.Success = false;
                    rowResult.Message = $"SKU '{sku}' đã tồn tại — bỏ qua.";
                    result.SkippedCount++;
                    result.Rows.Add(rowResult);
                    continue;
                }

                var categoryName = NullIfEmpty(GetCell(row, colMap, "TenDanhMuc"));
                int? categoryId = null;
                if (!string.IsNullOrEmpty(categoryName))
                {
                    var cat = categories.FirstOrDefault(c =>
                        c.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    categoryId = cat?.CategoryId;
                }

                int? supplierId = null;
                var supplierCode = NullIfEmpty(GetCell(row, colMap, "MaNCC"));
                var supplierName = NullIfEmpty(GetCell(row, colMap, "TenNCC"));
                if (!string.IsNullOrEmpty(supplierCode))
                {
                    supplierId = suppliers.FirstOrDefault(s =>
                        s.Code.Equals(supplierCode, StringComparison.OrdinalIgnoreCase))?.SupplierId;
                }
                if (!supplierId.HasValue && !string.IsNullOrEmpty(supplierName))
                {
                    supplierId = suppliers.FirstOrDefault(s =>
                        s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase))?.SupplierId;
                }

                TryParseDecimal(GetCell(row, colMap, "GiaNhap"), out var costPrice);
                TryParseInt(GetCell(row, colMap, "SoLuongTon"), out var stockQty);

                var product = new Product
                {
                    ProductName = name.Trim(),
                    Sku = sku ?? $"SKU-{DateTime.Now:yyyyMMddHHmmss}-{rowNum}",
                    Barcode = NullIfEmpty(GetCell(row, colMap, "MaVach")),
                    RegistrationNumber = NullIfEmpty(GetCell(row, colMap, "SoDangKy")),
                    Brand = NullIfEmpty(GetCell(row, colMap, "ThuongHieu")),
                    Price = price,
                    CostPrice = costPrice > 0 ? costPrice : null,
                    Package = NullIfEmpty(GetCell(row, colMap, "QuyCach")),
                    CategoryId = categoryId,
                    SupplierId = supplierId,
                    Ingredients = NullIfEmpty(GetCell(row, colMap, "ThanhPhan")),
                    Uses = NullIfEmpty(GetCell(row, colMap, "CongDung")),
                    Dosage = NullIfEmpty(GetCell(row, colMap, "LieuDung")),
                    TargetUsers = NullIfEmpty(GetCell(row, colMap, "DoiTuong")),
                    Contraindications = NullIfEmpty(GetCell(row, colMap, "ChongChiDinh")),
                    Origin = NullIfEmpty(GetCell(row, colMap, "XuatXu")),
                    IngredientUnit = NullIfEmpty(GetCell(row, colMap, "DonViTP")),
                    RequiresPrescription = ParseBool(GetCell(row, colMap, "CanDonThuoc")),
                    IsFeature = ParseBool(GetCell(row, colMap, "NoiBat")),
                    IsActive = true,
                    StockQuantity = 0,
                    SoldQuantity = 0,
                    Slug = ToSlug(name)
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync(ct);

                if (!string.IsNullOrEmpty(product.Sku))
                    skuSet.Add(product.Sku);

                var imageFile = NullIfEmpty(GetCell(row, colMap, "TenAnh")) ?? "default.png";
                var imagePath = Path.Combine(_env.WebRootPath, "images", "products", imageFile);
                if (!System.IO.File.Exists(imagePath))
                    imageFile = "default.png";

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.ProductId,
                    ImageUrl = imageFile,
                    IsMain = true,
                    SortOrder = 1
                });
                await _context.SaveChangesAsync(ct);

                if (stockQty > 0)
                {
                    await _inventoryService.ImportStockAsync(
                        product.ProductId, stockQty, "Nhập từ Excel", product.CostPrice,
                        $"Import Excel dòng {rowNum}", userId,
                        batchNo: $"LOT-IMP-{product.ProductId}");
                }

                rowResult.Success = true;
                rowResult.ProductId = product.ProductId;
                rowResult.Message = stockQty > 0
                    ? $"Thêm OK + nhập kho {stockQty} SP."
                    : "Thêm OK.";
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                rowResult.Success = false;
                rowResult.Message = ex.Message;
                result.ErrorCount++;
            }

            result.Rows.Add(rowResult);
        }

        return result;
    }

    private static Dictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (string.IsNullOrEmpty(header)) continue;

            foreach (var (key, aliases) in ColumnAliases)
            {
                if (aliases.Any(a => a.Equals(header, StringComparison.OrdinalIgnoreCase)))
                {
                    map[key] = cell.Address.ColumnNumber;
                    break;
                }
            }
        }
        return map;
    }

    private static string GetCell(IXLRow row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return "";
        var cell = row.Cell(col);
        return cell.DataType switch
        {
            XLDataType.Number => cell.GetDouble().ToString(CultureInfo.InvariantCulture),
            XLDataType.Boolean => cell.GetBoolean() ? "Co" : "Khong",
            _ => cell.GetString().Trim()
        };
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim().ToLowerInvariant();
        return v is "1" or "co" or "có" or "yes" or "true" or "x";
    }

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalized = value.Replace(",", "").Replace("đ", "").Replace("VND", "").Trim();
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(normalized, NumberStyles.Any, new CultureInfo("vi-VN"), out result);
    }

    private static bool TryParseInt(string? value, out int result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalized = value.Replace(",", "").Trim();
        return int.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    private static string ToSlug(string name)
    {
        var normalized = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        var slug = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..12] : slug;
    }
}
