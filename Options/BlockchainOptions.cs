namespace MergeCat.Options;

public class BlockchainOptions
{
    public string ContractAddress { get; init; } = string.Empty;
    public int RequiredConfirmation { get; init; }
    public string RpcUrl { get; init; } = string.Empty;
    public ulong MaxBlockRangePerQuery { get; init; }
    public int ContractCreationBlock { get; init; }
}
