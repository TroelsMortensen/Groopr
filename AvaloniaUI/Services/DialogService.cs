using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaUI.Views.Dialogs;

namespace AvaloniaUI.Services;

public sealed class DialogService : IDialogService
{
    public async Task ShowErrorAsync(string message)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var owner = desktop.MainWindow;
        if (owner is null)
        {
            return;
        }

        var dialog = new ErrorDialog
        {
            Message = message,
        };

        await dialog.ShowDialog(owner);
    }

    public async Task<bool> ShowConfirmAsync(string message)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return false;
        }

        var owner = desktop.MainWindow;
        if (owner is null)
        {
            return false;
        }

        var dialog = new ConfirmDialog
        {
            Message = message,
        };

        return await dialog.ShowDialog<bool>(owner);
    }
}
