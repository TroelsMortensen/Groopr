using System;
using AvaloniaUI.Data;
using AvaloniaUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaUI.ViewModels.InvalidationSetup;

public partial class InvalidationSetupViewModel : ViewModelBase
{
    private readonly InputConfiguration _inputConfiguration;
    private readonly IDialogService _dialogService;
    private readonly Action _navigateBack;
    private readonly Action _navigateForward;

    public InvalidationSetupViewModel() : this(new InputConfiguration(), new NullDialogService(), static () => { }, static () => { })
    {
    }

    public InvalidationSetupViewModel(
        InputConfiguration inputConfiguration,
        IDialogService dialogService,
        Action navigateBack,
        Action navigateForward)
    {
        _inputConfiguration = inputConfiguration;
        _dialogService = dialogService;
        _navigateBack = navigateBack;
        _navigateForward = navigateForward;
    }

    [RelayCommand]
    private void Back()
    {
        _navigateBack();
    }

    [RelayCommand]
    private void Next()
    {
        _navigateForward();
    }
}
