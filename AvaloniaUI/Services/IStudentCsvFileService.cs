using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AvaloniaUI.Services;

public interface IStudentCsvFileService
{
    Task<bool> SaveTemplateAsync(Visual visual);

    Task<Stream?> OpenCsvForReadAsync(Visual visual);
}
