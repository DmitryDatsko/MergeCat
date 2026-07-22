using MergeCat.Context;
using MergeCat.Models.DTO;
using MergeCat.Options;
using MergeCat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MergeCat.Controllers;

[ApiController]
[Authorize]
[Route("player")]
public class PlayerController(
    AppDbContext db,
    IBalanceService balanceService,
    IOptions<GameOptions> gameOptions
) : AuthorizedControllerBase
{
    private readonly GameOptions _gameOptions = gameOptions.Value;

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var player = await db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return NotFound(new { message = "User not found" });

        var league = LeagueExtensions.FromTotalEarned(player.TotalEarned);
        var (claimable, bonus) = balanceService.PreviewEarnings(player);

        return Ok(
            new ProfileResponse(
                player.Balance,
                player.IncomeRate,
                player.TotalEarned,
                player.LastCollectedAt,
                league.ToString(),
                claimable,
                bonus,
                bonus >= _gameOptions.MinBonusThresholdGold,
                player.BoostExpiresAt,
                player.BoostActivatedAt
            )
        );
    }

    [HttpPost("collect")]
    public async Task<IActionResult> Collect()
    {
        var player = await db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return NotFound("User not found");

        balanceService.CollectEarning(player);
        var league = LeagueExtensions.FromTotalEarned(player.TotalEarned);

        await db.SaveChangesAsync();

        return Ok(
            new CollectResponse(
                player.Balance,
                player.IncomeRate,
                player.TotalEarned,
                player.LastCollectedAt,
                league.ToString(),
                player.BoostExpiresAt,
                player.BoostActivatedAt
            )
        );
    }
}
