using MergeCat.Configuration;
using MergeCat.Context;
using MergeCat.Models;
using MergeCat.Models.DTO;
using MergeCat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MergeCat.Controllers;

[ApiController]
[Authorize]
[Route("board")]
public class BoardController(
    ApiDbContext db,
    IOptions<EnvVariables> env,
    IBalanceService balanceService
) : AuthorizedControllerBase
{
    private readonly EnvVariables _env = env.Value;

    [HttpGet("get-board")]
    public async Task<IActionResult> Board()
    {
        var player = await db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return NotFound("Player not found");

        await balanceService.CollectAsync(player);

        return Ok(await BuildBoardResponse(player));
    }

    [HttpPost("merge")]
    public async Task<IActionResult> Merge([FromBody] MergeRequest request)
    {
        if (request.FromIndex is < 0 or > 11 || request.ToIndex is < 0 or > 11)
            return BadRequest(new { message = "Cell index must be between 0 and 11" });

        if (request.FromIndex == request.ToIndex)
            return BadRequest(new { message = "Cannot merge a cell with itself" });

        var fromIndex = await db.Cells.FirstOrDefaultAsync(c =>
            c.PlayerId == CurrentPlayerId && c.Index == request.FromIndex
        );

        var toIndex = await db.Cells.FirstOrDefaultAsync(c =>
            c.PlayerId == CurrentPlayerId && c.Index == request.ToIndex
        );

        if (fromIndex is null || toIndex is null)
            return NotFound(new { message = "One or both cells are empty" });

        if (fromIndex.UnitLevel == 0 || toIndex.UnitLevel == 0)
            return Conflict(new { message = "One or both cells are empty" });

        if (fromIndex.UnitLevel != toIndex.UnitLevel)
            return Conflict(new { message = "Units must be the same level to merge" });

        var player = await db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return NotFound(new { message = "User not found" });
        await balanceService.CollectAsync(player);

        var oldIncome = CalculateIncome(fromIndex.UnitLevel) + CalculateIncome(toIndex.UnitLevel);

        var mergeLog = new MergeLog
        {
            PlayerId = player.Id,
            CellA = fromIndex,
            CellB = toIndex,
            ResultingLevel = toIndex.UnitLevel + 1,
            Timestamp = DateTime.UtcNow,
        };
        await db.MergeLogs.AddAsync(mergeLog);

        fromIndex.UnitLevel = 0;
        toIndex.UnitLevel++;
        var newIncome = CalculateIncome(toIndex.UnitLevel);
        player.IncomeRate = player.IncomeRate - oldIncome + newIncome;
        await db.SaveChangesAsync();

        return Ok(await BuildBoardResponse(CurrentPlayerId));
    }

    [HttpGet("get-prices")]
    public async Task<IActionResult> Prices()
    {
        var maxLevel =
            await db
                .Cells.Where(c => c.PlayerId == CurrentPlayerId && c.UnitLevel > 0)
                .MaxAsync(c => (int?)c.UnitLevel)
            ?? 0;

        var boughtAmount = await db
            .Players.Where(p => p.Id == CurrentPlayerId)
            .Select(p => p.DailyPurchases)
            .FirstOrDefaultAsync();

        if (maxLevel == 0)
            return Ok(
                new[]
                {
                    new
                    {
                        Level = 1,
                        Price = CalculateUnitCost(1, boughtAmount),
                        Speed = CalculateIncome(1),
                    },
                }
            );

        var prices = Enumerable
            .Range(1, maxLevel)
            .Select(k => new
            {
                Level = k,
                Price = CalculateUnitCost(k, boughtAmount),
                Speed = CalculateIncome(k),
            })
            .ToList();

        return Ok(prices);
    }

    [HttpPost("buy-unit")]
    public async Task<IActionResult> BuyUnit([FromBody] BuyUnitRequest request)
    {
        const int maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (request.Level <= 0)
                    return BadRequest("Level must be >= 1");

                var cells = await db
                    .Cells.Where(c => c.PlayerId == CurrentPlayerId)
                    .OrderBy(c => c.Index)
                    .ToListAsync();

                var allowedMax = cells
                    .Where(c => c.UnitLevel > 0)
                    .Select(c => c.UnitLevel)
                    .DefaultIfEmpty(0)
                    .Max();

                if (request.Level > allowedMax)
                    return Conflict("Level not unlocked yet");

                var emptyCell = cells.FirstOrDefault(c => c.UnitLevel == 0);

                if (emptyCell is null)
                    return Conflict(new { message = "No free cells available" });

                var player = await db.Players.FindAsync(CurrentPlayerId);
                if (player is null)
                    return NotFound(new { message = "User not found" });

                await balanceService.CollectAsync(player);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                bool isNewDay = player.LastPurchaseDate < today;
                int purchases = isNewDay ? 0 : player.DailyPurchases;

                double unitCost = CalculateUnitCost(request.Level, ++purchases);
                if (player.Balance < unitCost)
                    return Conflict(new { message = "Insufficient balance" });

                player.DailyPurchases = purchases;
                player.LastPurchaseDate = today;
                emptyCell.UnitLevel = request.Level;
                player.Balance -= unitCost;
                player.IncomeRate += CalculateIncome(request.Level);

                await db.SaveChangesAsync();

                return Ok(await BuildBoardResponse(player));
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries - 1)
            {
                db.ChangeTracker.Clear();
            }
        }

        return Conflict(new { message = "Please retry" });
    }

    [HttpPost("move")]
    public async Task<IActionResult> Move([FromBody] MergeRequest request)
    {
        if (request.FromIndex is < 0 or > 11 || request.ToIndex is < 0 or > 11)
            return BadRequest(new { message = "Cell index must be between 0 and 11" });

        if (request.FromIndex == request.ToIndex)
            return BadRequest(new { message = "Cannot move a cell to itself" });

        var player = await db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return NotFound("Player not found");

        var cells = await db
            .Cells.Where(c =>
                c.PlayerId == CurrentPlayerId
                && (c.Index == request.FromIndex || c.Index == request.ToIndex)
            )
            .ToListAsync();

        var fromCell = cells.FirstOrDefault(c => c.Index == request.FromIndex);
        var toCell = cells.FirstOrDefault(c => c.Index == request.ToIndex);

        if (fromCell is null || toCell is null)
            return NotFound(new { message = "One or both cells not found" });

        if (fromCell.UnitLevel <= 0 || toCell.UnitLevel != 0)
            return Conflict(
                new { message = "Move only possible when to index doesn't contain unit" }
            );

        (fromCell.UnitLevel, toCell.UnitLevel) = (toCell.UnitLevel, fromCell.UnitLevel);
        await balanceService.CollectAsync(player);
        await db.SaveChangesAsync();

        return Ok(await BuildBoardResponse(player));
    }

    private async Task<object> BuildBoardResponse(Player player)
    {
        var cells = await db
            .Cells.Where(c => c.PlayerId == player.Id)
            .OrderBy(c => c.Index)
            .Select(c => new
            {
                c.Id,
                c.Index,
                c.UnitLevel,
                c.PlayerId,
            })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var elapsed = (now - player.LastCollectedAt).TotalSeconds;

        return new
        {
            cells,
            balance = player.Balance + player.IncomeRate * elapsed,
            player.TotalEarned,
            league = player.League.ToString(),
            incomeRate = player.IncomeRate,
            lastCollectedAt = player.LastCollectedAt,
            serverTime = now,
        };
    }

    private double CalculateIncome(int unitLevel) =>
        unitLevel == 0 ? 0 : _env.IncomeBaseRate * Math.Pow(_env.IncomeGrowthRate, unitLevel - 1);

    private double CalculateUnitCost(int level, int boughtAmount) =>
        _env.UnitBaseCost
        * Math.Pow(_env.LevelGrowthRate, level - 1)
        * Math.Pow(_env.DailyPurchaseGrowthRate, boughtAmount);
}
