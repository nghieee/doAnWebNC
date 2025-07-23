using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using web_ban_thuoc.Models;
using System;

namespace web_ban_thuoc
{
    public class ChatHub : Hub
    {
        private readonly LongChauDbContext _context;
        public ChatHub(LongChauDbContext context)
        {
            _context = context;
        }
        // Gửi tin nhắn từ user tới admin hoặc ngược lại
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            try
            {
                // Lưu tin nhắn vào DB
                var chatMsg = new ChatMessage
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Message = message,
                    SentAt = DateTime.Now,
                    IsRead = false
                };
                _context.ChatMessages.Add(chatMsg);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lưu tin nhắn: sender={senderId}, receiver={receiverId}, message={message}, error={ex}");
            }
            // Gửi tin nhắn tới tất cả client (admin và user)
            await Clients.All.SendAsync("ReceiveMessage", senderId, message);
        }
    }
} 