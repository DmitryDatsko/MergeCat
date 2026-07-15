using MergeCat.Models;
using Microsoft.EntityFrameworkCore;

namespace MergeCat.Context;

public class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players { get; set; }
    public DbSet<Cell> Cells { get; set; }
    public DbSet<MergeLog> MergeLogs { get; set; }

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
    }
}
