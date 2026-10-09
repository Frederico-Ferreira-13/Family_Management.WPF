using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.Validation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Families.DTOs;
using FamilyManagement.Application.UseCases.Families.Queries.GetActiveFamilies;
using FamilyManagement.Application.UseCases.Users.Commands.AssignUserToFamily;
using FamilyManagement.Application.UseCases.Users.Commands.ChangeUserFamily;
using FamilyManagement.Application.UseCases.Users.Commands.RemoveUserFromFamily;
using FamilyManagement.Application.UseCases.Users.Commands.UpdateUser;
using FamilyManagement.Application.UseCases.Users.DTOs;

namespace Family_Management.WPF.ViewModel.Users;

public sealed class UserEditViewModel :
    BaseViewModel,
    INotifyDataErrorInfo,
    IParameterReceiver<UserDTO>
{
    private readonly UpdateUserHandler _updateUserHandler;

    private readonly AssignUserToFamilyHandler
        _assignUserToFamilyHandler;

    private readonly RemoveUserFromFamilyHandler
        _removeUserFromFamilyHandler;

    private readonly ChangeUserFamilyHandler
        _changeUserFamilyHandler;

    private readonly GetActiveFamiliesHandler
        _getActiveFamiliesHandler;

    private readonly UserEditValidator _validator = new();

    private UserDTO? _originalUser;

    private string _userName = string.Empty;
    private string _email = string.Empty;
    private FamilyDTO? _selectedFamily;

    public UserEditViewModel(
        UpdateUserHandler updateUserHandler,
        AssignUserToFamilyHandler assignUserToFamilyHandler,
        RemoveUserFromFamilyHandler removeUserFromFamilyHandler,
        ChangeUserFamilyHandler changeUserFamilyHandler,
        GetActiveFamiliesHandler getActiveFamiliesHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _updateUserHandler = updateUserHandler
            ?? throw new ArgumentNullException(
                nameof(updateUserHandler));

        _assignUserToFamilyHandler = assignUserToFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(assignUserToFamilyHandler));

        _removeUserFromFamilyHandler = removeUserFromFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(removeUserFromFamilyHandler));

        _changeUserFamilyHandler = changeUserFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(changeUserFamilyHandler));

        _getActiveFamiliesHandler = getActiveFamiliesHandler
            ?? throw new ArgumentNullException(
                nameof(getActiveFamiliesHandler));

        _validator.ErrorsChanged +=
            OnValidatorErrorsChanged;

        Title = "Editar utilizador";

        SaveCommand = new AsyncRelayCommand(
            ExecuteSaveAsync,
            CanExecuteSave);

        CancelCommand = new RelayCommand(
            ExecuteCancel);
    }

    public event EventHandler<DataErrorsChangedEventArgs>?
        ErrorsChanged;

    public ObservableCollection<FamilyDTO>
        AvailableFamilies { get; } = new();

    public Guid Id =>
        _originalUser?.Id ?? Guid.Empty;

    public DateTime? CreatedAt =>
        _originalUser?.CreatedAt;

    public bool IsActive =>
        _originalUser?.IsActive ?? false;

    public bool HasErrors =>
        _validator.HasErrors;

    public string UserName
    {
        get => _userName;

        set
        {
            if (!SetProperty(
                ref _userName,
                value ?? string.Empty))
            {
                return;
            }

            _validator.ValidateUserName(
                nameof(UserName),
                value);

            RaiseSaveCanExecuteChanged();
        }
    }

    public string Email
    {
        get => _email;

        set
        {
            if (!SetProperty(
                    ref _email,
                    value ?? string.Empty))
            {
                return;
            }

            _validator.ValidateEmail(
                nameof(Email),
                value);

            RaiseSaveCanExecuteChanged();
        }
    }

    public FamilyDTO? SelectedFamily
    {
        get => _selectedFamily;

        set
        {
            if (!SetProperty(
                    ref _selectedFamily,
                    value))
            {
                return;
            }

            RaiseSaveCanExecuteChanged();
        }
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public IEnumerable GetErrors(
        string? propertyName)
    {
        return _validator.GetErrors(
            propertyName);
    }

    public void ReceiveParameter(
        UserDTO parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        _originalUser = parameter;

        _userName = parameter.UserName;
        _email = parameter.Email;

        OnPropertyChanged(
            nameof(Id),
            nameof(CreatedAt),
            nameof(IsActive),
            nameof(UserName),
            nameof(Email));

        Title = $"Editar utilizador - {parameter.UserName}";

        RaiseSaveCanExecuteChanged();
    }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (_originalUser is null)
        {
            await NotificationService.ShowErrorAsync(
                "Não foi indicado o utilizador a editar.",
                "Editar utilizador");

            return;
        }

        await RunSafeAsync(
            async () =>
            {
                var result =
                    await _getActiveFamiliesHandler.HandleAsync(
                        new GetActiveFamiliesQuery(),
                        cancellationToken);

                if (result.IsFailure)
                {
                    await NotificationService.ShowErrorAsync(
                        result.Error.Message,
                        "Famílias");

                    return;
                }

                AvailableFamilies.Clear();

                foreach (var family in result.Value)
                {
                    AvailableFamilies.Add(family);
                }

                _selectedFamily =
                    _originalUser.FamilyId.HasValue
                        ? AvailableFamilies.FirstOrDefault(
                            family =>
                                family.Id ==
                                _originalUser.FamilyId.Value)
                        : null;

                OnPropertyChanged(
                    nameof(SelectedFamily));

                ValidateAll();

                IsInitialized = true;
            },
            cancellationToken: cancellationToken);

        RaiseSaveCanExecuteChanged();
    }

    private bool CanExecuteSave()
    {
        if (IsBusy ||
            HasErrors ||
            _originalUser is null)
        {
            return false;
        }

        var normalizedUserName =
            UserName.Trim();

        var normalizedEmail =
            Email.Trim();

        return !string.Equals(
                   normalizedUserName,
                   _originalUser.UserName,
                   StringComparison.Ordinal) ||
               !string.Equals(
                   normalizedEmail,
                   _originalUser.Email,
                   StringComparison.OrdinalIgnoreCase) ||
               SelectedFamily?.Id !=
                   _originalUser.FamilyId;
    }

    private async Task ExecuteSaveAsync()
    {
        if (_originalUser is null)
        {
            return;
        }

        ValidateAll();

        if (HasErrors)
        {
            return;
        }

        await RunSafeAsync(
            async () =>
            {
                var normalizedUserName =
                    UserName.Trim();

                var normalizedEmail =
                    Email.Trim();

                var profileChanged =
                    !string.Equals(
                        normalizedUserName,
                        _originalUser.UserName,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        normalizedEmail,
                        _originalUser.Email,
                        StringComparison.OrdinalIgnoreCase);

                if (profileChanged)
                {
                    var updateResult =
                        await _updateUserHandler.HandleAsync(
                            new UpdateUserCommand(
                                _originalUser.Id,
                                normalizedUserName,
                                normalizedEmail));

                    if (updateResult.IsFailure)
                    {
                        await NotificationService.ShowErrorAsync(
                            updateResult.Error.Message,
                            "Editar utilizador");

                        return;
                    }
                }

                var familyChanged =
                    SelectedFamily?.Id !=
                    _originalUser.FamilyId;

                if (familyChanged)
                {
                    var familyResult =
                        await UpdateFamilyAsync(
                            _originalUser);

                    if (!familyResult)
                    {
                        return;
                    }
                }

                _originalUser =
                    _originalUser with
                    {
                        UserName = normalizedUserName,
                        Email = normalizedEmail,
                        FamilyId = SelectedFamily?.Id,
                        FamilyName = SelectedFamily?.Name
                    };

                _userName =
                    _originalUser.UserName;

                _email =
                    _originalUser.Email;

                OnPropertyChanged(
                    nameof(UserName),
                    nameof(Email));

                RaiseSaveCanExecuteChanged();

                await NotificationService.ShowInformationAsync(
                    "Utilizador atualizado com sucesso.",
                    "Sucesso");

                NavigationService.GoBack();
            });

        RaiseSaveCanExecuteChanged();
    }

    private async Task<bool> UpdateFamilyAsync(
        UserDTO originalUser)
    {
        var originalFamilyId =
            originalUser.FamilyId;

        var newFamilyId =
            SelectedFamily?.Id;

        // Não tinha família e continua sem família.
        if (!originalFamilyId.HasValue &&
            !newFamilyId.HasValue)
        {
            return true;
        }

        // Não tinha família e foi selecionada uma.
        if (!originalFamilyId.HasValue &&
            newFamilyId.HasValue)
        {
            var result =
                await _assignUserToFamilyHandler.HandleAsync(
                    new AssignUserToFamilyCommand(
                        originalUser.Id,
                        newFamilyId.Value));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Família");

                return false;
            }

            return true;
        }

        // Tinha família e agora ficará sem família.
        if (originalFamilyId.HasValue &&
            !newFamilyId.HasValue)
        {
            var result =
                await _removeUserFromFamilyHandler.HandleAsync(
                    new RemoveUserFromFamilyCommand(
                        originalUser.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Família");

                return false;
            }

            return true;
        }

        // Família original e nova são iguais.
        if (originalFamilyId == newFamilyId)
        {
            return true;
        }

        // Mudança entre duas famílias.
        var changeResult =
            await _changeUserFamilyHandler.HandleAsync(
                new ChangeUserFamilyCommand(
                    originalUser.Id,
                    newFamilyId!.Value));

        if (changeResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                changeResult.Error.Message,
                "Família");

            return false;
        }

        return true;
    }

    private void ExecuteCancel()
    {
        NavigationService.GoBack();
    }

    private void ValidateAll()
    {
        _validator.ValidateAll(
            UserName,
            Email);
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