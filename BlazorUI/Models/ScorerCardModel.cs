namespace BlazorUI.Models;

public enum ScorerKind
{
    MutualMatch,
    PartialMatch,
    NegativeMatch
}

public class ScorerCardModel
{
    public const string DefaultWeightText = "1.0";

    public ScorerKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsRuleEnabled { get; set; }
    public string WeightText { get; set; } = DefaultWeightText;

    public bool IsInactive => !IsRuleEnabled;
}
