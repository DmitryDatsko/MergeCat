namespace MergeCat.Models.DTO;

public record CollectResponse(
    double Balance,
    double IncomeRate,
    double TotalEarned,
    DateTime LastCollectedAt,
    string League,
    DateTime? BoostExpiresAt
);
