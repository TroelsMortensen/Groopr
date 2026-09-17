namespace BlazorUI.Models;

public enum InvalidatorKind
{
    MaxNumberOfStudentsFromPreviousGroup
}

public class InvalidatorCardModel
{
    public const string DefaultMaxCountText = "2";

    public InvalidatorKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsRuleEnabled { get; set; }
    public string MaxCountText { get; set; } = DefaultMaxCountText;

    public bool IsInactive => !IsRuleEnabled;
}
