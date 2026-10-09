using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;

namespace Family_Management.WPF.ViewModel.Shell;

public sealed class AddNewSelectionViewModel : BaseViewModel
{
    public AddNewSelectionViewModel(
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        Title = "Adicionar";

        NavigateToNewItemCommand =
            new AsyncRelayCommand<AppScreen>(
                ExecuteNavigateToNewItemAsync);

        CloseCommand =
            new RelayCommand(Close);
    }

    public ICommand NavigateToNewItemCommand { get; }

    public ICommand CloseCommand { get; }

    private async Task ExecuteNavigateToNewItemAsync(
        AppScreen screen)
    {
        try
        {
            NavigationService.CloseModal();

            await NavigationService.NavigateToScreen(
                screen);
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync(
                $"Não foi possível abrir o formulário selecionado. {ex.Message}",
                "Navegação");
        }
    }

    private void Close()
    {
        NavigationService.CloseModal();
    }
}