namespace web_ban_thuoc.Services
{
    public interface IGeminiAiService
    {
        Task<string> GetAdviceAsync(string question, string baseUrl, List<ChatHistoryItem>? history = null);
        Task<object> GetProductCardAsync(int productId, string baseUrl);
    }

    public class ChatHistoryItem
    {
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
    }
}