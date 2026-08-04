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

        public async Task<string> GetAdviceAsync(string question, string baseUrl, List<ChatHistoryItem>? history = null)
        {
            var provider = _config["AiProvider"] ?? "Gemini";

            if (provider.Equals("Groq", StringComparison.OrdinalIgnoreCase))
            {
                return await GetGroqAdviceAsync(question, baseUrl, history);
            }
            else if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                return await GetOpenAiAdviceAsync(question, baseUrl, history);
            }

            return await GetGeminiAdviceAsync(question, baseUrl, history);
        }

        private async Task<string> GetGroqAdviceAsync(string question, string baseUrl, List<ChatHistoryItem>? history)
        {
            var apiKey = _config["Groq:ApiKey"];
            var model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile";
            var url = "https://api.groq.com/openai/v1/chat/completions";
            return await GetOpenAiCompatibleAdviceAsync(question, baseUrl, history, apiKey, model, url);
        }

        private async Task<string> GetOpenAiAdviceAsync(string question, string baseUrl, List<ChatHistoryItem>? history)
        {
            var apiKey = _config["OpenAI:ApiKey"];
            var model = _config["OpenAI:Model"] ?? "gpt-4o-mini";
            var url = "https://api.openai.com/v1/chat/completions";
            return await GetOpenAiCompatibleAdviceAsync(question, baseUrl, history, apiKey, model, url);
        }

        private async Task<string> GetOpenAiCompatibleAdviceAsync(string question, string baseUrl, List<ChatHistoryItem>? history, string apiKey, string model, string url)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return GetLocalFallbackAdvice(question, baseUrl);
            }

            try
            {
                var products = _context.Products.Where(p => p.IsActive).ToList();
                var productList = string.Join("\n", products.Select(p => 
                    $"Sản phẩm [ID: {p.ProductId}]: {p.ProductName}\n" +
                    $"Thương hiệu: {p.Brand}\n" +
                    $"Công dụng chính: {p.Uses}\n" +
                    $"Thành phần hoạt chất: {p.Ingredients}\n" +
                    $"Liều lượng khuyên dùng: {p.Dosage}\n" +
                    $"Chống chỉ định: {p.Contraindications}\n" +
                    $"Giá: {p.Price:N0}đ\n" +
                    $"Đối tượng sử dụng phù hợp: {p.TargetUsers}\n" +
                    $"---"
                ));

                var systemInstructionText = $@"Bạn là Trợ lý Dược sĩ Lâm sàng AI đại diện cho Nhà thuốc Long Châu Phake.
Nhiệm vụ của bạn là tư vấn sức khỏe, triệu chứng bệnh lý và đề xuất dược phẩm/vitamin phù hợp từ danh sách sản phẩm của nhà thuốc.

Quy tắc ứng xử và nghiệp vụ:
1. CHUYÊN NGHIỆP & AN TOÀN LÀ TRÊN HẾT:
   - Tuyệt đối không giới thiệu bừa bãi. Phải đối chiếu kỹ nhóm đối tượng sử dụng (Target Users) của sản phẩm. Ví dụ: KHÔNG được giới thiệu viên uống/siro có ghi chú chỉ định cho phụ nữ mang thai hoặc nhóm đối tượng đặc biệt nếu khách hàng chỉ là người lớn bình thường bị cảm cúm phổ thông, trừ khi họ xác nhận đang có thai hoặc bạn đã hỏi rõ.
   - Khi khách hàng hỏi một câu mơ hồ, không đầy đủ thông tin (ví dụ: 'tôi đang hơi cảm, có gì uống không', 'tôi bị đau bụng', 'tư vấn thuốc ho'...), bạn KHÔNG được vội vàng giới thiệu sản phẩm ngay lập tức. Hãy đóng vai một dược sĩ có tâm, đặt từ 1-3 câu hỏi làm rõ tình trạng của khách hàng trước (ví dụ: 'Bạn bị cảm bao lâu rồi? Có kèm theo sốt, ho hay sổ mũi gì không ạ? Bạn bao nhiêu tuổi và có đang mang thai hay có bệnh nền gì không để mình tư vấn chính xác nhất?').
   - Chỉ đưa ra khuyến nghị sản phẩm cụ thể khi đã hiểu rõ hơn các triệu chứng chính của khách hàng.
2. CÚ PHÁP ĐỀ XUẤT SẢN PHẨM:
   - Khi giới thiệu một sản phẩm cụ thể từ danh sách có sẵn, bạn BẮT BUỘC phải dùng cú pháp {{PRODUCT:ID}} để hệ thống hiển thị thẻ sản phẩm. Ví dụ: 'Bạn có thể uống {{PRODUCT:10}} để cải thiện cơn đau.'
   - KHÔNG bao giờ viết tên sản phẩm ngay sau hoặc gần thẻ (ví dụ: tránh viết '{{PRODUCT:10}} (Paracetamol)'). Chỉ dùng duy nhất thẻ {{PRODUCT:ID}}.
3. TỰ NHIÊN & XÃ GIAO:
   - Nếu khách hàng hỏi những câu xã giao, chào hỏi (chào bạn, cảm ơn, tạm biệt) hoặc hỏi các câu ngoài lề y tế (thời tiết, toán học, tán gẫu...), hãy trả lời tự nhiên, dí dỏm và thân thiện như con người thực. Tuyệt đối không được cố giới thiệu sản phẩm nếu không liên quan.

Dưới đây là danh sách sản phẩm hiện có tại nhà thuốc của chúng tôi:
{productList}";

                var messagesList = new List<object>();
                messagesList.Add(new { role = "system", content = systemInstructionText });

                string lastRole = "";
                if (history != null)
                {
                    foreach (var h in history)
                    {
                        var role = (h.Type == "user" || h.Type.ToLower() == "user") ? "user" : "assistant";
                        messagesList.Add(new { role = role, content = h.Content });
                        lastRole = role;
                    }
                }

                if (lastRole != "user")
                {
                    messagesList.Add(new { role = "user", content = question });
                }

                var payload = new
                {
                    model = model,
                    messages = messagesList.ToArray(),
                    temperature = 0.3
                };

                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"\n[OPENAI COMPATIBLE API ERROR - {(int)response.StatusCode}]: {json}\n");
                    string apiErrorDetails = $"HTTP {(int)response.StatusCode}";
                    try
                    {
                        using var errDoc = JsonDocument.Parse(json);
                        if (errDoc.RootElement.TryGetProperty("error", out var errEl) && errEl.TryGetProperty("message", out var msgEl))
                        {
                            apiErrorDetails += $": {msgEl.GetString()}";
                        }
                    }
                    catch { }
                    var fallbackResult = GetLocalFallbackAdvice(question, baseUrl);
                    return $"{fallbackResult}\n\n*(⚠️ Chế độ dự phòng: Kết nối API thất bại - {apiErrorDetails})*";
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    if (message.TryGetProperty("content", out var contentEl))
                    {
                        var answer = contentEl.GetString();
                        if (!string.IsNullOrWhiteSpace(answer)) return answer;
                    }
                }

                return GetLocalFallbackAdvice(question, baseUrl) + "\n\n*(⚠️ Chế độ dự phòng: API không trả về nội dung text)*";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[OPENAI COMPATIBLE EXCEPTION]: {ex.Message}\n");
                var fallbackResult = GetLocalFallbackAdvice(question, baseUrl);
                return $"{fallbackResult}\n\n*(⚠️ Chế độ dự phòng: Lỗi kết nối hệ thống API - {ex.Message})*";
            }
        }

        private async Task<string> GetGeminiAdviceAsync(string question, string baseUrl, List<ChatHistoryItem>? history = null)
        {
            var geminiKey = _config["Gemini:ApiKey"];
            
            if (string.IsNullOrEmpty(geminiKey))
            {
                return GetLocalFallbackAdvice(question, baseUrl);
            }

            try
            {
                var products = _context.Products.Where(p => p.IsActive).ToList();
                
                var productList = string.Join("\n", products.Select(p => 
                    $"Sản phẩm [ID: {p.ProductId}]: {p.ProductName}\n" +
                    $"Thương hiệu: {p.Brand}\n" +
                    $"Công dụng chính: {p.Uses}\n" +
                    $"Thành phần hoạt chất: {p.Ingredients}\n" +
                    $"Liều lượng khuyên dùng: {p.Dosage}\n" +
                    $"Chống chỉ định: {p.Contraindications}\n" +
                    $"Giá: {p.Price:N0}đ\n" +
                    $"Đối tượng sử dụng phù hợp: {p.TargetUsers}\n" +
                    $"---"
                ));
                
                var systemInstructionText = $@"Bạn là Trợ lý Dược sĩ Lâm sàng AI đại diện cho Nhà thuốc Long Châu Phake.
Nhiệm vụ của bạn là tư vấn sức khỏe, triệu chứng bệnh lý và đề xuất dược phẩm/vitamin phù hợp từ danh sách sản phẩm của nhà thuốc.

Quy tắc ứng xử và nghiệp vụ:
1. CHUYÊN NGHIỆP & AN TOÀN LÀ TRÊN HẾT:
   - Tuyệt đối không giới thiệu bừa bãi. Phải đối chiếu kỹ nhóm đối tượng sử dụng (Target Users) của sản phẩm. Ví dụ: KHÔNG được giới thiệu viên uống/siro có ghi chú chỉ định cho phụ nữ mang thai hoặc nhóm đối tượng đặc biệt nếu khách hàng chỉ là người lớn bình thường bị cảm cúm phổ thông, trừ khi họ xác nhận đang có thai hoặc bạn đã hỏi rõ.
   - Khi khách hàng hỏi một câu mơ hồ, không đầy đủ thông tin (ví dụ: 'tôi đang hơi cảm, có gì uống không', 'tôi bị đau bụng', 'tư vấn thuốc ho'...), bạn KHÔNG được vội vàng giới thiệu sản phẩm ngay lập tức. Hãy đóng vai một dược sĩ có tâm, đặt từ 1-3 câu hỏi làm rõ tình trạng của khách hàng trước (ví dụ: 'Bạn bị cảm bao lâu rồi? Có kèm theo sốt, ho hay sổ mũi gì không ạ? Bạn bao nhiêu tuổi và có đang mang thai hay có bệnh nền gì không để mình tư vấn chính xác nhất?').
   - Chỉ đưa ra khuyến nghị sản phẩm cụ thể khi đã hiểu rõ hơn các triệu chứng chính của khách hàng.
2. CÚ PHÁP ĐỀ XUẤT SẢN PHẨM:
   - Khi giới thiệu một sản phẩm cụ thể từ danh sách có sẵn, bạn BẮT BUỘC phải dùng cú pháp {{PRODUCT:ID}} để hệ thống hiển thị thẻ sản phẩm. Ví dụ: 'Bạn có thể uống {{PRODUCT:10}} để cải thiện cơn đau.'
   - KHÔNG bao giờ viết tên sản phẩm ngay sau hoặc gần thẻ (ví dụ: tránh viết '{{PRODUCT:10}} (Paracetamol)'). Chỉ dùng duy nhất thẻ {{PRODUCT:ID}}.
3. TỰ NHIÊN & XÃ GIAO:
   - Nếu khách hàng hỏi những câu xã giao, chào hỏi (chào bạn, cảm ơn, tạm biệt) hoặc hỏi các câu ngoài lề y tế (thời tiết, toán học, tán gẫu...), hãy trả lời tự nhiên, dí dỏm và thân thiện như con người thực. Tuyệt đối không được cố giới thiệu sản phẩm nếu không liên quan.

Dưới đây là danh sách sản phẩm hiện có tại nhà thuốc của chúng tôi:
{productList}";

                var contentsList = new List<object>();
                string lastRole = "";
                
                if (history != null)
                {
                    foreach (var h in history)
                    {
                        var role = (h.Type == "user" || h.Type.ToLower() == "user") ? "user" : "model";
                        
                        if (role == lastRole) continue;
                        
                        contentsList.Add(new
                        {
                            role = role,
                            parts = new[] { new { text = h.Content } }
                        });
                        lastRole = role;
                    }
                }
                
                if (lastRole != "user")
                {
                    contentsList.Add(new
                    {
                        role = "user",
                        parts = new[] { new { text = question } }
                    });
                }

                var modelName = "gemini-2.0-flash";
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={geminiKey}";
                
                var geminiPayload = new
                {
                    contents = contentsList.ToArray(),
                    systemInstruction = new
                    {
                        parts = new[] {
                            new { text = systemInstructionText }
                        }
                    }
                };

                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(geminiPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
                
                var response = await _httpClient.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    bool isQuotaOrModelError = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || json.Contains("gemini-2.0-flash") || json.Contains("quota");
                    if (isQuotaOrModelError)
                    {
                        Console.WriteLine($"\n[GEMINI 2.0 ERROR - {(int)response.StatusCode}]: Falling back to gemini-1.5-flash...\n");
                        var fallbackUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={geminiKey}";
                        var fallbackContent = new StringContent(jsonPayload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
                        
                        var fallbackResponse = await _httpClient.PostAsync(fallbackUrl, fallbackContent);
                        var fallbackJson = await fallbackResponse.Content.ReadAsStringAsync();
                        
                        if (fallbackResponse.IsSuccessStatusCode)
                        {
                            response = fallbackResponse;
                            json = fallbackJson;
                        }
                        else
                        {
                            Console.WriteLine($"\n[GEMINI 1.5 FALLBACK ERROR - {(int)fallbackResponse.StatusCode}]: {fallbackJson}\n");
                        }
                    }
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"\n[GEMINI API ERROR - {(int)response.StatusCode}]: {json}\n");
                    
                    string apiErrorDetails = $"HTTP {(int)response.StatusCode}";
                    try
                    {
                        using var errDoc = JsonDocument.Parse(json);
                        if (errDoc.RootElement.TryGetProperty("error", out var errEl) && errEl.TryGetProperty("message", out var msgEl))
                        {
                            apiErrorDetails += $": {msgEl.GetString()}";
                        }
                    }
                    catch { }

                    var fallbackResult = GetLocalFallbackAdvice(question, baseUrl);
                    return $"{fallbackResult}\n\n*(⚠️ Chế độ dự phòng: Kết nối Gemini API thất bại - {apiErrorDetails})*";
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
                
                return GetLocalFallbackAdvice(question, baseUrl) + "\n\n*(⚠️ Chế độ dự phòng: Gemini API không trả về nội dung text)*";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[GEMINI EXCEPTION]: {ex.Message}\n");
                var fallbackResult = GetLocalFallbackAdvice(question, baseUrl);
                return $"{fallbackResult}\n\n*(⚠️ Chế độ dự phòng: Lỗi kết nối hệ thống Gemini API - {ex.Message})*";
            }
        }

        private string GetLocalFallbackAdvice(string question, string baseUrl)
        {
            try
            {
                var queryStr = question.ToLower();
                var products = _context.Products.Where(p => p.IsActive).ToList();
                var matchedProducts = new List<Product>();
                
                var cleanQuestion = queryStr.Trim().TrimEnd('?', '!', '.', ' ');
                
                // Conversational intent checks for local chatbot fallback
                if (cleanQuestion == "chao" || cleanQuestion == "xin chao" || cleanQuestion == "hi" || cleanQuestion == "hello" || cleanQuestion.StartsWith("chào") || cleanQuestion.StartsWith("xin chào"))
                {
                    return "👋 Xin chào! Tôi là trợ lý dược sĩ ảo của nhà thuốc Long Châu. Hôm nay tôi có thể giúp gì cho sức khỏe của bạn?";
                }
                if (cleanQuestion.Contains("cam on") || cleanQuestion.Contains("cảm ơn") || cleanQuestion == "thanks" || cleanQuestion == "thank you" || cleanQuestion == "tks")
                {
                    return "Dạ không có gì ạ! Chúc bạn luôn có nhiều sức khỏe. Nếu cần tư vấn gì thêm bạn cứ nhắn tôi nhé! 😊";
                }
                if (cleanQuestion.Contains("tam biet") || cleanQuestion.Contains("tạm biệt") || cleanQuestion == "bye" || cleanQuestion == "goodbye")
                {
                    return "Tạm biệt bạn nhé! Hẹn gặp lại bạn. Chúc bạn một ngày tốt lành! 👋";
                }
                if (cleanQuestion.Contains("ban la ai") || cleanQuestion.Contains("bạn là ai") || cleanQuestion.Contains("ten la gi") || cleanQuestion.Contains("tên là gì"))
                {
                    return "Tôi là Trợ lý Dược sĩ AI của nhà thuốc Long Châu. Tôi có nhiệm vụ hỗ trợ bạn tư vấn bệnh lý và tìm các thuốc, vitamin hay thực phẩm chức năng thích hợp nhất. 💊";
                }
                if (cleanQuestion.Contains("khoe khong") || cleanQuestion.Contains("khỏe không") || cleanQuestion.Contains("co khoe khong") || cleanQuestion.Contains("có khỏe không"))
                {
                    return "Tôi là trợ lý AI nên lúc nào cũng đầy đủ năng lượng và sẵn sàng hỗ trợ tư vấn sức khỏe cho bạn 24/7! Bạn có khỏe không?";
                }

                // Keyword match search
                if (queryStr.Contains("gan") || queryStr.Contains("thai doc") || queryStr.Contains("bo gan") || queryStr.Contains("liver"))
                {
                    matchedProducts.AddRange(products.Where(p => p.ProductName.ToLower().Contains("gan") || p.Uses.ToLower().Contains("gan") || p.ProductName.ToLower().Contains("liver")));
                }
                if (queryStr.Contains("vitamin c") || queryStr.Contains("de khang") || queryStr.Contains("mien dich") || queryStr.Contains("vit c") || queryStr.Contains("suc de khang"))
                {
                    matchedProducts.AddRange(products.Where(p => p.ProductName.ToLower().Contains("vitamin c") || p.Uses.ToLower().Contains("de khang") || p.Uses.ToLower().Contains("mien dich") || p.ProductName.ToLower().Contains("c 500mg")));
                }
                if (queryStr.Contains("canxi") || queryStr.Contains("d3") || queryStr.Contains("xuong") || queryStr.Contains("khop") || queryStr.Contains("calcium"))
                {
                    matchedProducts.AddRange(products.Where(p => p.ProductName.ToLower().Contains("canxi") || p.ProductName.ToLower().Contains("d3") || p.Uses.ToLower().Contains("xuong") || p.Uses.ToLower().Contains("khop")));
                }
                if (queryStr.Contains("me bau") || queryStr.Contains("thai") || queryStr.Contains("sau sinh") || queryStr.Contains("bau") || queryStr.Contains("prenatal"))
                {
                    matchedProducts.AddRange(products.Where(p => p.ProductName.ToLower().Contains("prenatal") || p.Uses.ToLower().Contains("thai") || p.Uses.ToLower().Contains("bau")));
                }
                if (queryStr.Contains("sinh ly") || queryStr.Contains("nam") || queryStr.Contains("nu") || queryStr.Contains("than") || queryStr.Contains("maca"))
                {
                    matchedProducts.AddRange(products.Where(p => p.ProductName.ToLower().Contains("maca") || p.Uses.ToLower().Contains("sinh ly") || p.Uses.ToLower().Contains("than")));
                }
                if (queryStr.Contains("vitamin") || queryStr.Contains("tong hop") || queryStr.Contains("multivitamin") || queryStr.Contains("dinh duong"))
                {
                    matchedProducts.AddRange(products.Where(p => p.ProductName.ToLower().Contains("vitamin") || p.ProductName.ToLower().Contains("multivitamin") || p.Uses.ToLower().Contains("vitamin")));
                }

                // If no specific medical keywords match, check general keywords with stopwatch filter
                if (!matchedProducts.Any())
                {
                    var stopWords = new HashSet<string> { 
                        "bằng", "mấy", "là", "của", "cho", "ở", "trong", "trên", "dưới", "ngoài", "trong", "thế", 
                        "nào", "làm", "sao", "được", "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", 
                        "tám", "chín", "mười", "plus", "and", "the", "how", "what", "why", "who", "where",
                        "bao", "nhiêu", "này", "kia", "đó", "đây", "nào", "gì", "ai", "đâu"
                    };
                    var words = queryStr.Split(new[] { ' ', ',', '.', '?', '!', '+', '-', '*', '/', '=', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Where(w => w.Length > 2 && !stopWords.Contains(w) && !int.TryParse(w, out _))
                                       .ToList();
                    
                    foreach (var word in words)
                    {
                        var matches = products.Where(p => p.ProductName.ToLower().Contains(word) || p.Uses.ToLower().Contains(word)).ToList();
                        matchedProducts.AddRange(matches);
                        if (matchedProducts.DistinctBy(p => p.ProductId).Count() >= 3) break;
                    }
                }
                
                matchedProducts = matchedProducts.DistinctBy(p => p.ProductId).Take(3).ToList();

                if (matchedProducts.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("👋 Chào bạn! Tôi là trợ lý dược sĩ ảo của nhà thuốc.");
                    sb.AppendLine("Dựa trên câu hỏi của bạn, tôi xin khuyến nghị một số sản phẩm chăm sóc sức khỏe phù hợp dưới đây:");
                    sb.AppendLine();
                    
                    foreach (var p in matchedProducts)
                    {
                        sb.AppendLine($"💊 **{p.ProductName}**");
                        sb.AppendLine($"👉 *Công dụng*: {p.Uses}");
                        sb.AppendLine($"💵 *Giá bán*: {p.Price:N0}đ");
                        sb.AppendLine($"{{PRODUCT:{p.ProductId}}}");
                        sb.AppendLine();
                    }
                    
                    sb.AppendLine("Bạn có thể bấm trực tiếp vào nút **Thêm vào giỏ** trên thẻ sản phẩm để mua hàng nhanh, hoặc bấm vào thẻ để xem chi tiết hướng dẫn sử dụng sản phẩm nhé. Chúc bạn luôn nhiều sức khỏe!");
                    return sb.ToString();
                }
                
                return "👋 Xin chào! Tôi là trợ lý dược sĩ ảo. Hiện tại hệ thống AI đang bảo trì kết nối, và tôi chưa tìm thấy sản phẩm nào trong cửa hàng khớp chính xác với câu hỏi của bạn.\n\nBạn có thể thử hỏi về các nhu cầu như: **vitamin c, đề kháng, canxi, sinh lý nam/nữ, bổ gan giải độc, hay dinh dưỡng cho mẹ bầu** để tôi tìm sản phẩm phù hợp nhất cho bạn nhé!";
            }
            catch (Exception ex)
            {
                return $"❌ LỖI HỆ THỐNG DỰ PHÒNG: {ex.Message}";
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
                discountPrice = product.DiscountPrice,
                discountPercent = product.DiscountPercent,
                isDiscountActive = product.IsDiscountActive,
                image = imageUrl,
                url = $"{baseUrl}/Products/{product.ProductId}"
            });
        }
    }
}