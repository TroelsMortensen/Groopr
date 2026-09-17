using BlazorUI.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.ScorerSetup;

public partial class ScorerRuleCard
{
    [Parameter] public ScorerCardModel Card { get; set; } = null!;
    [Parameter] public EventCallback OnChanged { get; set; }

    private async Task OnEnabledChanged(ChangeEventArgs args)
    {
        Card.IsRuleEnabled = args.Value is true;
        await OnChanged.InvokeAsync();
    }

    private async Task OnWeightChanged(ChangeEventArgs args)
    {
        Card.WeightText = args.Value?.ToString() ?? string.Empty;
        await OnChanged.InvokeAsync();
    }
}
