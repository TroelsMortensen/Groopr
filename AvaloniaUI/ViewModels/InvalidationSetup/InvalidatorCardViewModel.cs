using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUI.ViewModels.InvalidationSetup;

public enum InvalidatorKind
{
    MaxNumberOfStudentsFromPreviousGroup
}

public partial class InvalidatorCardViewModel : ObservableObject
{
    public const string DefaultMaxCountText = "2";

    public InvalidatorKind Kind { get; }

    public string Title { get; }

    public string Description { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInactive))]
    public partial bool IsRuleEnabled { get; set; }

    public bool IsInactive => !IsRuleEnabled;

    [ObservableProperty]
    public partial string MaxCountText { get; set; }

    public InvalidatorCardViewModel(InvalidatorKind kind, string title, string description, bool isRuleEnabled = false)
    {
        Kind = kind;
        Title = title;
        Description = description;
        IsRuleEnabled = isRuleEnabled;
        MaxCountText = DefaultMaxCountText;
    }
}
