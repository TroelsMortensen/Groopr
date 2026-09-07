using System.Threading.Tasks;

namespace AvaloniaUI.Services;

public sealed class NullClipboardService : IClipboardService
{
    public Task SetTextAsync(string text) => Task.CompletedTask;
}
