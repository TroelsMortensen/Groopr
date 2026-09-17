namespace BlazorUI.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}
