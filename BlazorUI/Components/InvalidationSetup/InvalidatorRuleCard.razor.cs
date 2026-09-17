using BlazorUI.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.InvalidationSetup;

public partial class InvalidatorRuleCard
{
    [Parameter] public InvalidatorCardModel Card { get; set; } = null!;
    [Parameter] public EventCallback OnChanged { get; set; }

    private async Task OnEnabledChanged(ChangeEventArgs args)
    {
        Card.IsRuleEnabled = args.Value is true;
        await OnChanged.InvokeAsync();
    }

    private async Task OnMaxCountChanged(ChangeEventArgs args)
    {
        Card.MaxCountText = args.Value?.ToString() ?? string.Empty;
        await OnChanged.InvokeAsync();
    }
}
