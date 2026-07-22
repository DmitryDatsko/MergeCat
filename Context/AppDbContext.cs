using MergeCat.Models;
using MergeCat.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace MergeCat.Context;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IOptions<BlockchainOptions> blockchainOptions
) : DbContext(options)
{
    private readonly BlockchainOptions _blockchainOptions = blockchainOptions.Value;
    public DbSet<Player> Players { get; set; }
    public DbSet<Cell> Cells { get; set; }
    public DbSet<MergeLog> MergeLogs { get; set; }
    public DbSet<IndexerState> IndexerStates { get; set; }
    public DbSet<ProcessedPurchase> ProcessedPurchases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ethereumAddressConverter = new ValueConverter<EthereumAddress, string>(
            address => address.Value,
            value => EthereumAddress.Parse(value)
        );

        modelBuilder.Entity<Player>(entity =>
        {
            modelBuilder
                .Entity<Player>()
                .Property(p => p.WalletAddress)
                .HasConversion(ethereumAddressConverter);
            entity.HasIndex(p => p.WalletAddress).IsUnique();

            entity
                .Property(p => p.Version)
                .IsRowVersion()
                .HasColumnName("xmin")
                .HasColumnType("xid");

            entity
                .Property(p => p.League)
                .HasConversion(v => v.ToString(), v => Enum.Parse<League>(v));
        });

        modelBuilder.Entity<Cell>(entity =>
        {
            entity.HasIndex(c => new { c.PlayerId, c.Index }).IsUnique();
        });

        modelBuilder.Entity<ProcessedPurchase>(entity =>
        {
            modelBuilder
                .Entity<ProcessedPurchase>()
                .HasIndex(p => new { p.TxHash, p.LogIndex })
                .IsUnique();

            modelBuilder
                .Entity<ProcessedPurchase>()
                .Property(p => p.BuyerAddress)
                .HasConversion(ethereumAddressConverter);
            entity.HasIndex(p => p.BuyerAddress);

            modelBuilder
                .Entity<ProcessedPurchase>()
                .Property(p => p.TokenAddress)
                .HasConversion(ethereumAddressConverter);
            entity.HasIndex(p => p.TokenAddress);

            modelBuilder
                .Entity<ProcessedPurchase>()
                .Property(p => p.PaidAmount)
                .HasColumnType("numeric");
        });

        modelBuilder.Entity<IndexerState>(entity =>
        {
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.ToTable(t => t.HasCheckConstraint("CK_IndexerState_SingletonId", "\"id\" = 1"));

            entity.HasData(
                new IndexerState
                {
                    Id = 1,
                    LastProcessedBlock = (ulong)_blockchainOptions.ContractCreationBlock,
                }
            );
        });
    }
}
