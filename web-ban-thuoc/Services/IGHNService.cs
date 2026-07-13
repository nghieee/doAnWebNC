using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services;

public interface IGHNService
{
    Task<GhnAddressLookupResult> GetProvincesAsync(CancellationToken ct = default);
    Task<GhnAddressLookupResult> GetDistrictsAsync(int provinceId, CancellationToken ct = default);
    Task<GhnAddressLookupResult> GetWardsAsync(int districtId, CancellationToken ct = default);
    Task<GhnAddressLookupResult> SafeGetAddressLookupAsync(string? type, int? id, CancellationToken ct = default);
    Task<GhnAddressLookupResult> GetAddressByKeywordAsync(string keyword, CancellationToken ct = default);
    Task<List<GhnServiceOption>> GetServicesAsync(int toDistrictId, string toWardCode, CancellationToken ct = default);

    Task<GhnCreateOrderResponse> CreateOrderAsync(GhnCreateOrderRequest request, CancellationToken ct = default);
    Task<GhnPrintTokenResponse> CreatePrintTokenAsync(string orderCode, CancellationToken ct = default);
    Task<GhnEstimateFeeResponse?> EstimateShippingFeeAsync(GhnEstimateFeeRequest request, CancellationToken ct = default);
}
