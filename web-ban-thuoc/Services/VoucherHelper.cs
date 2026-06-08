using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services;

/// <summary>
/// Logic nghiệp vụ voucher: mã gán user (UserVouchers) vs mã dùng chung (IsPublic).
/// </summary>
public static class VoucherHelper
{
    public static async Task<(Voucher? voucher, UserVoucher? userVoucher, string? error)> ResolveForApplyAsync(
        LongChauDbContext context,
        string userId,
        string code,
        decimal cartSubtotal,
        IEnumerable<CartLineViewModel>? cartLines = null)
    {
        var assigned = await context.UserVouchers
            .Include(uv => uv.Voucher)
            .FirstOrDefaultAsync(uv =>
                uv.UserId == userId &&
                uv.Voucher.Code == code &&
                !uv.IsUsed &&
                uv.Voucher.IsActive &&
                uv.Voucher.ExpiryDate >= DateTime.Now);

        Voucher? voucher = assigned?.Voucher;

        if (voucher == null)
        {
            voucher = await context.Vouchers.FirstOrDefaultAsync(v =>
                v.Code == code &&
                v.IsPublic &&
                v.IsActive &&
                v.ExpiryDate >= DateTime.Now);

            if (voucher == null)
                return (null, null, "Mã giảm giá không hợp lệ hoặc đã hết hạn!");

            if (await context.UserVouchers.AnyAsync(uv =>
                    uv.UserId == userId && uv.VoucherId == voucher.VoucherId && uv.IsUsed))
                return (null, null, "Bạn đã sử dụng mã giảm giá này rồi!");

            if (voucher.MaxUsage.HasValue && voucher.UsedCount >= voucher.MaxUsage.Value)
                return (null, null, "Voucher đã hết lượt sử dụng!");
        }

        var ruleError = await ValidatePromotionRulesAsync(context, userId, voucher, cartSubtotal, cartLines);
        if (ruleError != null)
            return (null, null, ruleError);

        return (voucher, assigned, null);
    }

    public static async Task<string?> ValidatePromotionRulesAsync(
        LongChauDbContext context,
        string userId,
        Voucher voucher,
        decimal cartSubtotal,
        IEnumerable<CartLineViewModel>? cartLines)
    {
        if (voucher.MinOrderAmount.HasValue && cartSubtotal < voucher.MinOrderAmount.Value)
            return $"Đơn hàng tối thiểu {voucher.MinOrderAmount.Value:N0}đ để dùng mã này.";

        if (!string.IsNullOrWhiteSpace(voucher.RequiredRank))
        {
            var rankInfo = await context.UserRankInfos.FindAsync(userId);
            var userRank = rankInfo?.Rank ?? LoyaltyTiers.Silver;
            if (!MeetsRankRequirement(userRank, voucher.RequiredRank))
                return $"Mã chỉ dành cho thành viên hạng {voucher.RequiredRank} trở lên.";
        }

        if (voucher.DiscountType == "Category" && voucher.CategoryId.HasValue && cartLines != null)
        {
            var hasCategory = cartLines.Any(l => l.CategoryId == voucher.CategoryId);
            if (!hasCategory)
                return "Giỏ hàng không có sản phẩm thuộc danh mục áp dụng voucher.";
        }

        return null;
    }

    public static bool MeetsRankRequirement(string userRank, string requiredRank)
    {
        var order = new Dictionary<string, int>
        {
            [LoyaltyTiers.Silver] = 1,
            [LoyaltyTiers.Gold] = 2,
            [LoyaltyTiers.Platinum] = 3
        };
        if (!order.TryGetValue(userRank, out var userLevel))
            userLevel = 1;
        if (!order.TryGetValue(requiredRank, out var requiredLevel))
            requiredLevel = 1;
        return userLevel >= requiredLevel;
    }

    public static decimal CalculateDiscount(Voucher voucher, decimal cartSubtotal, IEnumerable<CartLineViewModel> lines)
    {
        decimal discount = 0;
        if (voucher.DiscountType == "FullOrder")
        {
            if (voucher.PercentValue.HasValue)
                discount = Math.Round(cartSubtotal * voucher.PercentValue.Value / 100);
            else if (voucher.DiscountAmount.HasValue)
                discount = voucher.DiscountAmount.Value;
        }
        else if (voucher.DiscountType == "Category" && voucher.CategoryId.HasValue)
        {
            var categoryTotal = lines
                .Where(l => l.CategoryId == voucher.CategoryId)
                .Sum(l => l.Price * l.Quantity);
            if (voucher.PercentValue.HasValue)
                discount = Math.Round(categoryTotal * voucher.PercentValue.Value / 100);
            else if (voucher.DiscountAmount.HasValue)
                discount = voucher.DiscountAmount.Value;
        }

        if (discount > cartSubtotal) discount = cartSubtotal;
        return discount;
    }

    public static void MarkUsed(LongChauDbContext context, string userId, string code, int orderId, decimal discountAmount)
    {
        var userVoucher = context.UserVouchers
            .Include(uv => uv.Voucher)
            .FirstOrDefault(uv => uv.UserId == userId && uv.Voucher.Code == code && !uv.IsUsed);

        Voucher? voucher;
        if (userVoucher != null)
        {
            userVoucher.IsUsed = true;
            userVoucher.UsedDate = DateTime.Now;
            userVoucher.Voucher.UsedCount++;
            voucher = userVoucher.Voucher;
        }
        else
        {
            voucher = context.Vouchers.FirstOrDefault(v => v.Code == code && v.IsPublic);
            if (voucher == null)
                return;

            voucher.UsedCount++;
            var existing = context.UserVouchers
                .FirstOrDefault(uv => uv.UserId == userId && uv.VoucherId == voucher.VoucherId);
            if (existing != null)
            {
                existing.IsUsed = true;
                existing.UsedDate = DateTime.Now;
            }
            else
            {
                context.UserVouchers.Add(new UserVoucher
                {
                    UserId = userId,
                    VoucherId = voucher.VoucherId,
                    IsUsed = true,
                    UsedDate = DateTime.Now
                });
            }
        }

        if (!context.VoucherRedemptions.Any(r => r.VoucherId == voucher.VoucherId && r.OrderId == orderId))
        {
            context.VoucherRedemptions.Add(new VoucherRedemption
            {
                VoucherId = voucher.VoucherId,
                UserId = userId,
                OrderId = orderId,
                DiscountAmount = discountAmount,
                RedeemedAt = DateTime.Now
            });
        }
    }

    public static void RevertUsage(LongChauDbContext context, string? userId, string code, int? orderId = null)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        var userVoucher = context.UserVouchers
            .Include(uv => uv.Voucher)
            .FirstOrDefault(uv => uv.UserId == userId && uv.Voucher.Code == code && uv.IsUsed);

        if (userVoucher != null)
        {
            userVoucher.IsUsed = false;
            userVoucher.UsedDate = null;
            if (userVoucher.Voucher.UsedCount > 0)
                userVoucher.Voucher.UsedCount--;
        }
        else
        {
            var publicVoucher = context.Vouchers.FirstOrDefault(v => v.Code == code && v.IsPublic);
            if (publicVoucher != null && publicVoucher.UsedCount > 0)
                publicVoucher.UsedCount--;
        }

        if (orderId.HasValue)
        {
            var redemption = context.VoucherRedemptions
                .FirstOrDefault(r => r.OrderId == orderId && !r.IsReverted);
            if (redemption != null)
                redemption.IsReverted = true;
        }
    }
}
