using System.IdentityModel.Tokens.Jwt;
using MergeCat.Context;
using MergeCat.Models;
using MergeCat.Options;
using MergeCat.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace MergeCat.Filters;

public class BonusGuardFilter(
    AppDbContext db,
    IBalanceService balanceService,
    IOptions<GameOptions> gameOptions
) : IAsyncActionFilter
{
    private readonly GameOptions _gameOptions = gameOptions.Value;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        var subClaim = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (subClaim is null || !Guid.TryParse(subClaim, out var playerId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var player = await db.Players.FindAsync(playerId);
        if (player is null)
        {
            context.Result = new NotFoundObjectResult(new { message = "Player not found" });
            return;
        }

        var (_, bonus) = balanceService.PreviewEarnings(player);
        if (bonus >= _gameOptions.MinBonusThresholdGold)
        {
            context.Result = new ConflictObjectResult(
                new { message = "Pending bonus must be claimed", bonus }
            );
            return;
        }

        context.HttpContext.Items[nameof(Player)] = player;

        await next();
    }
}
