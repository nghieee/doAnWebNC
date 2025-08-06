using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services
{
    public interface IPayOSService
    {
        Task<PayOSResponse> CreatePaymentAsync(PayOSRequest request);
        Task<PayOSStatusResponse> CheckPaymentStatusAsync(string orderCode);
        bool VerifyWebhookSignature(string signature, string data);
        string GenerateSignature(string data);
    }

    public class PayOSService : IPayOSService
    {
        private readonly ILogger<PayOSService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        // PayOS Configuration
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _baseUrl;

        public PayOSService(ILogger<PayOSService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;

            // Lấy cấu hình từ appsettings.json
            _clientId = _configuration["PayOS:ClientId"] ?? "adbf3b7a-17bd-4867-974e-6f1ddcc9ad6e";
            _apiKey = _configuration["PayOS:ApiKey"] ?? "cf4608e4-eb83-4e25-8619-8d9b57a16314";
            _checksumKey = _configuration["PayOS:ChecksumKey"] ?? "a94f6207b4e5d2311009fe88ff58a3aaa6a547795696cf1450e3003a552085b7";
            _baseUrl = _configuration["PayOS:BaseUrl"] ?? "https://api-merchant.payos.vn";

            // Cấu hình HttpClient
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PayOS-Integration/1.0");
        }

        public async Task<PayOSResponse> CreatePaymentAsync(PayOSRequest request)
        {
            try
            {
                _logger.LogInformation($"PayOS CreatePaymentAsync called with OrderCode: {request.OrderCode}, Amount: {request.Amount}");
                
                // Validate request
                if (request.OrderCode <= 0)
                {
                    _logger.LogError("OrderCode is invalid");
                    return new PayOSResponse { Code = "99", Desc = "OrderCode không được để trống" };
                }
                
                if (request.Amount <= 0)
                {
                    _logger.LogError($"Invalid amount: {request.Amount}");
                    return new PayOSResponse { Code = "99", Desc = "Số tiền phải lớn hơn 0" };
                }
                
                // Tạo chuỗi dữ liệu để ký theo định dạng PayOS
                // ĐÚNG CHUẨN: amount=...&cancelUrl=...&description=...&orderCode=...&returnUrl=...
                var dataToSign = $"amount={request.Amount}&cancelUrl={request.CancelUrl}&description={request.Description}&orderCode={request.OrderCode}&returnUrl={request.ReturnUrl}";
                
                // Log để debug
                _logger.LogInformation($"PayOS data to sign: {dataToSign}");
                _logger.LogInformation($"PayOS checksumKey: {_checksumKey}");
                
                request.Signature = GenerateSignature(dataToSign);
                
                _logger.LogInformation($"PayOS signature generated: {request.Signature}");

                // Serialize request
                var jsonRequest = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                
                // Thêm headers theo documentation PayOS
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                _logger.LogInformation($"PayOS request JSON: {jsonRequest}");

                // Gọi API PayOS theo documentation mới
                var response = await _httpClient.PostAsync("/v2/payment-requests", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"PayOS CreatePayment Response Status: {response.StatusCode}");
                _logger.LogInformation($"PayOS CreatePayment Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
                _logger.LogInformation($"PayOS CreatePayment Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var payOSResponse = JsonConvert.DeserializeObject<PayOSResponse>(responseContent);
                    _logger.LogInformation($"PayOS Deserialized Response: Code={payOSResponse?.Code}, Desc={payOSResponse?.Desc}, Data={payOSResponse?.Data != null}");
                    return payOSResponse ?? new PayOSResponse { Code = "99", Desc = "Invalid response" };
                }
                else
                {
                    _logger.LogError($"PayOS API Error: {response.StatusCode} - {responseContent}");
                    return new PayOSResponse { Code = "99", Desc = $"API call failed: {responseContent}" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment");
                _logger.LogError($"Exception Details: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                return new PayOSResponse { Code = "99", Desc = ex.Message };
            }
        }

        public async Task<PayOSStatusResponse> CheckPaymentStatusAsync(string orderCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v2/payment-requests/{orderCode}");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"PayOS Status Check Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var statusResponse = JsonConvert.DeserializeObject<PayOSStatusResponse>(responseContent);
                    return statusResponse ?? new PayOSStatusResponse { Error = -1, Message = "Invalid response" };
                }
                else
                {
                    _logger.LogError($"PayOS Status API Error: {response.StatusCode} - {responseContent}");
                    return new PayOSStatusResponse { Error = (int)response.StatusCode, Message = "API call failed" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking PayOS payment status");
                return new PayOSStatusResponse { Error = -1, Message = ex.Message };
            }
        }

        public bool VerifyWebhookSignature(string signature, string data)
        {
            try
            {
                var expectedSignature = GenerateSignature(data);
                return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying webhook signature");
                return false;
            }
        }

        public string GenerateSignature(string data)
        {
            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToHexString(hashBytes).ToLower();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating signature");
                return string.Empty;
            }
        }
    }
} 