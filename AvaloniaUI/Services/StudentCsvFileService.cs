using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Logic.Import;

namespace AvaloniaUI.Services;

public sealed class StudentCsvFileService : IStudentCsvFileService
{
    public async Task<bool> SaveTemplateAsync(Visual visual)
    {
        var topLevel = TopLevel.GetTopLevel(visual);
        if (topLevel is null)
        {
            return false;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Student Template",
            SuggestedFileName = "student_import_template.csv",
            FileTypeChoices =
            [
                new FilePickerFileType("CSV Files") { Patterns = ["*.csv"] },
            ],
        });

        if (file is null)
        {
            return false;
        }

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(StudentCsvTemplate.GetContent());
        return true;
    }

    public async Task<Stream?> OpenCsvForReadAsync(Visual visual)
    {
        var topLevel = TopLevel.GetTopLevel(visual);
        if (topLevel is null)
        {
            return null;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Student CSV",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("CSV Files") { Patterns = ["*.csv"] },
            ],
        });

        if (files.Count == 0)
        {
            return null;
        }

        return await files[0].OpenReadAsync();
    }
}
