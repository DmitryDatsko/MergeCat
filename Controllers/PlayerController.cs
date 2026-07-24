using MergeCat.Context;
using MergeCat.Models;
using MergeCat.Models.DTO;
using MergeCat.Options;
using MergeCat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MergeCat.Controllers;

[ApiController]
[Authorize]
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
        var (claimable, bonus) = balanceService.PreviewEarnings(player);

        await db.SaveChangesAsync();

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

    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard(
        [FromQuery] League league,
        [FromQuery] int page,
        [FromQuery] int pageSize = 20
    )
    {
        if (page <= 0 || pageSize <= 0)
            return BadRequest(new { message = "Page number must be greater than zero" });

        pageSize = Math.Min(pageSize, 100);

        var players = await db
            .Players.Where(p => p.League == league)
            .OrderByDescending(p => p.TotalEarned)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(p => new LeaderboardInstance(p.WalletAddress, p.TotalEarned, p.League))
            .ToListAsync();

        bool hasMore = players.Count > pageSize;
        if (hasMore)
            players.RemoveAt(players.Count - 1);

        return Ok(new LeaderboardResponse(players, league.Threshold(), hasMore));
    }
}
