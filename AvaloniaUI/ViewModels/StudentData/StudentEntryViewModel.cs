using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Logic.Models;

namespace AvaloniaUI.ViewModels.StudentData;

public partial class StudentEntryViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Number { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial List<string> PositiveWishes { get; set; } = [];

    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(Name) ? $"#{Number}" : $"#{Number} - {Name}";

    partial void OnNumberChanged(string value) => OnPropertyChanged(nameof(DisplayLabel));

    partial void OnNameChanged(string? value) => OnPropertyChanged(nameof(DisplayLabel));

    public Student ToStudent() => Student.Create(Number, PositiveWishes, Name);

    public static StudentEntryViewModel FromStudent(Student student) => new()
    {
        Number = student.Number,
        Name = student.Name,
        PositiveWishes = student.PositiveWishes.Select(number => number.Value).ToList(),
    };
}
