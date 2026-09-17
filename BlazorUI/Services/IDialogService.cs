namespace BlazorUI.Services;

public interface IDialogService
{
    Task ShowErrorAsync(string message);

    Task<bool> ShowConfirmAsync(string message);
}
