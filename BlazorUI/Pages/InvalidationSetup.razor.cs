using System.Globalization;
using BlazorUI.Data;
using BlazorUI.Data.InvalidationConfiguration;
using BlazorUI.Models;
using BlazorUI.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages;

public partial class InvalidationSetup
{
    [Inject] private InputConfiguration InputConfiguration { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private List<InvalidatorCardModel> InvalidatorCards { get; } = [];
    private string StatusMessage { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        if (InputConfiguration.StudentList is null
            || InputConfiguration.GroupSizeDistribution is null
            || InputConfiguration.EnabledScorers.Count == 0)
        {
            Navigation.NavigateTo("students", replace: true);
            return;
        }

        InvalidatorCards.Add(new InvalidatorCardModel
        {
            Kind = InvalidatorKind.MaxNumberOfStudentsFromPreviousGroup,
            Title = "Max students from previous group",
            Description = "For each student in a group, counts how many of their listed previous group members are in the same new group. Rejects the composition if any student exceeds the configured maximum."
        });

        RestoreFromConfiguration();
    }

    private void Back()
    {
        ClearStatus();
        Navigation.NavigateTo("scorer-setup");
    }

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
                    _ = DialogService.ShowErrorAsync($"Enter a valid integer maximum for {card.Title}.");
                    return;
                }

                enabledInvalidators.Add(CreateConfiguration(card.Kind, maxCount));
            }

            InputConfiguration.EnabledInvalidators = enabledInvalidators;
            Navigation.NavigateTo("generation");
        }
        catch (Exception ex)
        {
            _ = DialogService.ShowErrorAsync(ex.Message);
        }
    }

    private void RestoreFromConfiguration()
    {
        if (InputConfiguration.EnabledInvalidators.Count == 0)
        {
            return;
        }

        foreach (var card in InvalidatorCards)
        {
            card.IsRuleEnabled = false;
            card.MaxCountText = InvalidatorCardModel.DefaultMaxCountText;
        }

        foreach (var invalidator in InputConfiguration.EnabledInvalidators)
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
}
