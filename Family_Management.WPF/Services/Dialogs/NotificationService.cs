using System.Windows;
using Family_Management.WPF.Services.Threading;

namespace Family_Management.WPF.Services.Dialogs;

public sealed class NotificationService : INotificationService
{
    private readonly IDispatcherService _dispatcherService;

    public NotificationService(
        IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService
            ?? throw new ArgumentNullException(
                nameof(dispatcherService));
    }

    public Task<MessageBoxResult> ShowDialogAsync(
        string message,
        string title,
        MessageBoxButton buttons,
        MessageBoxImage icon)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return _dispatcherService.InvokeAsync(() =>
            MessageBox.Show(
                message,
                title,
                buttons,
                icon));
    }

    public async Task ShowInformationAsync(
        string message,
        string title = "Informação")
    {
        await ShowDialogAsync(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public async Task ShowWarningAsync(
        string message,
        string title = "Aviso")
    {
        await ShowDialogAsync(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    public async Task ShowErrorAsync(
        string message,
        string title = "Erro")
    {
        await ShowDialogAsync(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public async Task<bool> ShowConfirmationAsync(
        string message,
        string title = "Confirmação")
    {
        var result = await ShowDialogAsync(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }
}