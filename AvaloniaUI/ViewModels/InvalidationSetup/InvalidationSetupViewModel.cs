using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using AvaloniaUI.Data;
using AvaloniaUI.Data.InvalidationConfiguration;
using AvaloniaUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaUI.ViewModels.InvalidationSetup;

public partial class InvalidationSetupViewModel : ViewModelBase
{
    private readonly InputConfiguration _inputConfiguration;
    private readonly IDialogService _dialogService;
    private readonly Action _navigateBack;
    private readonly Action _navigateForward;

    public ObservableCollection<InvalidatorCardViewModel> InvalidatorCards { get; } = [];

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public InvalidationSetupViewModel() : this(new InputConfiguration(), new NullDialogService(), static () => { }, static () => { })
    {
    }

    public InvalidationSetupViewModel(
        InputConfiguration inputConfiguration,
        IDialogService dialogService,
        Action navigateBack,
        Action navigateForward)
    {
        _inputConfiguration = inputConfiguration;
        _dialogService = dialogService;
        _navigateBack = navigateBack;
        _navigateForward = navigateForward;

        var maxPreviousGroupCard = new InvalidatorCardViewModel(
            InvalidatorKind.MaxNumberOfStudentsFromPreviousGroup,
            "Max students from previous group",
            "For each student in a group, counts how many of their listed previous group members are in the same new group. Rejects the composition if any student exceeds the configured maximum.");

        InvalidatorCards.Add(maxPreviousGroupCard);

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

        try
        {
            var enabledInvalidators = new List<InvalidatorConfiguration>();

            foreach (var card in InvalidatorCards.Where(card => card.IsRuleEnabled))
            {
                if (!TryParseMaxCount(card.MaxCountText, out var maxCount))
                {
                    ShowError($"Enter a valid integer maximum for {card.Title}.");
                    return;
                }

                enabledInvalidators.Add(CreateConfiguration(card.Kind, maxCount));
            }

            _inputConfiguration.EnabledInvalidators = enabledInvalidators;
            _navigateForward();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void RestoreFromConfiguration()
    {
        if (_inputConfiguration.EnabledInvalidators.Count == 0)
        {
            return;
        }

        foreach (var card in InvalidatorCards)
        {
            card.IsRuleEnabled = false;
            card.MaxCountText = InvalidatorCardViewModel.DefaultMaxCountText;
        }

        foreach (var invalidator in _inputConfiguration.EnabledInvalidators)
        {
            switch (invalidator)
            {
                case MaxNumberOfStudentsFromPreviousGroupInvalidatorConfiguration maxPrevious:
                    ApplySavedConfiguration(
                        InvalidatorKind.MaxNumberOfStudentsFromPreviousGroup,
                        maxPrevious.MaxCount);
                    break;
            }
        }
    }

    private void ApplySavedConfiguration(InvalidatorKind kind, int maxCount)
    {
        var card = InvalidatorCards.FirstOrDefault(c => c.Kind == kind);
        if (card is null)
        {
            return;
        }

        card.IsRuleEnabled = true;
        card.MaxCountText = maxCount.ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryParseMaxCount(string input, out int maxCount)
    {
        if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxCount))
        {
            return false;
        }

        return maxCount >= 1;
    }

    private static InvalidatorConfiguration CreateConfiguration(InvalidatorKind kind, int maxCount) =>
        kind switch
        {
            InvalidatorKind.MaxNumberOfStudentsFromPreviousGroup =>
                MaxNumberOfStudentsFromPreviousGroupInvalidatorConfiguration.Create(maxCount),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown invalidator kind.")
        };

    private void ClearStatus() => StatusMessage = string.Empty;

    private void ShowError(string message) => _ = _dialogService.ShowErrorAsync(message);
}
