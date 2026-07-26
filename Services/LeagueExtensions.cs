using MergeCat.Models;

namespace MergeCat.Services;

public static class LeagueExtensions
{
    public static double Threshold(this League league) =>
        league switch
        {
            League.Bronze => 0,
            League.Silver => 500_000,
            League.Gold => 1_000_000,
            League.Emerald => 10_000_000,
            League.Sapphire => 100_000_000,
            League.Amethyst => 1_000_000_000,
            _ => throw new NotImplementedException(),
        };

    public static League FromTotalEarned(double totalEarned)
    {
        var leagues = Enum.GetValues<League>().OrderByDescending(l => l.Threshold());
        foreach (var league in leagues)
            if (totalEarned >= league.Threshold())
                return league;

        return League.Bronze;
    }
}
