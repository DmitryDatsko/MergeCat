using System.ComponentModel.DataAnnotations;

namespace MergeCat.Models;

public class Player
{
    public Guid Id { get; set; }
    public required EthereumAddress WalletAddress { get; set; }
    public double Balance { get; set; }
    public double TotalEarned { get; set; }
    public double IncomeRate { get; set; }
    public int DailyPurchases { get; set; }
    public League League { get; set; }
    public DateTime LastCollectedAt { get; set; }
    public DateTime? BoostActivatedAt { get; set; } = null;
    public DateTime? BoostExpiresAt { get; set; } = null;
    public DateOnly LastPurchaseDate { get; set; }

    [Timestamp]
    public uint Version { get; set; }

    public ICollection<Cell> Cells { get; } = [];
}
