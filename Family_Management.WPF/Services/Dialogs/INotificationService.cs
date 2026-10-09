using System.Windows;

namespace Family_Management.WPF.Services.Dialogs;

public interface INotificationService
{
    Task<MessageBoxResult> ShowDialogAsync(
        string message,
        string title,
        MessageBoxButton buttons,
        MessageBoxImage icon);

    Task ShowInformationAsync(
        string message,
        string title = "Informação");

    Task ShowWarningAsync(
        string message,
        string title = "Aviso");

    Task ShowErrorAsync(
        string message,
        string title = "Erro");

    Task<bool> ShowConfirmationAsync(
        string message,
        string title = "Confirmação");
}