I want to add a new section to the current StudentData view, which will allow the user to bulk import a list of students from a CSV file.

Create the section below the current Add Student section.

Add a button to download a sample CSV file, the code could be something like this:

```csharp
var files = await TopLevel.GetTopLevel(view)?.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
    {
        Title = "Save Student Template",
        SuggestedFileName = "student_import_template.csv",
        FileTypeChoices = [new FilePickerFileType("CSV Files") { Patterns = ["*.csv"] }]
    });

    if (files is not null)
    {
        await using var stream = await files.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteLineAsync("StudentNumber,Name,PositiveWishes");
        await writer.WriteLineAsync("123456,John Doe,\"654321, 789654\"");
    }
```

The csv file can then be loaded into excel, and the user can edit the file to their liking. The wishes are a comma separated list of student numbers.

Add a button to upload the CSV file of the above format, and parse the file into a list of students. Update the student list sidebar to show the new students.

In case of errors, do not import any students, instead show a message to the user using the ErrorDialog class, as is standard for the project.
The entire csv file should be parsed into Model classes, e.g. Student, StudentNumber, StudentList, etc, to validate the input. Then convert it to whatever the view needs, e.g. perhaps just strings.

I want this import/export functionality to be separated into its own class, instead of being mixed in with the current StudentData view, to the extend it is possible. 

Find below my conversation with the AI assistant, and use it to guide your work.

## Conversation with the AI assistant

Offering a "Download Template" button alongside a "Upload CSV" file picker is the gold standard for data import UX. It completely eliminates guesswork around column headers, casing, or order.

Here is the best approach for how to structure this feature, blending a smooth UX with robust backend safety:

1. The UX Layout
In your Step 1 view, add a dedicated section or card for Bulk Import alongside your manual entry form:

[ Download CSV Template ] — Generates and saves a clean, pre-formatted .csv file directly to the user's computer.

[ Browse / Upload CSV... ] — Opens a native file dialog (Avalonia's StorageProvider) to select the file.

Status message / Error display — Tells them how many students were successfully loaded, or clearly lists line numbers where errors occurred.

2. The CSV Template Design
Keep the format dead simple. Two or three columns are all you need:

Code snippet
StudentNumber,Name,PositiveWishes
1,Alice,"2, 5, 12"
2,Bob,3
3,Charlie,"1, 4"
Tip: Wrapping wish lists in quotes ("2, 5, 12") is standard CSV practice so that commas inside the wish list don't accidentally break your column parser.

3. Implementing the Download Feature in Avalonia
When they click "Download Template", you can generate a default file programmatically and save it using Avalonia's file picker:

C#
// Inside your ViewModel / Command
[RelayCommand]
private async Task DownloadTemplateAsync()
{
    var files = await TopLevel.GetTopLevel(view)?.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
    {
        Title = "Save Student Template",
        SuggestedFileName = "student_import_template.csv",
        FileTypeChoices = [new FilePickerFileType("CSV Files") { Patterns = ["*.csv"] }]
    });

    if (files is not null)
    {
        await using var stream = await files.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteLineAsync("StudentNumber,Name,PositiveWishes");
        await writer.WriteLineAsync("1,Example Student,\"2, 3\"");
    }
}
4. Handling the Upload & Validation Pipeline
When they upload a file, you want to parse it and run it straight through your existing strongly-typed models (StudentList, etc.) so that validation is unified whether they typed it by hand or uploaded a spreadsheet.

To avoid breaking on minor formatting quirks (like extra spaces, lowercase headers, or missing optional names), use a robust parser like CsvHelper (via NuGet) or a lightweight manual StreamReader loop.

Here is how the parser should handle error reporting:

Row-by-Row Validation: If row 4 has a typo (e.g., a student wishing for a non-existent ID, or text in a numeric field), don't just throw a generic crash. Catch it and tell the user: "Error on row 4: Student number 'abc' is not a valid integer."