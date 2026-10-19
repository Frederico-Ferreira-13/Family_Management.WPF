
using System;
using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.Validation;
using Family_Management.WPF.ViewModel.Common;

using FamilyManagement.Application.UseCases.Users.Commands.AssignUserToFamily;
using FamilyManagement.Application.UseCases.Users.Commands.CreateUser;
using FamilyManagement.Application.UseCases.Users.Queries.GetUserById;

namespace Family_Management.WPF.ViewModel.Families;

public sealed class FamilyMemberCreateViewModel :
    BaseViewModel,
    INotifyDataErrorInfo
{
    private readonly CurrentUserService _currentUserService;
    private readonly GetUserByIdHandler _getUserByIdHandler;
    private readonly CreateUserHandler _createUserHandler;
    private readonly AssignUserToFamilyHandler _assignUserToFamilyHandler;

    private readonly RegisterValidator _validator = new();

    private string _userName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

    public FamilyMemberCreateViewModel(
        CurrentUserService currentUserService,
        GetUserByIdHandler getUserByIdHandler,
        CreateUserHandler createUserHandler,
        AssignUserToFamilyHandler assignUserToFamilyHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(navigationService, notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _getUserByIdHandler = getUserByIdHandler
            ?? throw new ArgumentNullException(
                nameof(getUserByIdHandler));

        _createUserHandler = createUserHandler
            ?? throw new ArgumentNullException(
                nameof(createUserHandler));

        _assignUserToFamilyHandler = assignUserToFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(assignUserToFamilyHandler));

        Title = "Adicionar membro à família";

        _validator.ErrorsChanged += OnValidatorErrorsChanged;

        SaveCommand = new AsyncRelayCommand(
            ExecuteSaveAsync,
            CanExecuteSave);

        CancelCommand = new AsyncRelayCommand(
                ExecuteCancelAsync,
                () => !IsBusy);
    }

    // ==========================================
    // VALIDAÇÃO
    // ==========================================

    public event EventHandler<DataErrorsChangedEventArgs>?
        ErrorsChanged;

    public bool HasErrors => _validator.HasErrors;

    public IEnumerable GetErrors(string? propertyName)
    {
        return _validator.GetErrors(propertyName);
    }

    // ==========================================
    // CAMPOS
    // ==========================================

    public string UserName
    {
        get => _userName;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(ref _userName, value))
                return;

            _validator.ValidateUserName(value);

            RaiseSaveCanExecuteChanged();
        }
    }

    public string Email
    {
        get => _email;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(ref _email, value))
                return;

            _validator.ValidateEmail(value);

            RaiseSaveCanExecuteChanged();
        }
    }

    public string Password
    {
        get => _password;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(ref _password, value))
                return;

            _validator.ValidatePassword(value);

            _validator.ValidateConfirmPassword(
                value,
                ConfirmPassword);

            RaiseSaveCanExecuteChanged();
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(ref _confirmPassword, value))
                return;

            _validator.ValidateConfirmPassword(
                Password,
                value);

            RaiseSaveCanExecuteChanged();
        }
    }

    // ==========================================
    // COMANDOS
    // ==========================================

    public ICommand SaveCommand { get; }

    public AsyncRelayCommand CancelCommand { get; }

    // ==========================================
    // GUARDAR
    // ==========================================

    private bool CanExecuteSave()
    {
        return !IsBusy
            && !HasErrors
            && !string.IsNullOrWhiteSpace(UserName)
            && !string.IsNullOrWhiteSpace(Email)
            && !string.IsNullOrWhiteSpace(Password)
            && !string.IsNullOrWhiteSpace(ConfirmPassword);
    }

    private async Task ExecuteSaveAsync()
    {
        ValidateForm();

        if (!CanExecuteSave())
            return;

        var currentUserId = _currentUserService.UserId;

        if (!_currentUserService.IsAuthenticated ||
            !currentUserId.HasValue ||
            currentUserId.Value == Guid.Empty)
        {
            await NotificationService.ShowErrorAsync(
                "É necessário iniciar sessão para adicionar membros.",
                "Adicionar membro");

            return;
        }

        await RunSafeAsync(async () =>
        {
            // 1. Obter a família do utilizador autenticado.

            var currentUserResult =
                await _getUserByIdHandler.HandleAsync(
                    new GetUserByIdQuery(currentUserId.Value));

            if (currentUserResult.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    currentUserResult.Error.Message,
                    "Adicionar membro");

                return;
            }

            var familyId = currentUserResult.Value.FamilyId;

            if (!familyId.HasValue ||
                familyId.Value == Guid.Empty)
            {
                await NotificationService.ShowErrorAsync(
                    "O seu utilizador não está associado a uma família. " +
                    "Configure primeiro o agregado familiar.",
                    "Adicionar membro");

                return;
            }

            // 2. Criar o novo utilizador.

            var createResult =
                await _createUserHandler.HandleAsync(
                    new CreateUserCommand(
                        UserName.Trim(),
                        Email.Trim(),
                        Password));

            if (createResult.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    createResult.Error.Message,
                    "Criar utilizador");

                return;
            }

            // 3. Associar o utilizador à família atual.

            var assignResult =
                await _assignUserToFamilyHandler.HandleAsync(
                    new AssignUserToFamilyCommand(
                        createResult.Value.Id,
                        familyId.Value));

            if (assignResult.IsFailure)
            {
                ClearSensitiveData();

                await NotificationService.ShowErrorAsync(
                    "A conta foi criada, mas não foi possível " +
                    "associá-la à família.\n\n" +
                    assignResult.Error.Message +
                    "\n\nNão repita o registo. " +
                    "Associe o utilizador existente à família " +
                    "através da gestão de utilizadores.",
                    "Associação à família");

                return;
            }

            // 4. Limpar dados sensíveis.

            ClearSensitiveData();

            // 5. Confirmar sucesso.

            await NotificationService.ShowInformationAsync(
                "O novo membro foi criado e associado " +
                "à família com sucesso.",
                "Membro adicionado");

            // 6. Regressar sem alterar a sessão atual.

            NavigationService.GoBack();
        });

        RaiseSaveCanExecuteChanged();
    }

    // ==========================================
    // VALIDAR FORMULÁRIO
    // ==========================================

    private void ValidateForm()
    {
        _validator.ValidateUserName(UserName);

        _validator.ValidateEmail(Email);

        _validator.ValidatePassword(Password);

        _validator.ValidateConfirmPassword(
            Password,
            ConfirmPassword);
    }

    // ==========================================
    // CANCELAR
    // ==========================================

    private Task ExecuteCancelAsync()
    {
        ClearSensitiveData();

        NavigationService.GoBack();

        return Task.CompletedTask;
    }

    // ==========================================
    // LIMPAR DADOS SENSÍVEIS
    // ==========================================

    private void ClearSensitiveData()
    {
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }

    // ==========================================
    // EVENTOS DE VALIDAÇÃO
    // ==========================================

    private void OnValidatorErrorsChanged(
        object? sender,
        DataErrorsChangedEventArgs e)
    {
        ErrorsChanged?.Invoke(this, e);

        OnPropertyChanged(nameof(HasErrors));

        RaiseSaveCanExecuteChanged();
    }

    private void RaiseSaveCanExecuteChanged()
    {
        if (SaveCommand is AsyncRelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }
}
