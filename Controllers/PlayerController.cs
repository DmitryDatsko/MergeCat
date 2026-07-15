using MergeCat.Context;
using MergeCat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MergeCat.Controllers;

[ApiController]
[Authorize]
[Route("player")]
public class PlayerController(ApiDbContext db, IBalanceService balanceService)
    : AuthorizedControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var player = await db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return NotFound(new { message = "User not found" });

        await balanceService.CollectAsync(player);
        var league = LeagueExtensions.FromTotalEarned(player.TotalEarned);

        return Ok(
            new
            {
                player.Balance,
                player.IncomeRate,
                player.TotalEarned,
                player.LastCollectedAt,
                league,
            }
        );
    }
}
