namespace web_ban_thuoc.Models;

public static class LoyaltyPointTypes
{
    public const string Earn = "Earn";
    public const string Adjust = "Adjust";
    public const string Redeem = "Redeem";
}

public class LoyaltyPointTransaction
{
    public int LoyaltyPointTransactionId { get; set; }

    public string UserId { get; set; } = null!;

    public int Points { get; set; }

    /// <summary>Earn | Adjust | Redeem</summary>
    public string TransactionType { get; set; } = LoyaltyPointTypes.Earn;

    public int? OrderId { get; set; }

    public int? LoyaltyRewardId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Order? Order { get; set; }
}
