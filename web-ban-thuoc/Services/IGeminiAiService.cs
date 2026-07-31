namespace web_ban_thuoc.Services
{
    public interface IGeminiAiService
    {
        Task<string> GetAdviceAsync(string question, string baseUrl);
        Task<object> GetProductCardAsync(int productId, string baseUrl);
    }
}