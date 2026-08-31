using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using AvaloniaUI.Data;
using AvaloniaUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Logic.Import;
using Logic.Models;

namespace AvaloniaUI.ViewModels.StudentData;

public partial class StudentDataViewModel : ViewModelBase
{
    private readonly InputConfiguration _inputConfiguration;
    private readonly IDialogService _dialogService;
    private readonly IStudentCsvFileService _csvFileService;
    private readonly StudentCsvImporter _csvImporter = new();

    public ObservableCollection<StudentEntryViewModel> Students { get; } = [];

    [ObservableProperty]
    public partial string FormNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FormName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FormPositiveWishes { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditPanelVisible))]
    public partial StudentEntryViewModel? SelectedStudent { get; set; }

    [ObservableProperty]
    public partial string EditNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditPositiveWishes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public bool IsEditPanelVisible => SelectedStudent is not null;

    public StudentDataViewModel() : this(new InputConfiguration(), new NullDialogService(), new NullStudentCsvFileService())
    {
    }

    public StudentDataViewModel(
        InputConfiguration inputConfiguration,
        IDialogService dialogService,
        IStudentCsvFileService csvFileService)
    {
        _inputConfiguration = inputConfiguration;
        _dialogService = dialogService;
        _csvFileService = csvFileService;
    }

    [RelayCommand]
    private void SelectStudent(StudentEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        if (SelectedStudent == entry)
        {
            CancelEdit();
            return;
        }

        ClearStatus();
        SelectedStudent = entry;
        LoadEditFields(entry);
    }

    [RelayCommand]
    private void SaveChanges()
    {
        if (SelectedStudent is null)
        {
            return;
        }

        ClearStatus();

        if (string.IsNullOrWhiteSpace(EditNumber))
        {
            ShowError("Student number is required.");
            return;
        }

        var number = EditNumber.Trim();
        var name = string.IsNullOrWhiteSpace(EditName) ? null : EditName.Trim();
        var wishes = ParseWishes(EditPositiveWishes);

        if (Students.Any(s => s != SelectedStudent && s.Number == number))
        {
            ShowError($"Student number {number} already exists.");
            return;
        }

        SelectedStudent.Number = number;
        SelectedStudent.Name = name;
        SelectedStudent.PositiveWishes = wishes;

        CancelEdit();
    }

    [RelayCommand]
    private void AddStudent()
    {
        ClearStatus();

        if (string.IsNullOrWhiteSpace(FormNumber))
        {
            ShowError("Student number is required.");
            return;
        }

        var number = FormNumber.Trim();
        var name = string.IsNullOrWhiteSpace(FormName) ? null : FormName.Trim();
        var wishes = ParseWishes(FormPositiveWishes);

        if (Students.Any(s => s.Number == number))
        {
            ShowError($"Student number {number} already exists.");
            return;
        }

        try
        {
            StudentNumber.Create(number);
        }
        catch (Exception e)
        {
            ShowError(e.Message);
            return;
        }

        Students.Add(new StudentEntryViewModel
        {
            Number = number,
            Name = name,
            PositiveWishes = wishes,
        });

        ClearForm();
    }

    [RelayCommand]
    private void DeleteStudent(StudentEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        ClearStatus();
        Students.Remove(entry);

        if (SelectedStudent == entry)
        {
            CancelEdit();
        }
    }

    [RelayCommand]
    private void Next()
    {
        ClearStatus();

        try
        {
            var studentList = StudentList.Create(Students.Select(s => s.ToStudent()).ToList());
            _inputConfiguration.StudentList = studentList;
            SetSuccessStatus($"Saved {studentList.Students.Count} students.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DownloadTemplateAsync(Visual? visual)
    {
        if (visual is null)
        {
            return;
        }

        ClearStatus();
        var saved = await _csvFileService.SaveTemplateAsync(visual);
        if (saved)
        {
            SetSuccessStatus("Template saved.");
        }
    }

    [RelayCommand]
    private async Task ImportCsvAsync(Visual? visual)
    {
        if (visual is null)
        {
            return;
        }

        ClearStatus();

        if (Students.Count > 0)
        {
            var confirmed = await _dialogService.ShowConfirmAsync(
                $"Importing will replace the {Students.Count} student(s) currently in the list. Continue?");
            if (!confirmed)
            {
                return;
            }
        }

        await using var stream = await _csvFileService.OpenCsvForReadAsync(visual);
        if (stream is null)
        {
            return;
        }

        try
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            var studentList = _csvImporter.Import(reader);

            CancelEdit();
            Students.Clear();

            foreach (var student in studentList.Students)
            {
                Students.Add(StudentEntryViewModel.FromStudent(student));
            }

            SetSuccessStatus($"Imported {studentList.Students.Count} students.");
        }
        catch (StudentCsvImportException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void LoadEditFields(StudentEntryViewModel entry)
    {
        EditNumber = entry.Number;
        EditName = entry.Name ?? string.Empty;
        EditPositiveWishes = string.Join(", ", entry.PositiveWishes);
    }

    private void CancelEdit()
    {
        SelectedStudent = null;
        EditNumber = string.Empty;
        EditName = string.Empty;
        EditPositiveWishes = string.Empty;
    }

    private void ClearForm()
    {
        FormNumber = string.Empty;
        FormName = string.Empty;
        FormPositiveWishes = string.Empty;
    }

    private void ClearStatus()
    {
        StatusMessage = string.Empty;
    }

    private void SetSuccessStatus(string message)
    {
        StatusMessage = message;
    }

    private void ShowError(string message) => _ = _dialogService.ShowErrorAsync(message);

    private static List<string> ParseWishes(string input) =>
        input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static s => s.Length > 0)
            .ToList();
}
