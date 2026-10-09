using System.Collections;
using System.ComponentModel;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.Validation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Users.Commands.CreateUser;
using FamilyManagement.Application.UseCases.Users.Queries.IsUserNameUnique;
using FamilyManagement.Application.UseCases.Users.Queries.UserExistsByEmail;

namespace Family_Management.WPF.ViewModel.Authentication;

public sealed class RegisterViewModel :
    BaseViewModel,
    INotifyDataErrorInfo,
    IDisposable
{
    private readonly CreateUserHandler _createUserHandler;
    private readonly IsUserNameUniqueHandler _isUserNameUniqueHandler;
    private readonly UserExistsByEmailHandler _userExistsByEmailHandler;

    private readonly RegisterValidator _validator;

    private CancellationTokenSource? _validationCts;

    private string _userName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

    private bool _isDisposed;

    public RegisterViewModel(
        CreateUserHandler createUserHandler,
        IsUserNameUniqueHandler isUserNameUniqueHandler,
        UserExistsByEmailHandler userExistsByEmailHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _createUserHandler = createUserHandler
            ?? throw new ArgumentNullException(
                nameof(createUserHandler));

        _isUserNameUniqueHandler = isUserNameUniqueHandler
            ?? throw new ArgumentNullException(
                nameof(isUserNameUniqueHandler));

        _userExistsByEmailHandler = userExistsByEmailHandler
            ?? throw new ArgumentNullException(
                nameof(userExistsByEmailHandler));

        _validator = new RegisterValidator();

        _validator.ErrorsChanged += OnValidatorErrorsChanged;

        Title = "Criar conta";

        RegisterCommand = new AsyncRelayCommand(
            ExecuteRegisterAsync,
            CanExecuteRegister);

        NavigateBackCommand = new AsyncRelayCommand(
            NavigateBackAsync,
            () => !IsBusy);
    }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _validator.HasErrors;

    public string UserName
    {
        get => _userName;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(
                    ref _userName,
                    value))
            {
                return;
            }

            _validator.ValidateUserName(value);

            ScheduleAvailabilityValidation();

            RaiseCommandStates();
        }
    }

    public string Email
    {
        get => _email;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(
                    ref _email,
                    value))
            {
                return;
            }

            _validator.ValidateEmail(value);

            ScheduleAvailabilityValidation();

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

            _validator.ValidatePassword(value);

            _validator.ValidateConfirmPassword(
                value,
                ConfirmPassword);

            RaiseCommandStates();
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(
                    ref _confirmPassword,
                    value))
            {
                return;
            }

            _validator.ValidateConfirmPassword(
                Password,
                value);

            RaiseCommandStates();
        }
    }

    public AsyncRelayCommand RegisterCommand { get; }

    public AsyncRelayCommand NavigateBackCommand { get; }

    public IEnumerable GetErrors(
        string? propertyName)
    {
        return _validator.GetErrors(propertyName);
    }

    private bool CanExecuteRegister()
    {
        return !IsBusy
               && !HasErrors
               && !string.IsNullOrWhiteSpace(UserName)
               && !string.IsNullOrWhiteSpace(Email)
               && !string.IsNullOrWhiteSpace(Password)
               && !string.IsNullOrWhiteSpace(ConfirmPassword);
    }

    private async Task ExecuteRegisterAsync()
    {
        ValidateForm();

        if (HasErrors)
        {
            return;
        }

        CancelAvailabilityValidation();

        var userName = UserName.Trim();
        var email = Email.Trim();

        await RunSafeAsync(
            async () =>
            {
                //
                // Fazemos uma última verificação antes do registo.
                // O CreateUserHandler continua a ser a autoridade
                // final relativamente a duplicados.
                //
                var availabilityIsValid =
                    await ValidateAvailabilityForRegistrationAsync(
                        userName,
                        email);

                if (!availabilityIsValid)
                {
                    return;
                }

                var command = new CreateUserCommand(
                    userName,
                    email,
                    Password);

                var result =
                    await _createUserHandler.HandleAsync(
                        command);

                if (result.IsFailure)
                {
                    await NotificationService.ShowErrorAsync(
                        result.Error.Message,
                        "Erro no registo");

                    return;
                }

                ClearSensitiveData();

                await NotificationService.ShowInformationAsync(
                    "Conta criada com sucesso.",
                    "Sucesso");

                await NavigationService.NavigateToLoginView();
            });

        RaiseCommandStates();
    }

    private void ValidateForm()
    {
        _validator.ValidateUserName(
            UserName);

        _validator.ValidateEmail(
            Email);

        _validator.ValidatePassword(
            Password);

        _validator.ValidateConfirmPassword(
            Password,
            ConfirmPassword);
    }

    private void ScheduleAvailabilityValidation()
    {
        if (_isDisposed)
        {
            return;
        }

        CancelAvailabilityValidation();

        _validationCts =
            new CancellationTokenSource();

        _ = ValidateAvailabilityAfterDelayAsync(
            _validationCts.Token);
    }

    private async Task ValidateAvailabilityAfterDelayAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(
                400,
                cancellationToken);

            await ValidateAvailabilityAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Cancelamento esperado quando o utilizador
            // continua a escrever ou abandona o ecrã.
        }
        catch (ObjectDisposedException)
        {
            // O ViewModel foi libertado enquanto a
            // validação estava pendente.
        }
    }

    private async Task ValidateAvailabilityAsync(
        CancellationToken cancellationToken)
    {
        var userName =
            UserName.Trim();

        var email =
            Email.Trim();

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var userNameResult =
                await _isUserNameUniqueHandler.HandleAsync(
                    new IsUserNameUniqueQuery(
                        userName));

            cancellationToken.ThrowIfCancellationRequested();

            if (string.Equals(
                    userName,
                    UserName.Trim(),
                    StringComparison.Ordinal))
            {
                _validator.ValidateUserName(
                    UserName);

                if (userNameResult.IsSuccess &&
                    !userNameResult.Value)
                {
                    _validator.SetUserNameUnavailable();
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(email) &&
            ValidationHelper.IsValidEmail(email))
        {
            var emailResult =
                await _userExistsByEmailHandler.HandleAsync(
                    new UserExistsByEmailQuery(
                        email));

            cancellationToken.ThrowIfCancellationRequested();

            if (string.Equals(
                    email,
                    Email.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                _validator.ValidateEmail(
                    Email);

                if (emailResult.IsSuccess &&
                    emailResult.Value)
                {
                    _validator.SetEmailUnavailable();
                }
            }
        }

        RaiseCommandStates();
    }

    private async Task<bool>
        ValidateAvailabilityForRegistrationAsync(
            string userName,
            string email)
    {
        var userNameResult =
            await _isUserNameUniqueHandler.HandleAsync(
                new IsUserNameUniqueQuery(
                    userName));

        if (userNameResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                userNameResult.Error.Message,
                "Validação");

            return false;
        }

        if (!userNameResult.Value)
        {
            _validator.SetUserNameUnavailable();
            return false;
        }

        var emailResult =
            await _userExistsByEmailHandler.HandleAsync(
                new UserExistsByEmailQuery(
                    email));

        if (emailResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                emailResult.Error.Message,
                "Validação");

            return false;
        }

        if (emailResult.Value)
        {
            _validator.SetEmailUnavailable();
            return false;
        }

        return true;
    }

    private Task NavigateBackAsync()
    {
        CancelAvailabilityValidation();

        return NavigationService.NavigateToLoginView();
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

    private void RaiseCommandStates()
    {
        RegisterCommand.RaiseCanExecuteChanged();
        NavigateBackCommand.RaiseCanExecuteChanged();
    }

    private void CancelAvailabilityValidation()
    {
        var cts =
            Interlocked.Exchange(
                ref _validationCts,
                null);

        if (cts is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        finally
        {
            cts.Dispose();
        }
    }

    private void ClearSensitiveData()
    {
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        CancelAvailabilityValidation();

        _validator.ErrorsChanged -=
            OnValidatorErrorsChanged;

        GC.SuppressFinalize(this);
    }
}