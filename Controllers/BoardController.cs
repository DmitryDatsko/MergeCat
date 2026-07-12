using MergeCat.Configuration;
using MergeCat.Context;
using MergeCat.Models.DTO;
using MergeCat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MergeCat.Controllers;

[ApiController]
[Route("board")]
[Authorize]
public class BoardController(
    ApiDbContext db,
    IOptions<EnvVariables> env,
    IBalanceService balanceService
) : AuthorizedControllerBase
{
    private readonly EnvVariables _env = env.Value;

    [HttpGet("board")]
    public async Task<IActionResult> Board()
    {
        return Ok(await BuildBoardResponse(CurrentPlayerId));
    }

    [HttpPost("merge")]
    public async Task<IActionResult> Merge([FromBody] MergeRequest request)
    {
        if (request.CellAIndex is < 0 or > 11 || request.CellBIndex is < 0 or > 11)
            return BadRequest(new { message = "Cell index must be between 0 and 11" });

        if (request.CellAIndex == request.CellBIndex)
            return BadRequest(new { message = "Cannot merge a cell with itselft" });

        var cellA = await db.Cells.FirstOrDefaultAsync(c =>
            c.PlayerId == CurrentPlayerId && c.Index == request.CellAIndex
        );

        var cellB = await db.Cells.FirstOrDefaultAsync(c =>
            c.PlayerId == CurrentPlayerId && c.Index == request.CellBIndex
        );

        if (cellA is null || cellB is null)
            return NotFound(new { message = "One or both cells are empty" });

        if (cellA.UnitLevel != cellB.UnitLevel)
            return Conflict(new { message = "Units must be the same level to merge" });

        var player = await db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return NotFound(new { message = "User not found" });
        await balanceService.CollectAsync(player);

        var oldIncome = CalculateIncome(cellA.UnitLevel) + CalculateIncome(cellB.UnitLevel);

        cellA.UnitLevel++;
        cellB.UnitLevel = 0;
        var newIncome = CalculateIncome(cellA.UnitLevel);
        player.IncomeRate = player.IncomeRate - oldIncome + newIncome;
        await db.SaveChangesAsync();

        return Ok(await BuildBoardResponse(CurrentPlayerId));
    }

    [HttpPost("buy-unit")]
    public async Task<IActionResult> BuyUnit()
    {
        var emptyCell = await db
            .Cells.Where(c => c.PlayerId == CurrentPlayerId && c.UnitLevel == 0)
            .OrderBy(c => c.Index)
            .FirstOrDefaultAsync();

        if (emptyCell is null)
            return Conflict(new { message = "No free cells available" });

        var player = await db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return NotFound(new { message = "User not found" });

        await balanceService.CollectAsync(player);

        if (player.Balance < _env.UnitBaseCost)
            return Conflict(new { message = "Insufficient balance" });

        emptyCell.UnitLevel = 1;
        player.Balance -= _env.UnitBaseCost;
        player.IncomeRate += CalculateIncome(1);

        await db.SaveChangesAsync();

        return Ok(await BuildBoardResponse(CurrentPlayerId));
    }

    private async Task<object> BuildBoardResponse(Guid playerId)
    {
        var player = await db.Players.FindAsync(playerId);
        var cells = await db
            .Cells.Where(c => c.PlayerId == playerId)
            .OrderBy(c => c.Index)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var elapsed = (now - player!.LastCollectedAt).TotalSeconds;

        return new
        {
            cells,
            balance = player.Balance + player.IncomeRate * elapsed,
            incomeRate = player.IncomeRate,
            lastCollectedAt = player.LastCollectedAt,
            serverTime = now,
        };
    }

    private double CalculateIncome(int unitLevel) =>
        unitLevel == 0 ? 0 : _env.IncomeBaseRate * Math.Pow(_env.IncomeGrowthRate, unitLevel - 1);
}
