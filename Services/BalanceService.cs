using MergeCat.Models;

namespace MergeCat.Services;

public class BalanceService : IBalanceService
{
    const int MaxSecondsOffline = 14400;

    public Task CollectAsync(Player player)
    {
        var now = DateTime.UtcNow;
        var elapsed = Math.Min((now - player.LastCollectedAt).TotalSeconds, MaxSecondsOffline);
        var earned = player.IncomeRate * elapsed;

        player.Balance += earned;
        player.TotalEarned += earned;
        player.LastCollectedAt = now;

        return Task.CompletedTask;
    }
}
