using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services;

public class GHNService : IGHNService
{
    private readonly ILogger<GHNService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly string _shopId;
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
        },
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.None
    };

    public GHNService(
        ILogger<GHNService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;

        _token = _configuration["GHN:Token"] ?? throw new InvalidOperationException("Missing GHN:Token");
        _shopId = _configuration["GHN:ShopId"] ?? throw new InvalidOperationException("Missing GHN:ShopId");

        _httpClient.BaseAddress = new Uri(_configuration["GHN:BaseUrl"] ?? "https://dev-online-gateway.ghn.vn");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("Token", _token);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LongChauApp/1.0");
    }

    public async Task<GhnAddressLookupResult> GetProvincesAsync(CancellationToken ct = default)
    {
        var items = await CallGetAsync<List<GhnAddressItem>>("/shiip/public-api/master-data/province", ct);
        return new GhnAddressLookupResult { Success = true, Provinces = items };
    }

    public async Task<GhnAddressLookupResult> GetDistrictsAsync(int provinceId, CancellationToken ct = default)
    {
        var items = await CallGetAsync<List<GhnAddressItem>>($"/shiip/public-api/master-data/district?province_id={provinceId}", ct);
        return new GhnAddressLookupResult { Success = true, Districts = items };
    }

    public async Task<GhnAddressLookupResult> GetWardsAsync(int districtId, CancellationToken ct = default)
    {
        var items = await CallGetAsync<List<GhnAddressItem>>($"/shiip/public-api/master-data/ward?district_id={districtId}", ct);
        return new GhnAddressLookupResult { Success = true, Wards = items };
    }

    public async Task<GhnAddressLookupResult> SafeGetAddressLookupAsync(string? type, int? id, CancellationToken ct = default)
    {
        try
        {
            return type?.ToLower() switch
            {
                "province" => await GetProvincesAsync(ct),
                "district" when id.HasValue => await GetDistrictsAsync(id.Value, ct),
                "ward" when id.HasValue => await GetWardsAsync(id.Value, ct),
                _ => new GhnAddressLookupResult { Success = false, Message = "Loại địa chỉ không hợp lệ." }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN address lookup failed type={Type}", type);
            return new GhnAddressLookupResult { Success = false, Message = "Không lấy được dữ liệu địa chỉ." };
        }
    }

    public async Task<GhnAddressLookupResult> GetAddressByKeywordAsync(string keyword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return new GhnAddressLookupResult { Success = false, Message = "Thiếu từ khóa tìm kiếm." };
        }

        try
        {
            var allProvinces = await GetProvincesAsync(ct);
            if (allProvinces.Success)
            {
                var matchProvinces = allProvinces.Provinces
                    .Where(p => !string.IsNullOrWhiteSpace(p.ProvinceName)
                        && p.ProvinceName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchProvinces.Any())
                {
                    return new GhnAddressLookupResult
                    {
                        Success = true,
                        Provinces = matchProvinces.Select(p => new GhnAddressItem
                        {
                            ProvinceID = p.ProvinceID,
                            ProvinceName = p.ProvinceName
                        }).ToList()
                    };
                }
            }

            var allDistricts = new List<GhnAddressItem>();
            if (allProvinces.Success && allProvinces.Provinces.Any())
            {
                var districtTasks = allProvinces.Provinces
                    .Select(p => GetDistrictsAsync(p.ProvinceID, ct))
                    .ToList();

                var districtResults = await Task.WhenAll(districtTasks);
                foreach (var result in districtResults)
                {
                    if (result.Success)
                        allDistricts.AddRange(result.Districts);
                }
            }

            var matchDistricts = allDistricts
                .Where(d => !string.IsNullOrWhiteSpace(d.DistrictName)
                    && d.DistrictName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .GroupBy(d => d.DistrictID)
                .Select(g => g.First())
                .ToList();

            if (matchDistricts.Any())
            {
                return new GhnAddressLookupResult
                {
                    Success = true,
                    Districts = matchDistricts.Select(d => new GhnAddressItem
                    {
                        ProvinceID = d.ProvinceID,
                        ProvinceName = d.ProvinceName,
                        DistrictID = d.DistrictID,
                        DistrictName = d.DistrictName
                    }).ToList()
                };
            }

            var allWards = new List<GhnAddressItem>();
            foreach (var district in allDistricts.DistinctBy(d => d.DistrictID))
            {
                var wardResult = await GetWardsAsync(district.DistrictID, ct);
                if (wardResult.Success)
                    allWards.AddRange(wardResult.Wards);
            }

            var matchWards = allWards
                .Where(w => !string.IsNullOrWhiteSpace(w.WardName)
                    && w.WardName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .GroupBy(w => w.WardCode)
                .Select(g => g.First())
                .ToList();

            return new GhnAddressLookupResult
            {
                Success = true,
                Wards = matchWards.Select(w => new GhnAddressItem
                {
                    ProvinceID = w.ProvinceID,
                    ProvinceName = w.ProvinceName,
                    DistrictID = w.DistrictID,
                    DistrictName = w.DistrictName,
                    WardCode = w.WardCode,
                    WardName = w.WardName
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN address keyword search failed keyword={Keyword}", keyword);
            return new GhnAddressLookupResult { Success = false, Message = "Không tìm kiếm được địa chỉ." };
        }
    }

    public async Task<List<GhnServiceOption>> GetServicesAsync(int toDistrictId, string toWardCode, CancellationToken ct = default)
    {
        try
        {
            var fromDistrictId = _configuration.GetValue<int?>("GHN:FromDistrictId") ?? 0;
            var fromWardCode = _configuration.GetValue<string?>("GHN:FromWardCode");
            var shopIdParsed = int.TryParse(_shopId, out var parsedShopId) ? parsedShopId : 0;

            var payload = new Dictionary<string, object?>
            {
                ["shop_id"] = shopIdParsed,
                ["from_district"] = fromDistrictId,
                ["to_district"] = toDistrictId,
                ["from_ward_code"] = fromWardCode ?? string.Empty,
                ["to_ward_code"] = toWardCode
            };

            if (!string.IsNullOrWhiteSpace(fromWardCode))
                payload["FromWardCode"] = fromWardCode;

            var json = JsonConvert.SerializeObject(payload, JsonSettings);
            _logger.LogInformation("GHN available-services payload (len={Len}): {Payload} | Token.Length={TokenLen} | ShopIdHeader={ShopId}", json.Length, json, _token?.Length ?? 0, _shopId);
            using var content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
            content.Headers.Add("ShopId", _shopId);
            content.Headers.Add("Token", _token);

            using var response = await _httpClient.PostAsync("/shiip/public-api/v2/shipping-order/available-services", content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("GHN available services response {StatusCode}: {Body}", response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                return new List<GhnServiceOption>();
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return new List<GhnServiceOption>();
            }

            var items = new List<GhnServiceOption>();
            foreach (var item in data.EnumerateArray())
            {
                if (!item.TryGetProperty("service_id", out var serviceIdElement))
                    item.TryGetProperty("ServiceID", out serviceIdElement);
                if (!item.TryGetProperty("short_name", out var shortNameElement))
                    item.TryGetProperty("ShortName", out shortNameElement);
                if (!item.TryGetProperty("service_type_id", out var serviceTypeIdElement))
                    item.TryGetProperty("ServiceTypeID", out serviceTypeIdElement);

                if (serviceIdElement.ValueKind != JsonValueKind.Undefined
                    && shortNameElement.ValueKind != JsonValueKind.Undefined
                    && serviceTypeIdElement.ValueKind != JsonValueKind.Undefined)
                {
                    items.Add(new GhnServiceOption
                    {
                        ServiceId = serviceIdElement.GetInt32(),
                        ShortName = shortNameElement.GetString() ?? string.Empty,
                        ServiceTypeId = serviceTypeIdElement.GetInt32()
                    });
                }
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN get available services failed");
            return new List<GhnServiceOption>();
        }
    }

    public async Task<GhnCreateOrderResponse> CreateOrderAsync(GhnCreateOrderRequest request, CancellationToken ct = default)
    {
        try
        {
            var json = JsonConvert.SerializeObject(request, JsonSettings);
            using var content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
            content.Headers.Add("ShopId", _shopId);
            content.Headers.Add("Token", _token);

            using var response = await _httpClient.PostAsync("/shiip/public-api/v2/shipping-order/create", content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("GHN create order response {StatusCode}: {Body}", response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                return new GhnCreateOrderResponse { Success = false, Message = $"HTTP {(int)response.StatusCode}: {body}" };
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var codeElement) && codeElement.GetInt32() != 200)
            {
                return new GhnCreateOrderResponse
                {
                    Success = false,
                    Message = root.TryGetProperty("message", out var msg) ? msg.GetString() : body
                };
            }

            if (root.TryGetProperty("data", out var dataElement) && dataElement.TryGetProperty("order_code", out var orderCodeElement))
            {
                return new GhnCreateOrderResponse
                {
                    Success = true,
                    OrderCode = orderCodeElement.GetString()
                };
            }

            return new GhnCreateOrderResponse { Success = false, Message = "Không đọc được order_code từ GHN." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN create order failed");
            return new GhnCreateOrderResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<GhnPrintTokenResponse> CreatePrintTokenAsync(string orderCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return new GhnPrintTokenResponse { Success = false, Message = "Thiếu mã đơn hàng." };
        }

        try
        {
            var payload = new GhnPrintTokenRequest { OrderCodes = new List<string> { orderCode } };
            var json = JsonConvert.SerializeObject(payload);
            using var content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
            content.Headers.Add("Token", _token);

            using var response = await _httpClient.PostAsync("/shiip/public-api/v2/a5/gen-token", content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("GHN print token response {StatusCode}: {Body}", response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                return new GhnPrintTokenResponse { Success = false, Message = $"HTTP {(int)response.StatusCode}: {body}" };
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("code", out var codeElement) && codeElement.GetInt32() != 200)
            {
                return new GhnPrintTokenResponse
                {
                    Success = false,
                    Message = root.TryGetProperty("message", out var msg) ? msg.GetString() : body
                };
            }

            if (root.TryGetProperty("data", out var dataElement) && dataElement.TryGetProperty("token", out var tokenElement))
            {
                var token = tokenElement.GetString();
                var baseUrl = _configuration["GHN:BaseUrl"] ?? "https://dev-online-gateway.ghn.vn";
                return new GhnPrintTokenResponse
                {
                    Success = true,
                    Token = token,
                    A5Url = $"{baseUrl}/a5/public-api/printA5?token={token}",
                    Print80x80Url = $"{baseUrl}/a5/public-api/print80x80?token={token}"
                };
            }

            return new GhnPrintTokenResponse { Success = false, Message = "Không đọc được token in từ GHN." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN create print token failed");
            return new GhnPrintTokenResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<GhnEstimateFeeResponse?> EstimateShippingFeeAsync(GhnEstimateFeeRequest request, CancellationToken ct = default)
    {
        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["to_district_id"] = request.ToDistrictId,
                ["to_ward_code"] = request.ToWardCode,
                ["weight"] = request.Weight,
                ["length"] = request.Length,
                ["width"] = request.Width,
                ["height"] = request.Height,
                ["insurance_value"] = request.InsuranceValue,
                ["coupon"] = request.Coupon ?? ""
            };

            if (request.FromDistrictId.HasValue)
                payload["from_district_id"] = request.FromDistrictId.Value;
            if (!string.IsNullOrWhiteSpace(request.FromWardCode))
                payload["from_ward_code"] = request.FromWardCode;
            if (request.ServiceId.HasValue)
                payload["service_id"] = request.ServiceId.Value;
            if (request.ServiceTypeId.HasValue)
                payload["service_type_id"] = request.ServiceTypeId.Value;

            if (request.Items != null && request.Items.Any())
            {
                var items = request.Items.Select(i => new Dictionary<string, object?>
                {
                    ["name"] = i.Name,
                    ["quantity"] = i.Quantity,
                    ["weight"] = i.Weight,
                    ["length"] = i.Length,
                    ["width"] = i.Width,
                    ["height"] = i.Height
                });
                payload["items"] = items;
            }

            var json = JsonConvert.SerializeObject(payload, JsonSettings);
            using var content = new StringContent(json, Encoding.UTF8, new MediaTypeWithQualityHeaderValue("application/json"));
            content.Headers.Add("ShopId", _shopId);
            content.Headers.Add("Token", _token);

            using var response = await _httpClient.PostAsync("/shiip/public-api/v2/shipping-order/fee", content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("GHN estimate fee response {StatusCode}: {Body}", response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                var fee = new GhnEstimateFeeResponse();
                if (data.TryGetProperty("total", out var total) && total.TryGetInt32(out var totalValue))
                    fee.Total = totalValue;
                if (data.TryGetProperty("service_fee", out var serviceFee) && serviceFee.TryGetInt32(out var serviceFeeValue))
                    fee.ServiceFee = serviceFeeValue;
                if (data.TryGetProperty("insurance_fee", out var insuranceFee) && insuranceFee.TryGetInt32(out var insuranceFeeValue))
                    fee.InsuranceFee = insuranceFeeValue;
                if (data.TryGetProperty("coupon_value", out var couponValue) && couponValue.TryGetInt32(out var couponValueValue))
                    fee.CouponValue = couponValueValue;

                return fee;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN estimate shipping fee failed");
            return null;
        }
    }

    private async Task<T> CallGetAsync<T>(string relativeUrl, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(relativeUrl, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogInformation("GHN GET {Url} -> {StatusCode}: {Body}", relativeUrl, response.StatusCode, body);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {body}");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return default!;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var dataElement) &&
                dataElement.ValueKind != JsonValueKind.Null &&
                dataElement.ValueKind != JsonValueKind.Undefined)
            {
                var dataJson = dataElement.GetRawText();
                return JsonConvert.DeserializeObject<T>(dataJson, new JsonSerializerSettings { Error = (sender, args) => { args.ErrorContext.Handled = true; } }) ?? default!;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // ignore and fallback to direct deserialization below
        }

        return JsonConvert.DeserializeObject<T>(body, new JsonSerializerSettings { Error = (sender, args) => { args.ErrorContext.Handled = true; } }) ?? default!;
    }
}
