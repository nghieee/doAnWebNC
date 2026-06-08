using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web_ban_thuoc.Services;

namespace web_ban_thuoc.Controllers;

[Authorize]
[Route("api/loyalty")]
public class LoyaltyController : Controller
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly UserManager<IdentityUser> _userManager;

    public LoyaltyController(ILoyaltyService loyaltyService, UserManager<IdentityUser> userManager)
    {
        _loyaltyService = loyaltyService;
        _userManager = userManager;
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem([FromBody] RedeemRequest request)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var (success, message, voucherCode) = await _loyaltyService.RedeemRewardAsync(userId, request.RewardId);
        if (!success)
            return Json(new { success = false, message });

        var points = await _loyaltyService.GetPointsAsync(userId);
        return Json(new { success = true, message, voucherCode, remainingPoints = points });
    }

    public class RedeemRequest
    {
        public int RewardId { get; set; }
    }
}
