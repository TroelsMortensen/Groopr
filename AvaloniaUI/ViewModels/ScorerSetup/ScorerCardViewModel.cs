using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUI.ViewModels.ScorerSetup;

public enum ScorerKind
{
    MutualMatch,
    PartialMatch
}

public partial class ScorerCardViewModel : ObservableObject
{
    public const string DefaultWeightText = "1.0";

    public ScorerKind Kind { get; }

    public string Title { get; }

    public string Description { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInactive))]
    public partial bool IsRuleEnabled { get; set; }

    public bool IsInactive => !IsRuleEnabled;

    [ObservableProperty]
    public partial string WeightText { get; set; }

    public ScorerCardViewModel(ScorerKind kind, string title, string description, bool isRuleEnabled = false)
    {
        Kind = kind;
        Title = title;
        Description = description;
        IsRuleEnabled = isRuleEnabled;
        WeightText = DefaultWeightText;
    }
}
