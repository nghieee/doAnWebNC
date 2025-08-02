using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using web_ban_thuoc.Models;
using System.Linq;

namespace web_ban_thuoc.Controllers
{
    [ApiController]
    [Route("api/aibot")]
    public class AiBotController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly LongChauDbContext _context;
        private readonly string _baseUrl;
        
        public AiBotController(IConfiguration config, LongChauDbContext context) 
        { 
            _config = config; 
            _context = context;
            // Sử dụng localhost cho development, có thể cấu hình trong appsettings sau
            _baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:5226";
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AiBotRequest req)
        {
            var geminiKey = _config["Gemini:ApiKey"];
            if (!string.IsNullOrEmpty(geminiKey))
            {
                // Lấy toàn bộ sản phẩm từ DB (có thể lọc theo nhu cầu nếu muốn)
                var products = _context.Products.Where(p => p.IsActive).ToList();
                // Ghép thông tin sản phẩm vào prompt, mỗi sản phẩm có kèm link chi tiết
                var productList = string.Join("\n", products.Select(p => $"- [{p.ProductName} ({p.Brand})]({_baseUrl}/Product/Details/{p.ProductId}): {p.Uses}, Giá: {p.Price:N0}đ. Đối tượng: {p.TargetUsers}"));
                var prompt = $@"Dưới đây là danh sách sản phẩm của nhà thuốc:\n{productList}\n\nKhách hỏi: '{req.Question}'\n\nHướng dẫn trả lời:\n1. Trả lời thân thiện và chuyên nghiệp như một dược sĩ\n2. Khi đề cập đến sản phẩm cụ thể, chỉ sử dụng cú pháp {{PRODUCT:ID}} để hiển thị card sản phẩm\n3. Ví dụ: 'Tôi khuyên bạn dùng {{PRODUCT:10}} để tăng cường sức đề kháng'\n4. Có thể đề cập nhiều sản phẩm cùng lúc: 'Bạn có thể chọn {{PRODUCT:8}} hoặc {{PRODUCT:11}}'\n5. KHÔNG thêm tên sản phẩm trong ngoặc đơn sau {{PRODUCT:ID}}\n6. KHÔNG thêm tên sản phẩm vào text, chỉ dùng {{PRODUCT:ID}}\n7. Viết câu hoàn chỉnh, tự nhiên\n8. Luôn giải thích lý do tại sao sản phẩm phù hợp\n9. Giữ câu trả lời ngắn gọn, dễ hiểu";
                // Gọi Gemini API
                using var http = new HttpClient();
                var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key={geminiKey}";
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
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await http.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(500, new { error = "Lỗi gọi Gemini: " + json });
                }
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string answer = null;
                try
                {
                    answer = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                }
                catch
                {
                    answer = null;
                }
                if (string.IsNullOrWhiteSpace(answer))
                {
                    return StatusCode(500, new { error = "Không nhận được phản hồi từ Gemini." });
                }
                return Ok(new { answer });
            }

            // Nếu không có Gemini thì fallback OpenAI như cũ
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = _config["OpenAI:ApiKey"];
            }
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest(new { error = "API key chưa được cấu hình." });
            if (string.IsNullOrWhiteSpace(req.Question))
                return BadRequest(new { error = "Câu hỏi không được để trống." });
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var payload = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[] {
                        new { role = "system", content = "Bạn là dược sĩ AI tư vấn sức khỏe, thuốc, sản phẩm và hướng dẫn sử dụng website cho khách hàng một cách thân thiện, dễ hiểu." },
                        new { role = "user", content = req.Question }
                    },
                    max_tokens = 512,
                    temperature = 0.7
                };
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await http.PostAsync("https://api.openai.com/v1/chat/completions", content);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return StatusCode(500, new { error = "Lỗi gọi OpenAI: " + err });
                }
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var answer = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                return Ok(new { answer });
            }
        }

        [HttpGet("product/{id}")]
        public IActionResult GetProduct(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id && p.IsActive);
            if (product == null)
                return NotFound(new { error = "Không tìm thấy sản phẩm" });

            // Lấy ảnh của sản phẩm
            var productImage = _context.ProductImages
                .Where(pi => pi.ProductId == product.ProductId)
                .OrderBy(pi => pi.IsMain == true ? 0 : 1) // Ưu tiên ảnh chính trước
                .ThenBy(pi => pi.SortOrder ?? 999) // Sau đó theo thứ tự
                .FirstOrDefault();
            
            string imageUrl;
            if (productImage != null && !string.IsNullOrEmpty(productImage.ImageUrl))
            {
                // Nếu chỉ có tên file, thêm đường dẫn đầy đủ
                if (!productImage.ImageUrl.StartsWith("/") && !productImage.ImageUrl.StartsWith("http"))
                {
                    imageUrl = "/images/products/" + productImage.ImageUrl;
                }
                else
                {
                    imageUrl = productImage.ImageUrl;
                }
            }
            else
            {
                imageUrl = "/images/products/default.png";
            }
            
            // Debug: Log thông tin ảnh
            Console.WriteLine($"Product {product.ProductId}: {product.ProductName}");
            Console.WriteLine($"Found image: {productImage?.ImageUrl}");
            Console.WriteLine($"Final image URL: {imageUrl}");
            
            return Ok(new
            {
                id = product.ProductId,
                name = product.ProductName,
                brand = product.Brand,
                price = product.Price,
                image = imageUrl,
                url = $"{_baseUrl}/Products/{product.ProductId}"
            });
        }

        [HttpPost("save-chat-history")]
        public IActionResult SaveChatHistory([FromBody] List<ChatHistoryItem> history)
        {
            try
            {
                var jsonHistory = System.Text.Json.JsonSerializer.Serialize(history);
                HttpContext.Session.SetString("AiChatHistory", jsonHistory);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi lưu lịch sử chat: " + ex.Message });
            }
        }
    }

    public class AiBotRequest
    {
        public string Question { get; set; } = string.Empty;
    }

    public class ChatHistoryItem
    {
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
    }
} 