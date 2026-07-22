using System.Numerics;

namespace MergeCat.Models.DTO;

public record ContractEventResponse
{
    public required string BuyerAddress { get; init; }
    public required EventType EventType { get; init; }
    public required string TxHash { get; init; }
    public required BigInteger PaidAmount { get; init; }
    public required DateTime BlockTimestamp { get; init; }
}
