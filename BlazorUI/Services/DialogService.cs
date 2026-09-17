namespace BlazorUI.Services;

public sealed class DialogService : IDialogService
{
    private TaskCompletionSource? _errorCompletion;
    private TaskCompletionSource<bool>? _confirmCompletion;

    public event Action? Changed;

    public bool IsVisible { get; private set; }

    public bool IsConfirm { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public Task ShowErrorAsync(string message)
    {
        CancelPending();
        Message = message;
        IsConfirm = false;
        IsVisible = true;
        _errorCompletion = new TaskCompletionSource();
        Changed?.Invoke();
        return _errorCompletion.Task;
    }

    public Task<bool> ShowConfirmAsync(string message)
    {
        CancelPending();
        Message = message;
        IsConfirm = true;
        IsVisible = true;
        _confirmCompletion = new TaskCompletionSource<bool>();
        Changed?.Invoke();
        return _confirmCompletion.Task;
    }

    public void CloseError()
    {
        if (!IsVisible || IsConfirm)
        {
            return;
        }

        IsVisible = false;
        Message = string.Empty;
        var completion = _errorCompletion;
        _errorCompletion = null;
        Changed?.Invoke();
        completion?.TrySetResult();
    }

    public void CloseConfirm(bool confirmed)
    {
        if (!IsVisible || !IsConfirm)
        {
            return;
        }

        IsVisible = false;
        Message = string.Empty;
        var completion = _confirmCompletion;
        _confirmCompletion = null;
        Changed?.Invoke();
        completion?.TrySetResult(confirmed);
    }

    private void CancelPending()
    {
        _errorCompletion?.TrySetResult();
        _confirmCompletion?.TrySetResult(false);
        _errorCompletion = null;
        _confirmCompletion = null;
    }
}
