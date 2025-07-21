using System;

namespace web_ban_thuoc.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string? SenderId { get; set; } // Id người gửi (user hoặc admin)
        public string? ReceiverId { get; set; } // Id người nhận (user hoặc admin)
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }
} 