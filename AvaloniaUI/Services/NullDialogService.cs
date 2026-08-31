using System.Threading.Tasks;

namespace AvaloniaUI.Services;

public sealed class NullDialogService : IDialogService
{
    public Task ShowErrorAsync(string message) => Task.CompletedTask;

    public Task<bool> ShowConfirmAsync(string message) => Task.FromResult(false);
}
