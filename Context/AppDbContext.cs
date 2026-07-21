using MergeCat.Models;
using Microsoft.EntityFrameworkCore;

namespace MergeCat.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players { get; set; }
    public DbSet<Cell> Cells { get; set; }
    public DbSet<MergeLog> MergeLogs { get; set; }
    public DbSet<IndexerState> IndexerStates { get; set; }
    public DbSet<ProcessedPurchase> ProcessedPurchases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity
                .Property(p => p.WalletAddress)
                .HasConversion(v => v.Value, v => EthereumAddress.Parse(v))
                .HasColumnType("char(42)")
                .IsRequired();

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

        modelBuilder
            .Entity<ProcessedPurchase>()
            .HasIndex(p => new { p.TxHash, p.LogIndex })
            .IsUnique();

        modelBuilder.Entity<IndexerState>(entity =>
        {
            entity.Property(s => s.Id).ValueGeneratedNever();
            entity.ToTable(t => t.HasCheckConstraint("CK_IndexerState_SingletonId", "\"Id\" = 1"));

            entity.HasData(new IndexerState { Id = 1, LastProcessedBlock = 72693190 });
        });
    }
}
