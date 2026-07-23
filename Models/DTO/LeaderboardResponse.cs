namespace MergeCat.Models.DTO;

public record LeaderboardResponse(
    List<LeaderboardInstance> Players,
    double Threshold,
    bool HasMore
);
