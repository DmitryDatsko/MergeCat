using MergeCat.Models;

namespace MergeCat.Services;

public class BalanceService : IBalanceService
{
    public Task CollectAsync(Player player)
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - player.LastCollectedAt).TotalSeconds;
        var earned = player.IncomeRate * elapsed;

        player.Balance += elapsed;
        player.TotalEarned += earned;
        player.LastCollectedAt = now;

        return Task.CompletedTask;
    }
}
