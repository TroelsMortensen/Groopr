using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUI.Views.Dialogs;

public partial class ErrorDialog : Window
{
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ErrorDialog, string>(nameof(Message));

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ErrorDialog()
    {
        InitializeComponent();
    }

    private void OnOkayClick(object? sender, RoutedEventArgs e) => Close();
}
