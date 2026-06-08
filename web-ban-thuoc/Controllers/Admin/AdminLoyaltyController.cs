using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Controllers.Admin;

[Authorize(Roles = StaffRoles.Admin)]
[Route("AdminLoyalty")]
public class AdminLoyaltyController : Controller
{
    private readonly LongChauDbContext _context;

    public AdminLoyaltyController(LongChauDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var members = await _context.UserRankInfos
            .OrderByDescending(r => r.LoyaltyPoints)
            .Take(50)
            .ToListAsync();

        var userIds = members.Select(m => m.UserId).ToList();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? u.Id);

        ViewBag.Users = users;
        ViewBag.Tiers = LoyaltyTiers.Definitions;
        ViewBag.RecentTransactions = await _context.LoyaltyPointTransactions
            .OrderByDescending(t => t.CreatedAt)
            .Take(30)
            .ToListAsync();

        return View("~/Views/Admin/Loyalty/Index.cshtml", members);
    }

    [HttpGet("Rewards")]
    public async Task<IActionResult> Rewards()
    {
        var rewards = await _context.LoyaltyRewards
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.PointsCost)
            .ToListAsync();
        return View("~/Views/Admin/Loyalty/Rewards.cshtml", rewards);
    }

    [HttpPost("SaveReward")]
    public async Task<IActionResult> SaveReward([FromForm] LoyaltyRewardFormModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title) || model.PointsCost <= 0)
            return Json(new { success = false, message = "Vui lòng nhập tên quà và điểm đổi (> 0)." });

        if (model.RewardType == LoyaltyRewardTypes.VoucherPercent && (!model.PercentValue.HasValue || model.PercentValue <= 0))
            return Json(new { success = false, message = "Nhập % giảm hợp lệ." });
        if (model.RewardType == LoyaltyRewardTypes.VoucherFixed && (!model.DiscountAmount.HasValue || model.DiscountAmount <= 0))
            return Json(new { success = false, message = "Nhập số tiền giảm hợp lệ." });

        LoyaltyReward reward;
        if (model.LoyaltyRewardId > 0)
        {
            reward = await _context.LoyaltyRewards.FindAsync(model.LoyaltyRewardId)
                ?? throw new InvalidOperationException("Không tìm thấy quà.");
        }
        else
        {
            reward = new LoyaltyReward();
            _context.LoyaltyRewards.Add(reward);
        }

        reward.Title = model.Title.Trim();
        reward.Description = model.Description?.Trim();
        reward.PointsCost = model.PointsCost;
        reward.RewardType = model.RewardType;
        reward.PercentValue = model.RewardType == LoyaltyRewardTypes.VoucherPercent ? model.PercentValue : null;
        reward.DiscountAmount = model.RewardType == LoyaltyRewardTypes.VoucherFixed ? model.DiscountAmount : null;
        reward.ExpiryDays = Math.Max(1, model.ExpiryDays);
        reward.MinOrderAmount = model.MinOrderAmount;
        reward.RequiredRank = string.IsNullOrWhiteSpace(model.RequiredRank) ? null : model.RequiredRank;
        reward.StockRemaining = model.StockRemaining;
        reward.MaxPerUser = model.MaxPerUser;
        reward.IsActive = model.IsActive;
        reward.SortOrder = model.SortOrder;

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost("ToggleReward")]
    public async Task<IActionResult> ToggleReward(int id)
    {
        var reward = await _context.LoyaltyRewards.FindAsync(id);
        if (reward == null)
            return Json(new { success = false, message = "Không tìm thấy quà." });

        reward.IsActive = !reward.IsActive;
        await _context.SaveChangesAsync();
        return Json(new { success = true, isActive = reward.IsActive });
    }

    [HttpPost("DeleteReward")]
    public async Task<IActionResult> DeleteReward(int id)
    {
        var reward = await _context.LoyaltyRewards.FindAsync(id);
        if (reward == null)
            return Json(new { success = false, message = "Không tìm thấy quà." });

        var hasRedemptions = await _context.LoyaltyPointTransactions
            .AnyAsync(t => t.LoyaltyRewardId == id && t.TransactionType == LoyaltyPointTypes.Redeem);
        if (hasRedemptions)
        {
            reward.IsActive = false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Quà đã có lượt đổi — đã tắt thay vì xóa." });
        }

        _context.LoyaltyRewards.Remove(reward);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
}

public class LoyaltyRewardFormModel
{
    public int LoyaltyRewardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PointsCost { get; set; }
    public string RewardType { get; set; } = LoyaltyRewardTypes.VoucherFixed;
    public decimal? PercentValue { get; set; }
    public decimal? DiscountAmount { get; set; }
    public int ExpiryDays { get; set; } = 30;
    public decimal? MinOrderAmount { get; set; }
    public string? RequiredRank { get; set; }
    public int? StockRemaining { get; set; }
    public int? MaxPerUser { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
