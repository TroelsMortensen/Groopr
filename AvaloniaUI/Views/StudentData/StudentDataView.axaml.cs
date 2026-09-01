using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaUI.ViewModels.StudentData;

namespace AvaloniaUI.Views.StudentData;

public partial class StudentDataView : UserControl
{
    public StudentDataView()
    {
        InitializeComponent();
    }

    private void StudentRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Panel { DataContext: StudentEntryViewModel entry })
        {
            return;
        }

        if (DataContext is not StudentDataViewModel viewModel)
        {
            return;
        }

        viewModel.SelectStudentCommand.Execute(entry);
        e.Handled = true;
    }
}
