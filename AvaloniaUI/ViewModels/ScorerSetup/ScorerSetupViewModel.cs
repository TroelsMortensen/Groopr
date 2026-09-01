using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using AvaloniaUI.Data;
using AvaloniaUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaUI.ViewModels.ScorerSetup;

public partial class ScorerSetupViewModel : ViewModelBase
{
    private readonly InputConfiguration _inputConfiguration;
    private readonly IDialogService _dialogService;
    private readonly Action _navigateBack;

    public ObservableCollection<ScorerCardViewModel> ScorerCards { get; } = [];

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public ScorerSetupViewModel() : this(new InputConfiguration(), new NullDialogService(), static () => { })
    {
    }

    public ScorerSetupViewModel(
        InputConfiguration inputConfiguration,
        IDialogService dialogService,
        Action navigateBack)
    {
        _inputConfiguration = inputConfiguration;
        _dialogService = dialogService;
        _navigateBack = navigateBack;

        var mutualCard = new ScorerCardViewModel(
            ScorerKind.MutualMatch,
            "Mutual matches",
            "Score pairs of students who both listed each other as positive wishes when placed in the same group.");

        var partialCard = new ScorerCardViewModel(
            ScorerKind.PartialMatch,
            "Partial matches",
            "Score one-way positive wishes when both students are in the same group, even if the wish is not mutual.");

        ScorerCards.Add(mutualCard);
        ScorerCards.Add(partialCard);

        RestoreFromConfiguration();
    }

    [RelayCommand]
    private void Back()
    {
        ClearStatus();
        _navigateBack();
    }

    [RelayCommand]
    private void Next()
    {
        ClearStatus();

        if (!ScorerCards.Any(card => card.IsRuleEnabled))
        {
            ShowError("Enable at least one scoring rule before continuing.");
            return;
        }

        try
        {
            var enabledScorers = new List<ScorerConfigurationRecord>();

            foreach (var card in ScorerCards.Where(card => card.IsRuleEnabled))
            {
                if (!TryParseWeight(card.WeightText, out var weight))
                {
                    ShowError($"Enter a valid decimal weight for {card.Title}.");
                    return;
                }

                enabledScorers.Add(CreateConfiguration(card.Kind, weight));
            }

            _inputConfiguration.EnabledScorers = enabledScorers;
            SetSuccessStatus($"Saved {enabledScorers.Count} scoring rule(s).");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void RestoreFromConfiguration()
    {
        if (_inputConfiguration.EnabledScorers.Count == 0)
        {
            return;
        }

        foreach (var card in ScorerCards)
        {
            card.IsRuleEnabled = false;
            card.WeightText = ScorerCardViewModel.DefaultWeightText;
        }

        foreach (var scorer in _inputConfiguration.EnabledScorers)
        {
            switch (scorer)
            {
                case MutualMatchConfiguration mutual:
                    ApplySavedConfiguration(ScorerKind.MutualMatch, mutual.Weight);
                    break;
                case PartialMatchConfiguration partial:
                    ApplySavedConfiguration(ScorerKind.PartialMatch, partial.Weight);
                    break;
            }
        }
    }

    private void ApplySavedConfiguration(ScorerKind kind, double weight)
    {
        var card = ScorerCards.FirstOrDefault(c => c.Kind == kind);
        if (card is null)
        {
            return;
        }

        card.IsRuleEnabled = true;
        card.WeightText = weight.ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryParseWeight(string input, out double weight) =>
        double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out weight);

    private static ScorerConfigurationRecord CreateConfiguration(ScorerKind kind, double weight) =>
        kind switch
        {
            ScorerKind.MutualMatch => MutualMatchConfiguration.Create(weight),
            ScorerKind.PartialMatch => PartialMatchConfiguration.Create(weight),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown scorer kind.")
        };

    private void ClearStatus() => StatusMessage = string.Empty;

    private void SetSuccessStatus(string message) => StatusMessage = message;

    private void ShowError(string message) => _ = _dialogService.ShowErrorAsync(message);
}
