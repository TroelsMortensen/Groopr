using System.Threading.Tasks;

namespace AvaloniaUI.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}
