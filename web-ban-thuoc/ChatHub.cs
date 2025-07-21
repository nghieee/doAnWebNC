using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace web_ban_thuoc
{
    public class ChatHub : Hub
    {
        // Gửi tin nhắn từ user tới admin hoặc ngược lại
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            // Gửi tin nhắn tới người nhận (theo connectionId hoặc group)
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
        }
    }
} 