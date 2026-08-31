using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUI.Services;

public sealed class NullStudentCsvFileService : IStudentCsvFileService
{
    public Task<bool> SaveTemplateAsync(Visual visual) => Task.FromResult(false);

    public Task<Stream?> OpenCsvForReadAsync(Visual visual) => Task.FromResult<Stream?>(null);
}
