using Nethereum.ABI.FunctionEncoding.Attributes;

namespace MergeCat.Models.ContractDTO;

[Event("Purchased")]
public class PurchasedEvent : IEventDTO
{
    [Parameter("address", "buyer", 1, true)]
    public string BuyerAddress { get; set; } = string.Empty;

    [Parameter("address", "token", 2, true)]
    public string TokenAddress { get; set; } = string.Empty;

    [Parameter("uint8", "eventType", 3, true)]
    public byte Event { get; set; }

    [Parameter("uint256", "paid", 4, false)]
    public ulong Paid { get; set; }

    [Parameter("uint256", "timestamp", 5, false)]
    public DateTime Timestamp { get; set; }

    public EventType EventEnum => (EventType)Event;
}
