namespace MergeCat.Options;

public class GameOptions
{
    public int BoardSize { get; init; }

    public int StartingBalance { get; init; }

    public double UnitBaseCost { get; init; }
    public double LevelGrowthRate { get; init; }

    public double DailyPurchaseGrowthRate { get; init; }

    public double IncomeBaseRate { get; init; }
    public double IncomeGrowthRate { get; init; }

    public double MinBonusThresholdGold { get; init; }
}
