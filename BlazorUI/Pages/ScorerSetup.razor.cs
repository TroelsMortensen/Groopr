using System.Globalization;
using BlazorUI.Data;
using BlazorUI.Data.ScoringConfiguration;
using BlazorUI.Models;
using BlazorUI.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages;

public partial class ScorerSetup
{
    [Inject] private InputConfiguration InputConfiguration { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private List<ScorerCardModel> ScorerCards { get; } = [];
    private string StatusMessage { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        if (InputConfiguration.StudentList is null || InputConfiguration.GroupSizeDistribution is null)
        {
            Navigation.NavigateTo("students", replace: true);
            return;
        }

        ScorerCards.Add(new ScorerCardModel
        {
            Kind = ScorerKind.MutualMatch,
            Title = "Mutual matches",
            Description = "Score pairs of students who both listed each other as positive wishes when placed in the same group."
        });
        ScorerCards.Add(new ScorerCardModel
        {
            Kind = ScorerKind.PartialMatch,
            Title = "Partial matches",
            Description = "Score one-way positive wishes when both students are in the same group, even if the wish is not mutual."
        });
        ScorerCards.Add(new ScorerCardModel
        {
            Kind = ScorerKind.NegativeMatch,
            Title = "Negative matches",
            Description = "Subtract this many points from the group composition score for each negative wish between students placed in the same group."
        });

        RestoreFromConfiguration();
    }

    private void Back()
    {
        ClearStatus();
        Navigation.NavigateTo("group-sizing");
    }

    private void Next()
    {
        ClearStatus();

        if (!ScorerCards.Any(card => card.IsRuleEnabled))
        {
            _ = DialogService.ShowErrorAsync("Enable at least one scoring rule before continuing.");
            return;
        }

        try
        {
            var enabledScorers = new List<ScorerConfiguration>();

            foreach (var card in ScorerCards.Where(card => card.IsRuleEnabled))
            {
                if (!TryParseWeight(card.WeightText, out var weight))
                {
                    _ = DialogService.ShowErrorAsync($"Enter a valid decimal weight for {card.Title}.");
                    return;
                }

                enabledScorers.Add(CreateConfiguration(card.Kind, weight));
            }

            InputConfiguration.EnabledScorers = enabledScorers;
            Navigation.NavigateTo("invalidation-setup");
        }
        catch (Exception ex)
        {
            _ = DialogService.ShowErrorAsync(ex.Message);
        }
    }

    private void RestoreFromConfiguration()
    {
        if (InputConfiguration.EnabledScorers.Count == 0)
        {
            return;
        }

        foreach (var card in ScorerCards)
        {
            card.IsRuleEnabled = false;
            card.WeightText = ScorerCardModel.DefaultWeightText;
        }

        foreach (var scorer in InputConfiguration.EnabledScorers)
        {
            switch (scorer)
            {
                case MutualMatchScoringConfiguration mutual:
                    ApplySavedConfiguration(ScorerKind.MutualMatch, mutual.Weight);
                    break;
                case PartialMatchScoringConfiguration partial:
                    ApplySavedConfiguration(ScorerKind.PartialMatch, partial.Weight);
                    break;
                case NegativeMatchScoringConfiguration negative:
                    ApplySavedConfiguration(ScorerKind.NegativeMatch, negative.Weight);
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

    private static ScorerConfiguration CreateConfiguration(ScorerKind kind, double weight) =>
        kind switch
        {
            ScorerKind.MutualMatch => MutualMatchScoringConfiguration.Create(weight),
            ScorerKind.PartialMatch => PartialMatchScoringConfiguration.Create(weight),
            ScorerKind.NegativeMatch => NegativeMatchScoringConfiguration.Create(weight),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown scorer kind.")
        };

    private void ClearStatus() => StatusMessage = string.Empty;
}
