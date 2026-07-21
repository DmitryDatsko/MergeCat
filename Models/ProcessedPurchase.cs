namespace MergeCat.Models;

public class ProcessedPurchase
{
    public int Id { get; set; }
    public string TxHash { get; set; } = string.Empty;
    public int LogIndex { get; set; }

    public EthereumAddress BuyerAddress { get; set; }
    public EventType EventType { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime BlockTimestamp { get; set; }
    public DateTime ProcessedAt { get; set; }
}
