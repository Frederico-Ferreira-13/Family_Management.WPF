using System.Collections;
using System.ComponentModel;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.Validation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Authentication.Commands.Login;
using FamilyManagement.Application.UseCases.Setup.Interfaces;

namespace Family_Management.WPF.ViewModel.Authentication;

public sealed class LoginViewModel :
    BaseViewModel,
    INotifyDataErrorInfo
{
    private readonly LoginHandler _loginHandler;
    private readonly CurrentUserService _currentUserService;
    private readonly ISetupService _setupService;
    private readonly LoginValidator _validator;

    private string _emailOrUsername = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(
        LoginHandler loginHandler,
        CurrentUserService currentUserService,
        ISetupService setupService,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _loginHandler = loginHandler
            ?? throw new ArgumentNullException(nameof(loginHandler));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _setupService = setupService
            ?? throw new ArgumentNullException(nameof(setupService));

        _validator = new LoginValidator();

        _validator.ErrorsChanged +=
            OnValidatorErrorsChanged;

        Title = "Login";

        LoginCommand = new AsyncRelayCommand(
            ExecuteLoginAsync,
            CanExecuteLogin);

        NavigateToRegisterCommand =
            new AsyncRelayCommand(
                NavigateToRegisterAsync,
                CanNavigateToRegister);
    }

    public event EventHandler<DataErrorsChangedEventArgs>?
        ErrorsChanged;

    public bool HasErrors =>
        _validator.HasErrors;

    public string EmailOrUsername
    {
        get => _emailOrUsername;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(
                    ref _emailOrUsername,
                    value))
            {
                return;
            }

            _validator.ValidateEmailOrUsername(
                value);

            ClearAuthenticationError();

            RaiseCommandStates();
        }
    }

    public string Password
    {
        get => _password;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(
                    ref _password,
                    value))
            {
                return;
            }

            _validator.ValidatePassword(
                value);

            ClearAuthenticationError();

            RaiseCommandStates();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;

        private set
        {
            value ??= string.Empty;

            SetProperty(
                ref _errorMessage,
                value);
        }
    }

    public AsyncRelayCommand LoginCommand { get; }

    public AsyncRelayCommand NavigateToRegisterCommand { get; }

    public IEnumerable GetErrors(
        string? propertyName)
    {
        return _validator.GetErrors(
            propertyName);
    }

    private bool CanExecuteLogin()
    {
        return !IsBusy
               && !HasErrors
               && !string.IsNullOrWhiteSpace(
                   EmailOrUsername)
               && !string.IsNullOrWhiteSpace(
                   Password);
    }

    private bool CanNavigateToRegister()
    {
        return !IsBusy;
    }

    private async Task ExecuteLoginAsync()
    {
        ValidateForm();

        if (HasErrors)
        {
            ErrorMessage =
                GetFirstError(null);

            RaiseCommandStates();

            return;
        }

        await RunSafeAsync(async () =>
        {
            ErrorMessage =
                string.Empty;

            // Garante que uma sessão anterior não é reutilizada
            // numa nova tentativa de autenticação.
            _currentUserService.Clear();

            var command =
                new LoginCommand(
                    EmailOrUsername.Trim(),
                    Password);

            var result =
                await _loginHandler.HandleAsync(
                    command);

            if (result.IsFailure)
            {
                ErrorMessage =
                    result.Error.Message;

                return;
            }

            var user =
                result.Value.User;

            if (user.Id == Guid.Empty)
            {
                ErrorMessage =
                    "Não foi possível identificar o utilizador autenticado.";

                return;
            }

            _currentUserService.SetCurrentUser(
                user.Id);

            Password =
                string.Empty;

            /*
             * O utilizador ainda não pertence a uma família.
             *
             * FamilySetup permite criar uma família
             * ou aderir a uma existente.
             */
            if (!user.FamilyId.HasValue ||
                user.FamilyId.Value == Guid.Empty)
            {
                await NotificationService
                    .ShowInformationAsync(
                        "Ainda não pertence a nenhuma família. " +
                        "Crie uma nova ou peça para ser convidado.",
                        "Bem-vindo");

                await NavigationService
                    .NavigateToScreen(
                        AppScreen.FamilySetup);

                return;
            }

            var setupResult =
                await _setupService
                    .IsFamilySetupCompleteAsync(
                        user.FamilyId.Value,
                        user.Id);

            if (setupResult.IsFailure)
            {
                _currentUserService.Clear();

                ErrorMessage =
                    setupResult.Error.Message;

                await NotificationService
                    .ShowErrorAsync(
                        setupResult.Error.Message,
                        "Configuração");

                return;
            }

            /*
             * Família configurada:
             *      Dashboard
             *
             * Família ainda sem configuração financeira mínima:
             *      Accounts
             */
            await NavigationService
                .NavigateToScreen(
                    setupResult.Value
                        ? AppScreen.Dashboard
                        : AppScreen.Accounts);
        });

        RaiseCommandStates();
    }

    private Task NavigateToRegisterAsync()
    {
        return NavigationService
            .NavigateToRegisterView();
    }

    private void ValidateForm()
    {
        _validator.ValidateEmailOrUsername(
            EmailOrUsername);

        _validator.ValidatePassword(
            Password);
    }

    private void ClearAuthenticationError()
    {
        if (!string.IsNullOrEmpty(
                ErrorMessage))
        {
            ErrorMessage =
                string.Empty;
        }
    }

    private void OnValidatorErrorsChanged(
        object? sender,
        DataErrorsChangedEventArgs e)
    {
        ErrorsChanged?.Invoke(
            this,
            e);

        OnPropertyChanged(
            nameof(HasErrors));

        RaiseCommandStates();
    }

    private string GetFirstError(
        string? propertyName)
    {
        return GetErrors(propertyName)
                   .Cast<string>()
                   .FirstOrDefault()
               ?? string.Empty;
    }

    private void RaiseCommandStates()
    {
        LoginCommand
            .RaiseCanExecuteChanged();

        NavigateToRegisterCommand
            .RaiseCanExecuteChanged();
    }
}