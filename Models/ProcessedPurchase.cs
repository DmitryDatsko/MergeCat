using System.Numerics;

namespace MergeCat.Models;

public class ProcessedPurchase
{
    public int Id { get; set; }
    public string TxHash { get; set; } = string.Empty;
    public int LogIndex { get; set; }

    public required EthereumAddress BuyerAddress { get; set; }
    public required EthereumAddress TokenAddress { get; set; }
    public EventType EventType { get; set; }
    public BigInteger PaidAmount { get; set; }
    public DateTime BlockTimestamp { get; set; }
    public DateTime ProcessedAt { get; set; }
}
