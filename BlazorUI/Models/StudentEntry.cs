using Logic.Models;

namespace BlazorUI.Models;

public class StudentEntry
{
    public string Number { get; set; } = string.Empty;
    public string? Name { get; set; }
    public List<string> PositiveWishes { get; set; } = [];
    public List<string> PreviousGroupMembers { get; set; } = [];
    public List<string> NegativeWishes { get; set; } = [];

    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(Name) ? $"#{Number}" : $"#{Number} - {Name}";

    public Student ToStudent() => Student.Create(
        Number,
        PositiveWishes.Select(StudentNumber.Create).ToList(),
        Name,
        PreviousGroupMembers.Select(StudentNumber.Create).ToList(),
        NegativeWishes.Select(StudentNumber.Create).ToList());

    public static StudentEntry FromStudent(Student student) => new()
    {
        Number = student.Number,
        Name = student.Name,
        PositiveWishes = student.PositiveWishes.Select(number => number.Value).ToList(),
        PreviousGroupMembers = student.PreviousGroupMembers.Select(number => number.Value).ToList(),
        NegativeWishes = student.NegativeWishes.Select(number => number.Value).ToList(),
    };
}
