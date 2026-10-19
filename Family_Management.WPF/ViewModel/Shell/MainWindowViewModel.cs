
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Accounts;
using Family_Management.WPF.ViewModel.Authentication;
using Family_Management.WPF.ViewModel.Budgets;
using Family_Management.WPF.ViewModel.Categories;
using Family_Management.WPF.ViewModel.Common;
using Family_Management.WPF.ViewModel.Dashboard;
using Family_Management.WPF.ViewModel.Families;
using Family_Management.WPF.ViewModel.Finance;
using Family_Management.WPF.ViewModel.Goals;
using Family_Management.WPF.ViewModel.Investments;
using Family_Management.WPF.ViewModel.RecurringTransactions;
using Family_Management.WPF.ViewModel.Transactions;
using Family_Management.WPF.ViewModel.Users;

using FamilyManagement.Application.UseCases.Users.Queries.GetUserById;

namespace Family_Management.WPF.ViewModel.Shell;

public sealed class MainWindowViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;
    private readonly GetUserByIdHandler _getUserByIdHandler;

    private bool _isLoggedIn;
    private bool _isSidebarExpanded = true;

    private string _currentUserName = "Visitante";
    private string _currentPageTitle = "Área de trabalho";

    private AppScreen? _selectedScreen;
    private BaseViewModel? _currentChildViewModel;

    public MainWindowViewModel(
        CurrentUserService currentUserService,
        GetUserByIdHandler getUserByIdHandler,
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

        Title = "Gestão Familiar";

        // Navegação principal.
        NavigateCommand = new AsyncRelayCommand<AppScreen>(
            NavigateAsync,
            CanNavigate);

        // Menu de adicionar novos registos.
        OpenAddMenuCommand = new AsyncRelayCommand(
            OpenAddMenuAsync,
            () => IsLoggedIn && !IsBusy);

        // NOVO: acesso à gestão de utilizadores
        // através do nome/ícone do perfil.
        OpenUserManagementCommand = new AsyncRelayCommand(
            OpenUserManagementAsync,
            () => IsLoggedIn && !IsBusy);

        // Terminar sessão.
        LogoutCommand = new AsyncRelayCommand(
            ExecuteLogoutAsync,
            () => IsLoggedIn && !IsBusy);

        // Encerrar aplicação.
        ExitCommand = new RelayCommand(
            _ => Application.Current?.Shutdown());

        // Expandir ou recolher menu lateral.
        ToggleSidebarCommand = new RelayCommand(
            _ => IsSidebarExpanded = !IsSidebarExpanded);

        NavigationService.CurrentChildViewModelChanged +=
            OnCurrentChildViewModelChanged;

        _currentUserService.SessionChanged +=
            OnSessionChanged;
    }

    // ==========================================
    // PROPRIEDADES
    // ==========================================

    public bool IsLoggedIn
    {
        get => _isLoggedIn;

        private set
        {
            if (SetProperty(ref _isLoggedIn, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool IsSidebarExpanded
    {
        get => _isSidebarExpanded;

        private set => SetProperty(
            ref _isSidebarExpanded,
            value);
    }

    public string CurrentUserName
    {
        get => _currentUserName;

        private set => SetProperty(
            ref _currentUserName,
            value);
    }

    public string CurrentPageTitle
    {
        get => _currentPageTitle;

        private set => SetProperty(
            ref _currentPageTitle,
            value);
    }

    public AppScreen? SelectedScreen
    {
        get => _selectedScreen;

        private set => SetProperty(
            ref _selectedScreen,
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

            SynchronizeNavigationState(value);
        }
    }

    // ==========================================
    // COMANDOS
    // ==========================================

    public AsyncRelayCommand<AppScreen> NavigateCommand { get; }

    public AsyncRelayCommand OpenAddMenuCommand { get; }

    // NOVO
    public AsyncRelayCommand OpenUserManagementCommand { get; }

    public AsyncRelayCommand LogoutCommand { get; }

    public RelayCommand ExitCommand { get; }

    public RelayCommand ToggleSidebarCommand { get; }

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

        await RefreshSessionStateAsync(cancellationToken);

        if (NavigationService.CurrentViewModel is null)
        {
            await NavigationService.NavigateToLoginView();
        }
        else
        {
            CurrentChildViewModel =
                NavigationService.CurrentViewModel;
        }

        IsInitialized = true;
    }

    // ==========================================
    // NAVEGAÇÃO PRINCIPAL
    // ==========================================

    private bool CanNavigate(AppScreen screen)
    {
        return IsLoggedIn && !IsBusy;
    }

    private async Task NavigateAsync(AppScreen screen)
    {
        if (!CanNavigate(screen))
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            await NavigationService.NavigateToScreen(screen);
        });

        RaiseCommandStates();
    }

    // ==========================================
    // ADICIONAR NOVO REGISTO
    // ==========================================

    private async Task OpenAddMenuAsync()
    {
        if (!IsLoggedIn || IsBusy)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            await NavigationService
                .ShowModal<AddNewSelectionViewModel>();
        });

        RaiseCommandStates();
    }

    // ==========================================
    // NOVO: GESTÃO DE UTILIZADORES
    // ==========================================

    private async Task OpenUserManagementAsync()
    {
        if (!IsLoggedIn || IsBusy)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            await NavigationService.NavigateToScreen(
                AppScreen.UserManagement);
        });

        RaiseCommandStates();
    }

    // ==========================================
    // TERMINAR SESSÃO
    // ==========================================

    private async Task ExecuteLogoutAsync()
    {
        if (!IsLoggedIn || IsBusy)
        {
            return;
        }

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                "Deseja realmente terminar a sessão?",
                "Terminar sessão");

        if (!confirmed)
        {
            return;
        }

        _currentUserService.Clear();

        ApplyLoggedOutState();

        await NavigationService.NavigateToLoginView();

        RaiseCommandStates();
    }

    // ==========================================
    // EVENTOS DE NAVEGAÇÃO
    // ==========================================

    private void OnCurrentChildViewModelChanged(
        BaseViewModel viewModel)
    {
        CurrentChildViewModel = viewModel;
    }

    // ==========================================
    // EVENTOS DE SESSÃO
    // ==========================================

    private async void OnSessionChanged()
    {
        try
        {
            await RefreshSessionStateAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                "[SHELL] Erro ao atualizar a sessão: " +
                ex.Message);
        }
    }

    // ==========================================
    // ATUALIZAR DADOS DO UTILIZADOR
    // ==========================================

    private async Task RefreshSessionStateAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (!_currentUserService.IsAuthenticated ||
            !userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            ApplyLoggedOutState();
            return;
        }

        IsLoggedIn = true;

        var result = await _getUserByIdHandler.HandleAsync(
            new GetUserByIdQuery(userId.Value),
            cancellationToken);

        CurrentUserName = result.IsFailure
            ? "Utilizador"
            : result.Value.UserName;
    }

    // ==========================================
    // ESTADO SEM SESSÃO
    // ==========================================

    private void ApplyLoggedOutState()
    {
        IsLoggedIn = false;

        CurrentUserName = "Visitante";

        CurrentPageTitle = "Área de trabalho";

        SelectedScreen = null;

        IsSidebarExpanded = true;
    }

    // ==========================================
    // SINCRONIZAR NAVEGAÇÃO
    // ==========================================

    private void SynchronizeNavigationState(
        BaseViewModel? viewModel)
    {
        switch (viewModel)
        {
            case DashboardViewModel:
                SetScreen(
                    AppScreen.Dashboard,
                    "Visão Geral");
                break;

            case UniversalFinanceViewModel:
                SetScreen(
                    AppScreen.UniversalFinance,
                    "Gestão Financeira");
                break;

            case AccountViewModel:
                SetScreen(
                    AppScreen.Accounts,
                    "Contas Bancárias");
                break;

            case TransactionViewModel:
                SetScreen(
                    AppScreen.Transactions,
                    "Transações");
                break;

            case RecurringTransactionViewModel:
                SetScreen(
                    AppScreen.RecurringTransaction,
                    "Receitas e Despesas Recorrentes");
                break;

            case CategoryViewModel:
                SetScreen(
                    AppScreen.Category,
                    "Categorias");
                break;

            case BudgetViewModel:
                SetScreen(
                    AppScreen.Budget,
                    "Orçamentos");
                break;

            case GoalViewModel:
                SetScreen(
                    AppScreen.Goal,
                    "Objetivos");
                break;

            case InvestmentViewModel:
                SetScreen(
                    AppScreen.Investment,
                    "Investimentos");
                break;

            case FamilyViewModel:
                SetScreen(
                    AppScreen.Family,
                    "Agregado Familiar");
                break;

            case FamilySetupViewModel:
                SetScreen(
                    AppScreen.FamilySetup,
                    "Configuração Familiar");
                break;

            case UserManagementViewModel:
                SetScreen(
                    AppScreen.UserManagement,
                    "Gestão de Utilizadores");
                break;

            case LoginViewModel:
                ClearScreen("Iniciar sessão");
                break;

            case RegisterViewModel:
                ClearScreen("Criar conta");
                break;

            default:
                ClearScreen("Área de trabalho");
                break;
        }
    }

    // ==========================================
    // DEFINIR ECRÃ ATUAL
    // ==========================================

    private void SetScreen(
        AppScreen screen,
        string title)
    {
        SelectedScreen = screen;
        CurrentPageTitle = title;
    }

    private void ClearScreen(string title)
    {
        SelectedScreen = null;
        CurrentPageTitle = title;
    }

    // ==========================================
    // ATUALIZAR ESTADO DOS COMANDOS
    // ==========================================

    private void RaiseCommandStates()
    {
        NavigateCommand.RaiseCanExecuteChanged();

        OpenAddMenuCommand.RaiseCanExecuteChanged();

        // NOVO
        OpenUserManagementCommand.RaiseCanExecuteChanged();

        LogoutCommand.RaiseCanExecuteChanged();
    }
}