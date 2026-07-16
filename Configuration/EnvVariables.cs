namespace MergeCat.Configuration;

public class EnvVariables
{
    public string JwtTokenSecret { get; set; } = string.Empty;
    public string CookieName { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int BoardSize { get; set; }
    public double StartingBalance { get; set; }
    public double UnitBaseCost { get; set; }
    public double LevelGrowthRate { get; set; }
    public double DailyPurchaseGrowthRate { get; set; }
    public double IncomeBaseRate { get; set; }
    public double IncomeGrowthRate { get; set; }
}
