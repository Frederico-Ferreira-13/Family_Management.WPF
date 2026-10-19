using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using Family_Management.WPF.ViewModel.Authentication;
using Family_Management.WPF.ViewModel.Families;

using FamilyManagement.Application.UseCases.Users.Commands.ActivateUser;
using FamilyManagement.Application.UseCases.Users.Commands.DeactivateUser;
using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Queries.GetAllUsers;

namespace Family_Management.WPF.ViewModel.Users;

public sealed class UserManagementViewModel : BaseViewModel
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
        : base(navigationService, notificationService)
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

        Title = "Gestão de utilizadores e família";

        // ==========================================
        // COMANDOS EXISTENTES
        // ==========================================

        LoadUsersCommand = new AsyncRelayCommand(
            ExecuteLoadUsersAsync,
            () => !IsBusy);

        EditUserCommand = new AsyncRelayCommand(
            ExecuteEditUserAsync,
            CanEditUser);

        DeactivateUserCommand = new AsyncRelayCommand(
            ExecuteDeactivateUserAsync,
            CanDeactivateUser);

        ActivateUserCommand = new AsyncRelayCommand(
            ExecuteActivateUserAsync,
            CanActivateUser);

        // ==========================================
        // NOVOS COMANDOS
        // ==========================================

        OpenMyProfileCommand = new AsyncRelayCommand(
            ExecuteOpenMyProfileAsync,
            () => !IsBusy &&
                  _currentUserService.IsAuthenticated);

        AddFamilyMemberCommand = new AsyncRelayCommand(
            ExecuteAddFamilyMemberAsync,
            () => !IsBusy &&
                  _currentUserService.IsAuthenticated);

        OpenFamilyManagementCommand = new AsyncRelayCommand(
            ExecuteOpenFamilyManagementAsync,
            () => !IsBusy &&
                  _currentUserService.IsAuthenticated);
    }

    // ==========================================
    // COLEÇÃO DE UTILIZADORES
    // ==========================================

    public ObservableCollection<UserDTO> Users { get; } = new();

    // ==========================================
    // UTILIZADOR SELECIONADO
    // ==========================================

    public UserDTO? SelectedUser
    {
        get => _selectedUser;

        set
        {
            if (!SetProperty(ref _selectedUser, value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    // ==========================================
    // COMANDOS EXISTENTES
    // ==========================================

    public AsyncRelayCommand LoadUsersCommand { get; }

    public AsyncRelayCommand EditUserCommand { get; }

    public AsyncRelayCommand DeactivateUserCommand { get; }

    public AsyncRelayCommand ActivateUserCommand { get; }

    // ==========================================
    // NOVOS COMANDOS
    // ==========================================

    public AsyncRelayCommand OpenMyProfileCommand { get; }

    public AsyncRelayCommand AddFamilyMemberCommand { get; }

    public AsyncRelayCommand OpenFamilyManagementCommand { get; }

    // ==========================================
    // INICIALIZAÇÃO
    // ==========================================

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await LoadUsersAsync(cancellationToken);

        IsInitialized = true;
    }

    // ==========================================
    // CARREGAR UTILIZADORES
    // ==========================================

    private Task ExecuteLoadUsersAsync()
    {
        return LoadUsersAsync();
    }

    private async Task LoadUsersAsync(
        CancellationToken cancellationToken = default)
    {
        await RunSafeAsync(
            () => ReloadUsersCoreAsync(cancellationToken),
            cancellationToken: cancellationToken);

        RaiseCommandStates();
    }

    private async Task ReloadUsersCoreAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _getAllUsersHandler.HandleAsync(
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

        // Mantemos o utilizador autenticado na lista.
        // Anteriormente era excluído, impedindo
        // a edição do próprio perfil.

        var users = result.Value
            .OrderBy(user => user.UserName)
            .ToList();

        Users.Clear();

        foreach (var user in users)
        {
            Users.Add(user);
        }

        SelectedUser = null;
    }

    // ==========================================
    // EDITAR UTILIZADOR
    // ==========================================

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

    // ==========================================
    // NOVO: ABRIR O MEU PERFIL
    // ==========================================

    private async Task ExecuteOpenMyProfileAsync()
    {
        if (IsBusy ||
            !_currentUserService.IsAuthenticated)
        {
            return;
        }

        var currentUserId = _currentUserService.UserId;

        if (!currentUserId.HasValue ||
            currentUserId.Value == Guid.Empty)
        {
            await NotificationService.ShowErrorAsync(
                "Não foi possível identificar o utilizador autenticado.",
                "O meu perfil");

            return;
        }

        // Procurar o utilizador autenticado na coleção.
        var currentUser = Users.FirstOrDefault(
            user => user.Id == currentUserId.Value);

        // Se não estiver carregado, atualizar a lista.
        if (currentUser is null)
        {
            await LoadUsersAsync();

            currentUser = Users.FirstOrDefault(
                user => user.Id == currentUserId.Value);
        }

        if (currentUser is null)
        {
            await NotificationService.ShowErrorAsync(
                "Não foi possível encontrar o seu perfil.",
                "O meu perfil");

            return;
        }

        await NavigationService.NavigateTo<
            UserEditViewModel,
            UserDTO>(currentUser);
    }

    // ==========================================
    // NOVO: ADICIONAR MEMBRO À FAMÍLIA
    // ==========================================

    private async Task ExecuteAddFamilyMemberAsync()
    {
        if (IsBusy ||
            !_currentUserService.IsAuthenticated)
        {
            return;
        }

        await NavigationService.NavigateTo<
            FamilyMemberCreateViewModel>();
    }

    // ==========================================
    // NOVO: ABRIR GESTÃO DA FAMÍLIA
    // ==========================================

    private async Task ExecuteOpenFamilyManagementAsync()
    {
        if (IsBusy ||
            !_currentUserService.IsAuthenticated)
        {
            return;
        }

        await NavigationService.NavigateToScreen(
            AppScreen.Family);
    }

    // ==========================================
    // ATIVAR UTILIZADOR
    // ==========================================

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

        if (user is null || user.IsActive)
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

        await RunSafeAsync(async () =>
        {
            var result = await _activateUserHandler.HandleAsync(
                new ActivateUserCommand(user.Id));

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

    // ==========================================
    // DESATIVAR UTILIZADOR
    // ==========================================

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

        if (user is null || !user.IsActive)
        {
            return;
        }

        // Não permitir desativar a própria conta
        // a partir desta página.
        var currentUserId = _currentUserService.UserId;

        if (currentUserId.HasValue &&
            user.Id == currentUserId.Value)
        {
            await NotificationService.ShowErrorAsync(
                "Não pode desativar a sua própria conta nesta página.",
                "Desativar utilizador");

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

        await RunSafeAsync(async () =>
        {
            var result = await _deactivateUserHandler.HandleAsync(
                new DeactivateUserCommand(user.Id));

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

    // ==========================================
    // ATUALIZAR ESTADO DOS COMANDOS
    // ==========================================

    private void RaiseCommandStates()
    {
        LoadUsersCommand.RaiseCanExecuteChanged();

        EditUserCommand.RaiseCanExecuteChanged();

        ActivateUserCommand.RaiseCanExecuteChanged();

        DeactivateUserCommand.RaiseCanExecuteChanged();

        OpenMyProfileCommand.RaiseCanExecuteChanged();

        AddFamilyMemberCommand.RaiseCanExecuteChanged();

        OpenFamilyManagementCommand.RaiseCanExecuteChanged();
    }
}