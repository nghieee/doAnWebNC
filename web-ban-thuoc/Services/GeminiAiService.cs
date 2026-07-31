using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services
{
    public class GeminiAiService : IGeminiAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly LongChauDbContext _context;

        public GeminiAiService(HttpClient httpClient, IConfiguration config, LongChauDbContext context)
        {
            _httpClient = httpClient;
            _config = config;
            _context = context;
        }

        public async Task<string> GetAdviceAsync(string question, string baseUrl)
        {
            var geminiKey = _config["Gemini:ApiKey"];
            
            if (string.IsNullOrEmpty(geminiKey))
            {
                return "❌ LỖI CẤU HÌNH: Không tìm thấy 'Gemini:ApiKey' trong file appsettings.json!";
            }

            try
            {
                var products = _context.Products.Where(p => p.IsActive).ToList();
                var productList = string.Join("\n", products.Select(p => $"- [{p.ProductName} ({p.Brand})]({baseUrl}/Product/Details/{p.ProductId}): {p.Uses}, Giá: {p.Price:N0}đ. Đối tượng: {p.TargetUsers}"));
                
                var prompt = $@"Dưới đây là danh sách sản phẩm của nhà thuốc:\n{productList}\n\nKhách hỏi: '{question}'\n\nHướng dẫn trả lời:\n1. Trả lời thân thiện và chuyên nghiệp như một dược sĩ\n2. Khi đề cập đến sản phẩm cụ thể, chỉ sử dụng cú pháp {{PRODUCT:ID}} để hiển thị card sản phẩm\n3. Ví dụ: 'Tôi khuyên bạn dùng {{PRODUCT:10}} để tăng cường sức đề kháng'\n4. Có thể đề cập nhiều sản phẩm cùng lúc: 'Bạn có thể chọn {{PRODUCT:8}} hoặc {{PRODUCT:11}}'\n5. KHÔNG thêm tên sản phẩm trong ngoặc đơn sau {{PRODUCT:ID}}\n6. KHÔNG thêm tên sản phẩm vào text, chỉ dùng {{PRODUCT:ID}}\n7. Viết câu hoàn chỉnh, tự nhiên\n8. Luôn giải thích lý do tại sao sản phẩm phù hợp\n9. Giữ câu trả lời ngắn gọn, dễ hiểu";

                // 👉 CHỐT CHÍNH XÁC MÔ HÌNH TỪ DANH SÁCH GOOGLE CẤP CHO BẠN: "gemini-2.0-flash"
                var modelName = "gemini-2.0-flash";
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={geminiKey}";
                
                var geminiPayload = new
                {
                    contents = new[] {
                        new {
                            role = "user",
                            parts = new[] { new { text = prompt } }
                        }
                    }
                };

                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(geminiPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
                
                var response = await _httpClient.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"\n[GEMINI API ERROR - {(int)response.StatusCode}]: {json}\n");
                    return $"❌ GOOGLE API TỪ CHỐI (Mã lỗi {(int)response.StatusCode}):\n{json}";
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var parts = candidates[0].GetProperty("content").GetProperty("parts");
                    if (parts.GetArrayLength() > 0)
                    {
                        var answer = parts[0].GetProperty("text").GetString();
                        if (!string.IsNullOrWhiteSpace(answer)) return answer;
                    }
                }
                
                return "⚠️ Gemini không trả về nội dung văn bản.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[GEMINI EXCEPTION]: {ex.Message}\n");
                return $"❌ LỖI HỆ THỐNG KHI GỌI GEMINI: {ex.Message}";
            }
        }

        public async Task<object> GetProductCardAsync(int productId, string baseUrl)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == productId && p.IsActive);
            if (product == null) return null;

            var productImage = _context.ProductImages
                .Where(pi => pi.ProductId == product.ProductId)
                .OrderBy(pi => pi.IsMain == true ? 0 : 1)
                .ThenBy(pi => pi.SortOrder ?? 999)
                .FirstOrDefault();

            string imageUrl;
            if (productImage != null && !string.IsNullOrEmpty(productImage.ImageUrl))
            {
                if (!productImage.ImageUrl.StartsWith("/") && !productImage.ImageUrl.StartsWith("http"))
                    imageUrl = "/images/products/" + productImage.ImageUrl;
                else
                    imageUrl = productImage.ImageUrl;
            }
            else
            {
                imageUrl = "/images/products/default.png";
            }

            return await Task.FromResult(new
            {
                id = product.ProductId,
                name = product.ProductName,
                brand = product.Brand,
                price = product.Price,
                image = imageUrl,
                url = $"{baseUrl}/Products/{product.ProductId}"
            });
        }
    }
}