using AvaloniaUI.Data;
using AvaloniaUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly InputConfiguration _inputConfiguration = new();
    private readonly DialogService _dialogService = new();

    [ObservableProperty]
    public partial ViewModelBase? CurrentViewModel { get; set; }

    public MainViewModel()
    {
        CurrentViewModel = new StudentData.StudentDataViewModel(
            _inputConfiguration,
            _dialogService,
            new StudentCsvFileService());
    }
}
