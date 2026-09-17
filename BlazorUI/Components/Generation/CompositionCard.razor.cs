using BlazorUI.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.Generation;

public partial class CompositionCard
{
    [Parameter] public CompositionCardModel Composition { get; set; } = null!;
    [Parameter] public EventCallback<CompositionCardModel> OnCopy { get; set; }
}
