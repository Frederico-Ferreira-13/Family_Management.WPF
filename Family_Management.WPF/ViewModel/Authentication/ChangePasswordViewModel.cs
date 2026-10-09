using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Users.Commands.ChangePassword;

namespace Family_Management.WPF.ViewModel.Authentication;

public sealed class ChangePasswordViewModel : BaseViewModel
{
    private const int MinimumPasswordLength = 8;

    private readonly ChangePasswordHandler _changePasswordHandler;
    private readonly CurrentUserService _currentUserService;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmNewPassword = string.Empty;

    public ChangePasswordViewModel(
        ChangePasswordHandler changePasswordHandler,
        CurrentUserService currentUserService,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _changePasswordHandler = changePasswordHandler
            ?? throw new ArgumentNullException(
                nameof(changePasswordHandler));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        Title = "Alterar palavra-passe";

        ChangePasswordCommand =
            new AsyncRelayCommand(
                ChangePasswordAsync,
                CanChangePassword);

        CancelCommand =
            new RelayCommand(
                Cancel);
    }

    public string CurrentPassword
    {
        get => _currentPassword;

        set
        {
            if (!SetProperty(
                    ref _currentPassword,
                    value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(
                nameof(IsNewPasswordDifferent));

            RaiseCommandStates();
        }
    }

    public string NewPassword
    {
        get => _newPassword;

        set
        {
            if (!SetProperty(
                    ref _newPassword,
                    value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(
                nameof(PasswordsMatch),
                nameof(IsNewPasswordLongEnough),
                nameof(IsNewPasswordDifferent));

            RaiseCommandStates();
        }
    }

    public string ConfirmNewPassword
    {
        get => _confirmNewPassword;

        set
        {
            if (!SetProperty(
                    ref _confirmNewPassword,
                    value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(
                nameof(PasswordsMatch));

            RaiseCommandStates();
        }
    }

    public bool PasswordsMatch =>
        !string.IsNullOrEmpty(NewPassword) &&
        NewPassword == ConfirmNewPassword;

    public bool IsNewPasswordLongEnough =>
        NewPassword.Length >= MinimumPasswordLength;

    public bool IsNewPasswordDifferent =>
        !string.IsNullOrEmpty(NewPassword) &&
        NewPassword != CurrentPassword;

    public ICommand ChangePasswordCommand { get; }

    public ICommand CancelCommand { get; }

    public event Action? CloseRequest;

    private bool CanChangePassword()
    {
        return !IsBusy &&
               _currentUserService.IsAuthenticated &&
               !string.IsNullOrWhiteSpace(CurrentPassword) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               NewPassword.Length >= MinimumPasswordLength &&
               NewPassword != CurrentPassword &&
               PasswordsMatch;
    }

    private async Task ChangePasswordAsync()
    {
        if (!CanChangePassword())
        {
            return;
        }

        var userId =
            _currentUserService.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            await NotificationService.ShowErrorAsync(
                "Não existe um utilizador autenticado.",
                "Sessão");

            return;
        }

        await RunSafeAsync(async () =>
        {
            var command =
                new ChangePasswordCommand(
                    UserId: userId.Value,
                    CurrentPassword: CurrentPassword,
                    NewPassword: NewPassword);

            var result =
                await _changePasswordHandler.HandleAsync(
                    command);

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Alterar palavra-passe");

                return;
            }

            ClearFields();

            await NotificationService.ShowInformationAsync(
                "A palavra-passe foi alterada com sucesso.",
                "Sucesso");

            CloseRequest?.Invoke();
        });

        RaiseCommandStates();
    }

    private void Cancel()
    {
        ClearFields();

        CloseRequest?.Invoke();
    }

    private void ClearFields()
    {
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmNewPassword = string.Empty;
    }

    private void RaiseCommandStates()
    {
        if (ChangePasswordCommand
            is AsyncRelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }
}