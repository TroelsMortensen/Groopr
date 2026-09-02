using System.Globalization;
using Logic.Models;

namespace AvaloniaUI.ViewModels.GroupCompositionGeneration;

public partial class GroupMemberViewModel
{
    public string DisplayText { get; }

    private GroupMemberViewModel(string displayText) => DisplayText = displayText;

    public static GroupMemberViewModel FromStudent(Student student)
    {
        var name = string.IsNullOrWhiteSpace(student.Name) ? "Unknown" : student.Name;
        return new GroupMemberViewModel($"{student.Number}, {name}");
    }
}
