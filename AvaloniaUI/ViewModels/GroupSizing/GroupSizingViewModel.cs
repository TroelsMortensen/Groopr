using System;
using AvaloniaUI.Data;
using AvaloniaUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Logic.GroupSizing;
using Logic.Models;

namespace AvaloniaUI.ViewModels.GroupSizing;

public partial class GroupSizingViewModel : ViewModelBase
{
    private readonly InputConfiguration _inputConfiguration;
    private readonly IDialogService _dialogService;
    private readonly Action _navigateBack;
    private readonly Action _navigateForward;

    public int StudentCount => _inputConfiguration.StudentList?.Students.Count ?? 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsManualMode))]
    public partial bool IsPriorityMode { get; set; } = true;

    public bool IsManualMode
    {
        get => !IsPriorityMode;
        set => IsPriorityMode = !value;
    }

    [ObservableProperty]
    public partial string PriorityInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ManualSizesInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CalculatedResultText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public GroupSizingViewModel() : this(new InputConfiguration(), new NullDialogService(), static () => { }, static () => { })
    {
    }

    public GroupSizingViewModel(
        InputConfiguration inputConfiguration,
        IDialogService dialogService,
        Action navigateBack,
        Action navigateForward)
    {
        _inputConfiguration = inputConfiguration;
        _dialogService = dialogService;
        _navigateBack = navigateBack;
        _navigateForward = navigateForward;

        if (_inputConfiguration.GroupSizeDistribution is { } distribution)
        {
            var sizesText = FormatSizes(distribution.Sizes);
            ManualSizesInput = sizesText;
            CalculatedResultText = sizesText;
        }
    }

    [RelayCommand]
    private void Calculate()
    {
        ClearStatus();

        if (!TryGetStudentCount(out var studentCount))
        {
            return;
        }

        try
        {
            var priorities = GroupSizePriorities.Create(ParseCommaSeparatedInts(PriorityInput));
            var distribution = GroupSizesCalculator.DetermineGroupSizes(studentCount, priorities);
            CalculatedResultText = FormatSizes(distribution.Sizes);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void Back()
    {
        ClearStatus();
        _navigateBack();
    }

    [RelayCommand]
    private void Next()
    {
        ClearStatus();

        if (!TryGetStudentCount(out var studentCount))
        {
            return;
        }

        try
        {
            var sizes = IsPriorityMode
                ? ParseCommaSeparatedInts(CalculatedResultText)
                : ParseCommaSeparatedInts(ManualSizesInput);

            if (IsPriorityMode && string.IsNullOrWhiteSpace(CalculatedResultText))
            {
                ShowError("Calculate a group size distribution before continuing.");
                return;
            }

            _inputConfiguration.GroupSizeDistribution = GroupSizeDistribution.Create(sizes, studentCount);
            _navigateForward();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private bool TryGetStudentCount(out int studentCount)
    {
        studentCount = StudentCount;
        if (studentCount > 0)
        {
            return true;
        }

        ShowError("No students are configured. Go back and add students first.");
        return false;
    }

    private void ClearStatus() => StatusMessage = string.Empty;

    private void ShowError(string message) => _ = _dialogService.ShowErrorAsync(message);

    private static int[] ParseCommaSeparatedInts(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new FormatException("Enter at least one number.");
        }

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            throw new FormatException("Enter at least one number.");
        }

        var sizes = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out sizes[i]))
            {
                throw new FormatException($"\"{parts[i]}\" is not a valid number.");
            }
        }

        return sizes;
    }

    private static string FormatSizes(System.Collections.Generic.IReadOnlyList<int> sizes) =>
        string.Join(", ", sizes);
}
