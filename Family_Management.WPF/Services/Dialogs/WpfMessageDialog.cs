using System.Windows;
using Family_Management.WPF.Services.Threading;

namespace Family_Management.WPF.Services.Dialogs;

public sealed class WpfMessageDialog : IMessageDialogService
{
    private readonly IDispatcherService _dispatcherService;

    public WpfMessageDialog(
        IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService
            ?? throw new ArgumentNullException(
                nameof(dispatcherService));
    }

    public Task<MessageBoxResult> ShowAsync(
        string message,
        string title,
        MessageBoxButton buttons,
        MessageBoxImage icon)
    {
        return _dispatcherService.InvokeAsync(() =>
            MessageBox.Show(
                message,
                title,
                buttons,
                icon));
    }

    public async Task ShowInfoAsync(
        string message,
        string title = "Informação")
    {
        await ShowAsync(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public async Task ShowErrorAsync(
        string message,
        string title = "Erro")
    {
        await ShowAsync(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public async Task<bool> ConfirmAsync(
        string message,
        string title = "Confirmação")
    {
        var result = await ShowAsync(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }
}