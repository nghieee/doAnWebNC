using Microsoft.AspNetCore.Mvc;
using web_ban_thuoc.Services;

namespace web_ban_thuoc.Controllers
{
    [ApiController]
    [Route("api/aibot")]
    public class AiBotController : ControllerBase
    {
        private readonly IGeminiAiService _aiService;
        private readonly string _baseUrl;

        public AiBotController(IGeminiAiService aiService, IConfiguration config)
        {
            _aiService = aiService;
            _baseUrl = config["AppSettings:BaseUrl"] ?? "https://localhost:5226";
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] AiBotRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Question))
                return BadRequest(new { error = "Câu hỏi không được để trống." });

            try
            {
                var answer = await _aiService.GetAdviceAsync(req.Question, _baseUrl);
                return Ok(new { answer });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("product/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var productCard = await _aiService.GetProductCardAsync(id, _baseUrl);
            if (productCard == null)
            {
                return NotFound(new { error = "Không tìm thấy sản phẩm" });
            }
            return Ok(productCard);
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