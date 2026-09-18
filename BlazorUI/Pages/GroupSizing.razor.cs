using BlazorUI.Data;
using BlazorUI.Services;
using Logic.GroupSizing;
using Logic.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Pages;

public partial class GroupSizing
{
    [Inject] private InputConfiguration InputConfiguration { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private int StudentCount => InputConfiguration.StudentList?.Students.Count ?? 0;
    private bool IsPriorityMode { get; set; } = true;
    private bool IsManualMode => !IsPriorityMode;
    private string PriorityInput { get; set; } = string.Empty;
    private string ManualSizesInput { get; set; } = string.Empty;
    private string CalculatedResultText { get; set; } = string.Empty;
    private string StatusMessage { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        if (InputConfiguration.StudentList is null)
        {
            Navigation.NavigateTo("students", replace: true);
            return;
        }

        if (InputConfiguration.GroupSizeDistribution is { } distribution)
        {
            var sizesText = FormatSizes(distribution.Sizes);
            ManualSizesInput = sizesText;
            CalculatedResultText = sizesText;
        }
    }

    private void SetPriorityMode(bool priorityMode) => IsPriorityMode = priorityMode;

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
            _ = DialogService.ShowErrorAsync(ex.Message);
        }
    }

    private void Back()
    {
        ClearStatus();
        Navigation.NavigateTo("students");
    }

    private void Next()
    {
        ClearStatus();

        if (!TryGetStudentCount(out var studentCount))
        {
            return;
        }

        try
        {
            if (IsPriorityMode && string.IsNullOrWhiteSpace(CalculatedResultText))
            {
                _ = DialogService.ShowErrorAsync("Calculate a group size distribution before continuing.");
                return;
            }

            var sizes = IsPriorityMode
                ? ParseCommaSeparatedInts(CalculatedResultText)
                : ParseCommaSeparatedInts(ManualSizesInput);

            InputConfiguration.GroupSizeDistribution = GroupSizeDistribution.Create(sizes, studentCount);
            Navigation.NavigateTo("scorer-setup");
        }
        catch (Exception ex)
        {
            _ = DialogService.ShowErrorAsync(ex.Message);
        }
    }

    private bool TryGetStudentCount(out int studentCount)
    {
        studentCount = StudentCount;
        if (studentCount > 0)
        {
            return true;
        }

        _ = DialogService.ShowErrorAsync("No students are configured. Go back and add students first.");
        return false;
    }

    private void ClearStatus() => StatusMessage = string.Empty;

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

    private static string FormatSizes(IReadOnlyList<int> sizes) => string.Join(", ", sizes);
}
