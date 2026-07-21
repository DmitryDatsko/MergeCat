namespace MergeCat.Models.DTO;

public record ProfileResponse(
    double Balance,
    double IncomeRate,
    double TotalEarned,
    DateTime LastCollectedAt,
    string League,
    double ClaimableGold,
    double BonusGold,
    bool BonusClaimAvailable,
    DateTime? BoostExpiresAt
);
