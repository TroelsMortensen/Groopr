using AvaloniaUI.Data;
using AvaloniaUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly InputConfiguration _inputConfiguration = new();
    private readonly DialogService _dialogService = new();
    private readonly ClipboardService _clipboardService = new();

    [ObservableProperty]
    public partial ViewModelBase? CurrentViewModel { get; set; }

    public MainViewModel()
    {
        NavigateToStudentData();
    }

    private void NavigateToStudentData()
    {
        CurrentViewModel = new StudentData.StudentDataViewModel(
            _inputConfiguration,
            _dialogService,
            new StudentCsvFileService(),
            NavigateToGroupSizing);
    }

    private void NavigateToGroupSizing()
    {
        CurrentViewModel = new GroupSizing.GroupSizingViewModel(
            _inputConfiguration,
            _dialogService,
            NavigateToStudentData,
            NavigateToScorerSetup);
    }

    private void NavigateToScorerSetup()
    {
        CurrentViewModel = new ScorerSetup.ScorerSetupViewModel(
            _inputConfiguration,
            _dialogService,
            NavigateToGroupSizing,
            NavigateToInvalidationSetup);
    }

    private void NavigateToInvalidationSetup()
    {
        CurrentViewModel = new InvalidationSetup.InvalidationSetupViewModel(
            _inputConfiguration,
            _dialogService,
            NavigateToScorerSetup,
            NavigateToGroupCompositionGeneration);
    }

    private void NavigateToGroupCompositionGeneration()
    {
        CurrentViewModel = new GroupCompositionGeneration.GroupCompositionGenerationViewModel(
            _inputConfiguration,
            _dialogService,
            _clipboardService,
            NavigateToInvalidationSetup);
    }
}
