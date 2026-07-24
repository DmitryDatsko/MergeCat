using MergeCat.Models;

namespace MergeCat.Services;

public class BalanceService : IBalanceService
{
    private static readonly TimeSpan OfflineCap = TimeSpan.FromHours(4);

    public double CalculateEarningsForRange(DateTime rangeStart, DateTime rangeEnd, Player player)
    {
        if (rangeEnd <= rangeStart)
            return 0;

        var totalDuration = rangeEnd - rangeStart;

        if (player.BoostExpiresAt is null || player.BoostExpiresAt <= rangeStart)
            return totalDuration.TotalSeconds * player.IncomeRate;

        if (player.BoostExpiresAt >= rangeEnd)
            return totalDuration.TotalSeconds * player.IncomeRate * 2;

        var boostedDuration = player.BoostExpiresAt.Value - rangeStart;
        var normalDuration = rangeEnd - player.BoostExpiresAt.Value;

        return boostedDuration.TotalSeconds * player.IncomeRate * 2
            + normalDuration.TotalSeconds * player.IncomeRate;
    }

    public void CollectEarning(Player player)
    {
        var (claimedGold, _) = PreviewEarnings(player);
        var now = DateTime.UtcNow;

        player.Balance += claimedGold;
        player.TotalEarned += claimedGold;
        player.League = LeagueExtensions.FromTotalEarned(player.TotalEarned);

        if (player.BoostExpiresAt.HasValue && player.BoostExpiresAt <= now)
        {
            player.BoostExpiresAt = null;
            player.BoostActivatedAt = null;
        }

        player.LastCollectedAt = now;
    }

    public void CollectWithBonus(Player player)
    {
        var (claimedGold, bonusGold) = PreviewEarnings(player);
        var total = claimedGold + bonusGold;
        var now = DateTime.UtcNow;

        if (player.BoostExpiresAt.HasValue && player.BoostExpiresAt <= now)
        {
            player.BoostExpiresAt = null;
            player.BoostActivatedAt = null;
        }

        player.Balance += total;
        player.TotalEarned += total;
        player.LastCollectedAt = now;
        player.League = LeagueExtensions.FromTotalEarned(player.TotalEarned);
    }

    public (double ClaimableGold, double BonusGold) PreviewEarnings(Player player)
    {
        var now = DateTime.UtcNow;
        var cappedPoint = player.LastCollectedAt + OfflineCap;
        if (cappedPoint > now)
            cappedPoint = now;

        var claimedGold = CalculateEarningsForRange(player.LastCollectedAt, cappedPoint, player);

        var bonusGold = now > cappedPoint ? CalculateEarningsForRange(cappedPoint, now, player) : 0;

        return (claimedGold, bonusGold);
    }
}
