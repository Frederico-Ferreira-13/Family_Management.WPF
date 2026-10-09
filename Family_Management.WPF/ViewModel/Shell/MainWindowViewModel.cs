using System.Diagnostics;
using System.Windows;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Users.Queries.GetUserById;

namespace Family_Management.WPF.ViewModel.Shell;

public sealed class MainWindowViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;
    private readonly GetUserByIdHandler _getUserByIdHandler;

    private bool _isLoggedIn;
    private string _currentUserName = "Visitante";
    private BaseViewModel? _currentChildViewModel;

    public MainWindowViewModel(
        CurrentUserService currentUserService,
        GetUserByIdHandler getUserByIdHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _getUserByIdHandler = getUserByIdHandler
            ?? throw new ArgumentNullException(
                nameof(getUserByIdHandler));

        Title = "Gestão Familiar";

        NavigateCommand =
            new AsyncRelayCommand<AppScreen>(
                NavigateAsync,
                CanNavigate);

        OpenAddMenuCommand =
            new AsyncRelayCommand(
                OpenAddMenuAsync,
                () => IsLoggedIn && !IsBusy);

        LogoutCommand =
            new AsyncRelayCommand(
                ExecuteLogoutAsync,
                () => IsLoggedIn && !IsBusy);

        ExitCommand =
            new RelayCommand(
                _ => ExecuteExit());

        NavigationService.CurrentChildViewModelChanged +=
            OnCurrentChildViewModelChanged;

        _currentUserService.SessionChanged +=
            OnSessionChanged;
    }

    public bool IsLoggedIn
    {
        get => _isLoggedIn;

        private set
        {
            if (!SetProperty(
                    ref _isLoggedIn,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public string CurrentUserName
    {
        get => _currentUserName;

        private set =>
            SetProperty(
                ref _currentUserName,
                value);
    }

    public BaseViewModel? CurrentChildViewModel
    {
        get => _currentChildViewModel;

        private set
        {
            if (!SetProperty(
                    ref _currentChildViewModel,
                    value))
            {
                return;
            }

            Debug.WriteLine(
                $"[DEBUG] CurrentChildViewModel alterado para: " +
                $"{value?.GetType().Name ?? "null"}");
        }
    }

    public AsyncRelayCommand<AppScreen> NavigateCommand { get; }

    public AsyncRelayCommand LogoutCommand { get; }

    public AsyncRelayCommand OpenAddMenuCommand { get; }

    public RelayCommand ExitCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        await RefreshSessionStateAsync(
            cancellationToken);

        if (NavigationService.CurrentViewModel is null)
        {
            await NavigationService
                .NavigateToLoginView();
        }
        else
        {
            CurrentChildViewModel =
                NavigationService.CurrentViewModel;
        }

        IsInitialized = true;
    }

    private bool CanNavigate(
        AppScreen screen)
    {
        return IsLoggedIn &&
               !IsBusy;
    }

    private async Task NavigateAsync(
        AppScreen screen)
    {
        if (!CanNavigate(screen))
            return;

        await RunSafeAsync(async () =>
        {
            await NavigationService
                .NavigateToScreen(screen);
        });

        RaiseCommandStates();
    }

    private async Task OpenAddMenuAsync()
    {
        if (!IsLoggedIn)
            return;

        await RunSafeAsync(async () =>
        {
            await NavigationService
                .ShowModal<AddNewSelectionViewModel>();
        });

        RaiseCommandStates();
    }

    private async Task ExecuteLogoutAsync()
    {
        if (!IsLoggedIn)
            return;

        var confirmed =
            await NotificationService
                .ShowConfirmationAsync(
                    "Deseja realmente terminar a sessão?",
                    "Terminar sessão");

        if (!confirmed)
            return;

        _currentUserService.Clear();

        ApplyLoggedOutState();

        await NavigationService
            .NavigateToLoginView();

        RaiseCommandStates();
    }

    private static void ExecuteExit()
    {
        Application.Current?.Shutdown();
    }

    private void OnCurrentChildViewModelChanged(
        BaseViewModel viewModel)
    {
        CurrentChildViewModel =
            viewModel;
    }

    private async void OnSessionChanged()
    {
        try
        {
            await RefreshSessionStateAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"[ERRO] Não foi possível atualizar " +
                $"o estado da sessão: {ex.Message}");
        }
    }

    private async Task RefreshSessionStateAsync(
        CancellationToken cancellationToken = default)
    {
        var userId =
            _currentUserService.UserId;

        if (!_currentUserService.IsAuthenticated ||
            !userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            ApplyLoggedOutState();
            return;
        }

        IsLoggedIn =
            true;

        var result =
            await _getUserByIdHandler.HandleAsync(
                new GetUserByIdQuery(
                    userId.Value),
                cancellationToken);

        if (result.IsFailure)
        {
            CurrentUserName =
                "Utilizador";

            return;
        }

        CurrentUserName =
            result.Value.UserName;
    }

    private void ApplyLoggedOutState()
    {
        IsLoggedIn =
            false;

        CurrentUserName =
            "Visitante";
    }

    private void RaiseCommandStates()
    {
        NavigateCommand
            .RaiseCanExecuteChanged();

        LogoutCommand
            .RaiseCanExecuteChanged();

        OpenAddMenuCommand
            .RaiseCanExecuteChanged();
    }
}