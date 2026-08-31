namespace Logic.Models;

public record GroupComposition(IReadOnlyList<Group> Groups, double TotalScore = 0.0);