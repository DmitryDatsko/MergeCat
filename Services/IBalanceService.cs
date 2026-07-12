using MergeCat.Models;

namespace MergeCat.Services;

public interface IBalanceService
{
    Task CollectAsync(Player player);
}
