namespace MergeCat.Models;

public class Player
{
    public Guid Id { get; set; }
    public EthereumAddress WalletAddress { get; set; }
    public double Balance { get; set; }
    public double TotalEarned { get; set; }
    public double IncomeRate { get; set; }
    public DateTime LastCollectedAt { get; set; }

    public ICollection<Cell> Cells { get; } = [];
}
