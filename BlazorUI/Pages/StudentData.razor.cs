using BlazorUI.Data;
using BlazorUI.Models;
using BlazorUI.Services;
using Logic.Import;
using Logic.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorUI.Pages;

public partial class StudentData
{
    [Inject] private InputConfiguration InputConfiguration { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IStudentCsvFileService CsvFileService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private readonly StudentCsvImporter _csvImporter = new();
    private List<StudentEntry> Students { get; } = [];

    private string FormNumber { get; set; } = string.Empty;
    private string FormName { get; set; } = string.Empty;
    private string FormPositiveWishes { get; set; } = string.Empty;
    private string FormPreviousGroupMembers { get; set; } = string.Empty;
    private string FormNegativeWishes { get; set; } = string.Empty;

    private StudentEntry? SelectedStudent { get; set; }
    private string EditNumber { get; set; } = string.Empty;
    private string EditName { get; set; } = string.Empty;
    private string EditPositiveWishes { get; set; } = string.Empty;
    private string EditPreviousGroupMembers { get; set; } = string.Empty;
    private string EditNegativeWishes { get; set; } = string.Empty;
    private string StatusMessage { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        if (InputConfiguration.StudentList is { } studentList)
        {
            foreach (var student in studentList.Students)
            {
                Students.Add(StudentEntry.FromStudent(student));
            }

            SortStudents();
        }
    }

    private void SelectStudent(StudentEntry entry)
    {
        if (SelectedStudent == entry)
        {
            CancelEdit();
            return;
        }

        ClearStatus();
        SelectedStudent = entry;
        LoadEditFields(entry);
    }

    private void SaveChanges()
    {
        if (SelectedStudent is null)
        {
            return;
        }

        ClearStatus();

        if (string.IsNullOrWhiteSpace(EditNumber))
        {
            _ = DialogService.ShowErrorAsync("Student number is required.");
            return;
        }

        var number = EditNumber.Trim();
        var name = string.IsNullOrWhiteSpace(EditName) ? null : EditName.Trim();
        var wishes = ParseStudentNumbers(EditPositiveWishes);
        var previousGroupMembers = ParseStudentNumbers(EditPreviousGroupMembers);
        var negativeWishes = ParseStudentNumbers(EditNegativeWishes);

        if (Students.Any(s => s != SelectedStudent && s.Number == number))
        {
            _ = DialogService.ShowErrorAsync($"Student number {number} already exists.");
            return;
        }

        SelectedStudent.Number = number;
        SelectedStudent.Name = name;
        SelectedStudent.PositiveWishes = wishes;
        SelectedStudent.PreviousGroupMembers = previousGroupMembers;
        SelectedStudent.NegativeWishes = negativeWishes;

        SortStudents();
        CancelEdit();
    }

    private void AddStudent()
    {
        ClearStatus();

        if (string.IsNullOrWhiteSpace(FormNumber))
        {
            _ = DialogService.ShowErrorAsync("Student number is required.");
            return;
        }

        var number = FormNumber.Trim();
        var name = string.IsNullOrWhiteSpace(FormName) ? null : FormName.Trim();
        var wishes = ParseStudentNumbers(FormPositiveWishes);
        var previousGroupMembers = ParseStudentNumbers(FormPreviousGroupMembers);
        var negativeWishes = ParseStudentNumbers(FormNegativeWishes);

        if (Students.Any(s => s.Number == number))
        {
            _ = DialogService.ShowErrorAsync($"Student number {number} already exists.");
            return;
        }

        try
        {
            StudentNumber.Create(number);
        }
        catch (Exception e)
        {
            _ = DialogService.ShowErrorAsync(e.Message);
            return;
        }

        Students.Add(new StudentEntry
        {
            Number = number,
            Name = name,
            PositiveWishes = wishes,
            PreviousGroupMembers = previousGroupMembers,
            NegativeWishes = negativeWishes,
        });

        SortStudents();
        ClearForm();
    }

    private void DeleteStudent(StudentEntry entry)
    {
        ClearStatus();
        Students.Remove(entry);

        if (SelectedStudent == entry)
        {
            CancelEdit();
        }
    }

    private void Next()
    {
        ClearStatus();

        try
        {
            var studentList = StudentList.Create(Students.Select(s => s.ToStudent()).ToList());
            InputConfiguration.StudentList = studentList;
            Navigation.NavigateTo("group-sizing");
        }
        catch (Exception ex)
        {
            _ = DialogService.ShowErrorAsync(ex.Message);
        }
    }

    private async Task DownloadTemplateAsync()
    {
        ClearStatus();
        await CsvFileService.DownloadTemplateAsync();
        StatusMessage = "Template saved.";
    }

    private async Task ImportCsvAsync(InputFileChangeEventArgs args)
    {
        ClearStatus();

        if (Students.Count > 0)
        {
            var confirmed = await DialogService.ShowConfirmAsync(
                $"Importing will replace the {Students.Count} student(s) currently in the list. Continue?");
            if (!confirmed)
            {
                return;
            }
        }

        var file = args.File;
        if (file is null)
        {
            return;
        }

        try
        {
            // Browser file streams only support async reads; CsvHelper/StreamReader read
            // synchronously, so buffer into memory first.
            await using var browserStream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            using var memoryStream = new MemoryStream();
            await browserStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var reader = new StreamReader(memoryStream);
            var studentList = _csvImporter.Import(reader);

            CancelEdit();
            Students.Clear();

            foreach (var student in studentList.Students)
            {
                Students.Add(StudentEntry.FromStudent(student));
            }

            SortStudents();
            StatusMessage = $"Imported {studentList.Students.Count} students.";
        }
        catch (StudentCsvImportException ex)
        {
            await DialogService.ShowErrorAsync(ex.Message);
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(ex.Message);
        }
    }

    private void LoadEditFields(StudentEntry entry)
    {
        EditNumber = entry.Number;
        EditName = entry.Name ?? string.Empty;
        EditPositiveWishes = string.Join(", ", entry.PositiveWishes);
        EditPreviousGroupMembers = string.Join(", ", entry.PreviousGroupMembers);
        EditNegativeWishes = string.Join(", ", entry.NegativeWishes);
    }

    private void CancelEdit()
    {
        SelectedStudent = null;
        EditNumber = string.Empty;
        EditName = string.Empty;
        EditPositiveWishes = string.Empty;
        EditPreviousGroupMembers = string.Empty;
        EditNegativeWishes = string.Empty;
    }

    private void ClearForm()
    {
        FormNumber = string.Empty;
        FormName = string.Empty;
        FormPositiveWishes = string.Empty;
        FormPreviousGroupMembers = string.Empty;
        FormNegativeWishes = string.Empty;
    }

    private void ClearStatus() => StatusMessage = string.Empty;

    private static List<string> ParseStudentNumbers(string input) =>
        input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static s => s.Length > 0)
            .ToList();

    private void SortStudents()
    {
        var sorted = Students.OrderBy(static s => int.Parse(s.Number)).ToList();
        Students.Clear();
        Students.AddRange(sorted);
    }
}
