using MergeCat.Models;

namespace MergeCat.Services;

public interface IBalanceService
{
    void CollectEarning(Player player);
    void CollectWithBonus(Player player);
    double CalculateEarningsForRange(DateTime rangeStart, DateTime rangeEnd, Player player);
    (double ClaimableGold, double BonusGold) PreviewEarnings(Player player);
}
