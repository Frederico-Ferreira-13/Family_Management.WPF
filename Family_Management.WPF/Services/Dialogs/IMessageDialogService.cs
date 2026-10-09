using System.Windows;

namespace Family_Management.WPF.Services.Dialogs;

public interface IMessageDialogService
{
    Task<MessageBoxResult> ShowAsync(
        string message,
        string title,
        MessageBoxButton buttons,
        MessageBoxImage icon);

    Task ShowInfoAsync(
        string message,
        string title = "Informação");

    Task ShowErrorAsync(
        string message,
        string title = "Erro");

    Task<bool> ConfirmAsync(
        string message,
        string title = "Confirmação");
}