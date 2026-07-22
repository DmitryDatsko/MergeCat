using MergeCat.Context;
using MergeCat.Models;
using MergeCat.Models.ContractDTO;
using MergeCat.Models.DTO;
using MergeCat.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace MergeCat.Services;

public class OnChainPurchaseIndexer(
    IServiceScopeFactory scopeFactory,
    Web3 web3,
    IOptions<BlockchainOptions> options,
    ILogger<OnChainPurchaseIndexer> logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly Web3 _web3 = web3;
    private readonly ILogger<OnChainPurchaseIndexer> _logger = logger;
    private readonly BlockchainOptions _options = options.Value;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNewEventAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process on-chain events");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessNewEventAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var balanceService = scope.ServiceProvider.GetRequiredService<IBalanceService>();
        var notificationHub = scope.ServiceProvider.GetRequiredService<PurchaseNotificationHub>();
        var state = await db.IndexerStates.FirstOrDefaultAsync(ct);

        var latestBlock = (await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;
        var safeBlock = latestBlock - _options.RequiredConfirmation;

        if (safeBlock <= state!.LastProcessedBlock)
            return;

        ulong fromBlock = state.LastProcessedBlock + 1;
        var toBlock = Math.Min(fromBlock + _options.MaxBlockRangePerQuery, (ulong)safeBlock);

        var purchasedEvent = _web3.Eth.GetEvent<PurchasedEvent>(_options.ContractAddress);
        var filter = purchasedEvent.CreateFilterInput(
            new BlockParameter(fromBlock),
            new BlockParameter(toBlock)
        );

        var logs = await purchasedEvent.GetAllChangesAsync(filter);
        var pendingNotifications =
            new List<(string walletAddress, ContractEventResponse Payload)>();

        foreach (var log in logs)
        {
            var notification = await HandlePurchaseEventAsync(db, balanceService, log, ct);
            if (notification is not null)
                pendingNotifications.Add(notification.Value);
        }

        state.LastProcessedBlock = toBlock;
        await db.SaveChangesAsync(ct);

        foreach (var (wallet, payload) in pendingNotifications)
            notificationHub.Publish(wallet, payload);
    }

    private async Task<(string Wallet, ContractEventResponse Payload)?> HandlePurchaseEventAsync(
        AppDbContext db,
        IBalanceService balanceService,
        EventLog<PurchasedEvent> log,
        CancellationToken ct
    )
    {
        var txHash = log.Log.TransactionHash;
        var logIndex = (int)log.Log.LogIndex.Value;

        var alreadyProduced = await db.ProcessedPurchases.AnyAsync(
            p => p.TxHash == txHash && p.LogIndex == logIndex,
            ct
        );
        if (alreadyProduced)
            return null;

        var buyerAddress = log.Event.BuyerAddress.ToLower();
        var player = await db.Players.FirstOrDefaultAsync(p => p.WalletAddress == buyerAddress, ct);

        if (player is null)
        {
            _logger.LogWarning(
                "Purchase event for unknown wallet {Wallet}, tx {Tx}",
                log.Event.BuyerAddress,
                txHash
            );
            return null;
        }

        var eventType = log.Event.EventEnum;
        var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(
            log.Log.BlockNumber
        );
        var blockTimestamp = DateTimeOffset
            .FromUnixTimeSeconds((long)block.Timestamp.Value)
            .UtcDateTime;

        switch (eventType)
        {
            case EventType.BoostSpeed75m:
                ApplyBoost(balanceService, player, TimeSpan.FromMinutes(75));
                break;
            case EventType.BoostSpeed4h:
                ApplyBoost(balanceService, player, TimeSpan.FromMinutes(240));
                break;
            case EventType.BoostSpeed24h:
                ApplyBoost(balanceService, player, TimeSpan.FromMinutes(1440));
                break;
            case EventType.OfflineReward:
                balanceService.CollectWithBonus(player);
                break;
        }

        db.ProcessedPurchases.Add(
            new ProcessedPurchase
            {
                TxHash = txHash,
                LogIndex = logIndex,
                BuyerAddress = EthereumAddress.Parse(log.Event.BuyerAddress),
                TokenAddress = EthereumAddress.Parse(log.Event.TokenAddress),
                EventType = eventType,
                PaidAmount = log.Event.Paid,
                BlockTimestamp = blockTimestamp,
                ProcessedAt = DateTime.UtcNow,
            }
        );

        var payload = new ContractEventResponse
        {
            BuyerAddress = buyerAddress,
            EventType = eventType,
            TxHash = txHash,
            PaidAmount = log.Event.Paid,
            BlockTimestamp = blockTimestamp,
        };

        return (buyerAddress, payload);
    }

    private static void ApplyBoost(
        IBalanceService balanceService,
        Player player,
        TimeSpan boostDuration
    )
    {
        balanceService.CollectEarning(player);

        var now = DateTime.UtcNow;
        var isStacking = player.BoostExpiresAt.HasValue && player.BoostExpiresAt > now;

        player.BoostExpiresAt = isStacking
            ? player.BoostExpiresAt!.Value.Add(boostDuration)
            : now.Add(boostDuration);

        if (!isStacking)
            player.BoostActivatedAt = now;
    }
}
