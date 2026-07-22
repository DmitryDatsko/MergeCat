using System.Collections.Concurrent;
using System.Threading.Channels;
using MergeCat.Models.DTO;

namespace MergeCat.Services;

public class PurchaseNotificationHub
{
    private readonly ConcurrentDictionary<
        string,
        ConcurrentBag<Channel<ContractEventResponse>>
    > _subscribers = new();

    public ChannelReader<ContractEventResponse> Subscribe(
        string walletAddress,
        CancellationToken ct
    )
    {
        var channel = Channel.CreateUnbounded<ContractEventResponse>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );

        var bag = _subscribers.GetOrAdd(walletAddress, _ => []);
        bag.Add(channel);

        ct.Register(() => channel.Writer.TryComplete());

        return channel.Reader;
    }

    public void Publish(string walletAddress, ContractEventResponse payload)
    {
        if (!_subscribers.TryGetValue(walletAddress, out var bag))
            return;

        foreach (var channel in bag)
            channel.Writer.TryWrite(payload);
    }
}
