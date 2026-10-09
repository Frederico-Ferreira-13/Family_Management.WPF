using System.Collections.ObjectModel;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Users.Commands.ActivateUser;
using FamilyManagement.Application.UseCases.Users.Commands.DeactivateUser;
using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Queries.GetAllUsers;

namespace Family_Management.WPF.ViewModel.Users;

public sealed class UserManagementViewModel
    : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;
    private readonly GetAllUsersHandler _getAllUsersHandler;
    private readonly ActivateUserHandler _activateUserHandler;
    private readonly DeactivateUserHandler _deactivateUserHandler;

    private UserDTO? _selectedUser;

    public UserManagementViewModel(
        CurrentUserService currentUserService,
        GetAllUsersHandler getAllUsersHandler,
        ActivateUserHandler activateUserHandler,
        DeactivateUserHandler deactivateUserHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _getAllUsersHandler = getAllUsersHandler
            ?? throw new ArgumentNullException(
                nameof(getAllUsersHandler));

        _activateUserHandler = activateUserHandler
            ?? throw new ArgumentNullException(
                nameof(activateUserHandler));

        _deactivateUserHandler = deactivateUserHandler
            ?? throw new ArgumentNullException(
                nameof(deactivateUserHandler));

        Title = "Gestão de utilizadores";

        LoadUsersCommand =
            new AsyncRelayCommand(
                ExecuteLoadUsersAsync,
                () => !IsBusy);

        EditUserCommand =
            new AsyncRelayCommand(
                ExecuteEditUserAsync,
                CanEditUser);

        DeactivateUserCommand =
            new AsyncRelayCommand(
                ExecuteDeactivateUserAsync,
                CanDeactivateUser);

        ActivateUserCommand =
            new AsyncRelayCommand(
                ExecuteActivateUserAsync,
                CanActivateUser);
    }

    public ObservableCollection<UserDTO> Users { get; }
        = new();

    public UserDTO? SelectedUser
    {
        get => _selectedUser;

        set
        {
            if (!SetProperty(
                    ref _selectedUser,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public AsyncRelayCommand LoadUsersCommand { get; }

    public AsyncRelayCommand EditUserCommand { get; }

    public AsyncRelayCommand DeactivateUserCommand { get; }

    public AsyncRelayCommand ActivateUserCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await LoadUsersAsync(
            cancellationToken);

        IsInitialized = true;
    }

    private Task ExecuteLoadUsersAsync()
    {
        return LoadUsersAsync();
    }

    private async Task LoadUsersAsync(
        CancellationToken cancellationToken = default)
    {
        await RunSafeAsync(
            () => ReloadUsersCoreAsync(
                cancellationToken),
            cancellationToken: cancellationToken);

        RaiseCommandStates();
    }

    private async Task ReloadUsersCoreAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result =
            await _getAllUsersHandler.HandleAsync(
                new GetAllUsersQuery(),
                cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Utilizadores");

            return;
        }

        var currentUserId =
            _currentUserService.UserId;

        var users = result.Value
            .Where(user =>
                !currentUserId.HasValue ||
                currentUserId.Value == Guid.Empty ||
                user.Id != currentUserId.Value)
            .OrderBy(user => user.UserName)
            .ToList();

        Users.Clear();

        foreach (var user in users)
        {
            Users.Add(user);
        }

        SelectedUser = null;
    }

    private bool CanEditUser()
    {
        return !IsBusy &&
               SelectedUser is not null;
    }

    private async Task ExecuteEditUserAsync()
    {
        var user = SelectedUser;

        if (user is null)
        {
            return;
        }

        await NavigationService.NavigateTo<
            UserEditViewModel,
            UserDTO>(user);
    }

    private bool CanActivateUser()
    {
        return !IsBusy &&
               SelectedUser is
               {
                   IsActive: false
               };
    }

    private async Task ExecuteActivateUserAsync()
    {
        var user = SelectedUser;

        if (user is null ||
            user.IsActive)
        {
            return;
        }

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Deseja ativar o utilizador '{user.UserName}'?",
                "Ativar utilizador");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(
            async () =>
            {
                var result =
                    await _activateUserHandler.HandleAsync(
                        new ActivateUserCommand(
                            user.Id));

                if (result.IsFailure)
                {
                    await NotificationService.ShowErrorAsync(
                        result.Error.Message,
                        "Ativar utilizador");

                    return;
                }

                await ReloadUsersCoreAsync();

                await NotificationService.ShowInformationAsync(
                    "Utilizador ativado com sucesso.",
                    "Sucesso");
            });

        RaiseCommandStates();
    }

    private bool CanDeactivateUser()
    {
        return !IsBusy &&
               SelectedUser is
               {
                   IsActive: true
               };
    }

    private async Task ExecuteDeactivateUserAsync()
    {
        var user = SelectedUser;

        if (user is null ||
            !user.IsActive)
        {
            return;
        }

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Deseja desativar o utilizador '{user.UserName}'?",
                "Desativar utilizador");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(
            async () =>
            {
                var result =
                    await _deactivateUserHandler.HandleAsync(
                        new DeactivateUserCommand(
                            user.Id));

                if (result.IsFailure)
                {
                    await NotificationService.ShowErrorAsync(
                        result.Error.Message,
                        "Desativar utilizador");

                    return;
                }

                await ReloadUsersCoreAsync();

                await NotificationService.ShowInformationAsync(
                    "Utilizador desativado com sucesso.",
                    "Sucesso");
            });

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        LoadUsersCommand.RaiseCanExecuteChanged();
        EditUserCommand.RaiseCanExecuteChanged();
        ActivateUserCommand.RaiseCanExecuteChanged();
        DeactivateUserCommand.RaiseCanExecuteChanged();
    }
}