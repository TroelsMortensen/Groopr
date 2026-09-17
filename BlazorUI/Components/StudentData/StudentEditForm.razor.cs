using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.StudentData;

public partial class StudentEditForm
{
    [Parameter] public string Number { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> NumberChanged { get; set; }
    [Parameter] public string Name { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> NameChanged { get; set; }
    [Parameter] public string PositiveWishes { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> PositiveWishesChanged { get; set; }
    [Parameter] public string PreviousGroupMembers { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> PreviousGroupMembersChanged { get; set; }
    [Parameter] public string NegativeWishes { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> NegativeWishesChanged { get; set; }
    [Parameter] public EventCallback OnSave { get; set; }
}
