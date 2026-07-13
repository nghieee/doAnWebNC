namespace web_ban_thuoc.Models;

public class GhnEstimateFeeRequest
{
    public int? FromDistrictId { get; set; }
    public string? FromWardCode { get; set; }
    public int ToDistrictId { get; set; }
    public string ToWardCode { get; set; } = string.Empty;
    public int Weight { get; set; }
    public int Length { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int InsuranceValue { get; set; }
    public int? ServiceId { get; set; }
    public int? ServiceTypeId { get; set; }
    public string? Coupon { get; set; }
    public List<GhnCreateOrderItem>? Items { get; set; }
}

public class GhnEstimateFeeResponse
{
    public int Total { get; set; }
    public int ServiceFee { get; set; }
    public int InsuranceFee { get; set; }
    public int CouponValue { get; set; }
}

public class GhnAddressItem
{
    public int ProvinceID { get; set; }
    public string ProvinceName { get; set; } = string.Empty;

    public int DistrictID { get; set; }
    public string DistrictName { get; set; } = string.Empty;

    public string WardCode { get; set; } = string.Empty;
    public string WardName { get; set; } = string.Empty;
}

public class GhnServiceOption
{
    public int ServiceId { get; set; }
    public string ShortName { get; set; } = string.Empty;
    public int ServiceTypeId { get; set; }
}

public class GhnAddressLookupResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<GhnAddressItem> Provinces { get; set; } = new();
    public List<GhnAddressItem> Districts { get; set; } = new();
    public List<GhnAddressItem> Wards { get; set; } = new();
}

public class GhnCreateOrderItem
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Quantity { get; set; }
    public int Price { get; set; }
    public int Length { get; set; }
    public int Width { get; set; }
    public int Weight { get; set; }
    public int Height { get; set; }
}

public class GhnCreateOrderRequest
{
    public int PaymentTypeId { get; set; } = 2;
    public string Note { get; set; } = "Xin gọi trước khi giao hàng";
    public string RequiredNote { get; set; } = "KHONGCHOXEMHANG";
    public string ClientOrderCode { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string ToPhone { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string ToWardCode { get; set; } = string.Empty;
    public int ToDistrictId { get; set; }
    public int CodAmount { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Weight { get; set; }
    public int Length { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int InsuranceValue { get; set; }
    public int? ServiceId { get; set; }
    public int ServiceTypeId { get; set; } = 2;
    public List<GhnCreateOrderItem> Items { get; set; } = new();
}

public class GhnCreateOrderResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? OrderCode { get; set; }
}

public class GhnPrintTokenRequest
{
    public List<string> OrderCodes { get; set; } = new();
}

public class GhnPrintTokenResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public string? A5Url { get; set; }
    public string? Print80x80Url { get; set; }
}
