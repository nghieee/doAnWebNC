using Microsoft.AspNetCore.SignalR;
using web_ban_thuoc.Models;

namespace web_ban_thuoc;

public class ChatHub : Hub
{
    public const string AdminReceiverId = "admin";

    private readonly LongChauDbContext _context;

    public ChatHub(LongChauDbContext context)
    {
        _context = context;
    }

    public static string GetConversationGroup(string customerUserId) => $"chat_{customerUserId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
        if (!string.IsNullOrEmpty(userId) && userId != AdminReceiverId)
            await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroup(userId));

        await base.OnConnectedAsync();
    }

    /// <summary>Admin mở cuộc hội thoại với khách — join group riêng.</summary>
    public async Task JoinConversation(string customerUserId)
    {
        if (string.IsNullOrEmpty(customerUserId))
            return;
        await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroup(customerUserId));
    }

    public async Task SendMessage(string senderId, string receiverId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var customerId = senderId == AdminReceiverId ? receiverId : senderId;

        try
        {
            _context.ChatMessages.Add(new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                SentAt = DateTime.Now,
                IsRead = false
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi lưu chat: {ex.Message}");
        }

        await Clients.Group(GetConversationGroup(customerId))
            .SendAsync("ReceiveMessage", senderId, message);
    }
}
