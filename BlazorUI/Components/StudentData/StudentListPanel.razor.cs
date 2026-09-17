using BlazorUI.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorUI.Components.StudentData;

public partial class StudentListPanel
{
    [Parameter] public IReadOnlyList<StudentEntry> Students { get; set; } = [];
    [Parameter] public StudentEntry? SelectedStudent { get; set; }
    [Parameter] public EventCallback<StudentEntry> OnSelect { get; set; }
    [Parameter] public EventCallback<StudentEntry> OnDelete { get; set; }
}
