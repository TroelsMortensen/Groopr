using Microsoft.JSInterop;

namespace BlazorUI.Services;

public sealed class ClipboardService(IJSRuntime jsRuntime) : IClipboardService
{
    public async Task SetTextAsync(string text) =>
        await jsRuntime.InvokeVoidAsync("grooprClipboard.setText", text);
}
