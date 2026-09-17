using BlazorUI.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components;

public partial class DialogHost : ComponentBase, IDisposable
{
    [Inject]
    private DialogService Dialogs { get; set; } = null!;

    protected override void OnInitialized()
    {
        Dialogs.Changed += OnDialogChanged;
    }

    private void OnDialogChanged() => InvokeAsync(StateHasChanged);

    private void OnBackdropClick()
    {
        if (Dialogs.IsConfirm)
        {
            Dialogs.CloseConfirm(false);
        }
        else
        {
            Dialogs.CloseError();
        }
    }

    public void Dispose() => Dialogs.Changed -= OnDialogChanged;
}
