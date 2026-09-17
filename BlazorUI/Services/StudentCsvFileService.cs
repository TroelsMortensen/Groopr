using Logic.Import;
using Microsoft.JSInterop;

namespace BlazorUI.Services;

public sealed class StudentCsvFileService(IJSRuntime jsRuntime) : IStudentCsvFileService
{
    public async Task DownloadTemplateAsync() =>
        await jsRuntime.InvokeVoidAsync(
            "grooprDownload.downloadText",
            "students-template.csv",
            StudentCsvTemplate.GetContent(),
            "text/csv");
}
