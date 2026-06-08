using System.Text;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services;

public interface ILoyaltyService
{
    Task EarnPointsForDeliveredOrderAsync(int orderId);
    Task<int> GetPointsAsync(string userId);
    Task<List<LoyaltyRewardViewModel>> GetRewardsForUserAsync(string userId, string rank, int currentPoints);
    Task<List<LoyaltyRedeemHistoryViewModel>> GetRedeemHistoryAsync(string userId, int take = 10);
    Task<(bool success, string message, string? voucherCode)> RedeemRewardAsync(string userId, int rewardId);
}

public class LoyaltyService : ILoyaltyService
{
    private readonly LongChauDbContext _context;

    public LoyaltyService(LongChauDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetPointsAsync(string userId)
    {
        var info = await _context.UserRankInfos.FindAsync(userId);
        return info?.LoyaltyPoints ?? 0;
    }

    public async Task EarnPointsForDeliveredOrderAsync(int orderId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null || string.IsNullOrEmpty(order.UserId))
            return;

        var alreadyEarned = await _context.LoyaltyPointTransactions.AnyAsync(t =>
            t.OrderId == orderId && t.TransactionType == LoyaltyPointTypes.Earn);
        if (alreadyEarned)
            return;

        var info = await EnsureRankInfoAsync(order.UserId);

        var amount = order.TotalAmount ?? 0;
        var points = LoyaltyTiers.CalculateEarnedPoints(amount);
        if (points <= 0)
            return;

        info.LoyaltyPoints += points;
        _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
        {
            UserId = order.UserId,
            Points = points,
            TransactionType = LoyaltyPointTypes.Earn,
            OrderId = orderId,
            Description = $"Tích điểm đơn #{orderId}"
        });

        await _context.SaveChangesAsync();
    }

    public async Task<List<LoyaltyRewardViewModel>> GetRewardsForUserAsync(string userId, string rank, int currentPoints)
    {
        var rewards = await _context.LoyaltyRewards
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.PointsCost)
            .ToListAsync();

        var claimCounts = await _context.LoyaltyPointTransactions
            .Where(t => t.UserId == userId && t.TransactionType == LoyaltyPointTypes.Redeem && t.LoyaltyRewardId != null)
            .GroupBy(t => t.LoyaltyRewardId)
            .Select(g => new { RewardId = g.Key!.Value, Count = g.Count() })
            .ToDictionaryAsync(x => x.RewardId, x => x.Count);

        return rewards.Select(r =>
        {
            var vm = new LoyaltyRewardViewModel
            {
                LoyaltyRewardId = r.LoyaltyRewardId,
                Title = r.Title,
                Description = r.Description,
                PointsCost = r.PointsCost,
                RewardType = r.RewardType,
                PercentValue = r.PercentValue,
                DiscountAmount = r.DiscountAmount,
                RequiredRank = r.RequiredRank,
                StockRemaining = r.StockRemaining,
                RewardLabel = BuildRewardLabel(r)
            };

            var (can, reason) = CanRedeem(r, rank, currentPoints, claimCounts.GetValueOrDefault(r.LoyaltyRewardId));
            vm.CanRedeem = can;
            vm.DisabledReason = reason;
            return vm;
        }).ToList();
    }

    public async Task<List<LoyaltyRedeemHistoryViewModel>> GetRedeemHistoryAsync(string userId, int take = 10)
    {
        return await _context.LoyaltyPointTransactions
            .Where(t => t.UserId == userId && t.TransactionType == LoyaltyPointTypes.Redeem)
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .Select(t => new LoyaltyRedeemHistoryViewModel
            {
                CreatedAt = t.CreatedAt,
                Title = t.Description ?? "Đổi quà",
                Points = t.Points
            })
            .ToListAsync();
    }

    public async Task<(bool success, string message, string? voucherCode)> RedeemRewardAsync(string userId, int rewardId)
    {
        var reward = await _context.LoyaltyRewards.FirstOrDefaultAsync(r => r.LoyaltyRewardId == rewardId);
        if (reward == null || !reward.IsActive)
            return (false, "Quà tặng không tồn tại hoặc đã ngừng phát hành.", null);

        var info = await EnsureRankInfoAsync(userId);
        var claimCount = await _context.LoyaltyPointTransactions.CountAsync(t =>
            t.UserId == userId && t.LoyaltyRewardId == rewardId && t.TransactionType == LoyaltyPointTypes.Redeem);

        var (can, reason) = CanRedeem(reward, info.Rank, info.LoyaltyPoints, claimCount);
        if (!can)
            return (false, reason ?? "Không thể đổi quà.", null);

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var code = await GenerateUniqueVoucherCodeAsync($"DOI{reward.LoyaltyRewardId}");
            var voucher = new Voucher
            {
                Code = code,
                Description = $"Voucher đổi điểm: {reward.Title}",
                DiscountType = "FullOrder",
                PercentValue = reward.RewardType == LoyaltyRewardTypes.VoucherPercent ? reward.PercentValue : null,
                DiscountAmount = reward.RewardType == LoyaltyRewardTypes.VoucherFixed ? reward.DiscountAmount : null,
                ExpiryDate = DateTime.Now.Date.AddDays(Math.Max(1, reward.ExpiryDays)),
                Detail = $"Đổi từ {reward.PointsCost:N0} điểm thưởng. HSD {reward.ExpiryDays} ngày.",
                IsActive = true,
                IsPublic = false,
                MinOrderAmount = reward.MinOrderAmount
            };
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            _context.UserVouchers.Add(new UserVoucher
            {
                UserId = userId,
                VoucherId = voucher.VoucherId,
                IsUsed = false,
                IsNew = true
            });

            info.LoyaltyPoints -= reward.PointsCost;
            if (reward.StockRemaining.HasValue)
                reward.StockRemaining = Math.Max(0, reward.StockRemaining.Value - 1);

            _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
            {
                UserId = userId,
                Points = reward.PointsCost,
                TransactionType = LoyaltyPointTypes.Redeem,
                LoyaltyRewardId = reward.LoyaltyRewardId,
                Description = $"Đổi quà: {reward.Title} → mã {code}"
            });

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, $"Đổi quà thành công! Mã voucher: {code}", code);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static (bool can, string? reason) CanRedeem(LoyaltyReward r, string userRank, int points, int userClaimCount)
    {
        if (!r.IsActive)
            return (false, "Quà đã ngừng phát hành.");
        if (r.StockRemaining.HasValue && r.StockRemaining <= 0)
            return (false, "Quà đã hết.");
        if (points < r.PointsCost)
            return (false, $"Cần thêm {(r.PointsCost - points):N0} điểm.");
        if (!string.IsNullOrWhiteSpace(r.RequiredRank) && !VoucherHelper.MeetsRankRequirement(userRank, r.RequiredRank))
            return (false, $"Chỉ dành cho hạng {r.RequiredRank} trở lên.");
        if (r.MaxPerUser.HasValue && userClaimCount >= r.MaxPerUser.Value)
            return (false, "Bạn đã đổi quà này đủ số lần cho phép.");
        return (true, null);
    }

    private static string BuildRewardLabel(LoyaltyReward r) => r.RewardType switch
    {
        LoyaltyRewardTypes.VoucherPercent when r.PercentValue.HasValue => $"Giảm {r.PercentValue.Value:N0}% đơn hàng",
        LoyaltyRewardTypes.VoucherFixed when r.DiscountAmount.HasValue => $"Giảm {r.DiscountAmount.Value:N0}đ",
        _ => "Voucher ưu đãi"
    };

    private async Task<UserRankInfo> EnsureRankInfoAsync(string userId)
    {
        var info = await _context.UserRankInfos.FindAsync(userId);
        if (info != null)
            return info;

        info = new UserRankInfo { UserId = userId };
        _context.UserRankInfos.Add(info);
        await _context.SaveChangesAsync();
        return info;
    }

    private async Task<string> GenerateUniqueVoucherCodeAsync(string prefix)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        string code;
        do
        {
            var sb = new StringBuilder(prefix);
            for (int i = 0; i < 6; i++)
                sb.Append(chars[random.Next(chars.Length)]);
            code = sb.ToString();
        } while (await _context.Vouchers.AnyAsync(v => v.Code == code));
        return code;
    }
}
