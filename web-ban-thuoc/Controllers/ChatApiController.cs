using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers;

[Authorize]
[Route("api/chat")]
public class ChatApiController : Controller
{
    private readonly LongChauDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ChatApiController(LongChauDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("history")]
    public async Task<IActionResult> History()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var adminId = ChatHub.AdminReceiverId;
        var messages = await _context.ChatMessages
            .Where(m =>
                (m.SenderId == userId && m.ReceiverId == adminId) ||
                (m.SenderId == adminId && m.ReceiverId == userId))
            .OrderBy(m => m.SentAt)
            .Select(m => new { m.SenderId, m.Message, m.SentAt })
            .ToListAsync();

        var unread = await _context.ChatMessages
            .Where(m => m.SenderId == adminId && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();
        foreach (var msg in unread)
            msg.IsRead = true;
        if (unread.Count > 0)
            await _context.SaveChangesAsync();

        return Json(messages);
    }
}
