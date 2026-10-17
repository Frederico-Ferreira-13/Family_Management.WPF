
using System.Windows;
using System.Windows.Media;

using Family_Management.WPF.Services.Threading;

using Family_Management.WPF.ViewModel;
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
using Family_Management.WPF.ViewModel.Shell;
using Family_Management.WPF.ViewModel.Transactions;
using Family_Management.WPF.ViewModel.Users;

using Microsoft.Extensions.DependencyInjection;

namespace Family_Management.WPF.Services.Navigation;

public sealed class NavigationService :
    INavigationService,
    IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDispatcherService _dispatcherService;

    private readonly Stack<BaseViewModel> _navigationHistory = new();

    private BaseViewModel? _currentViewModel;

    private bool _isDisposed;

    public NavigationService(
        IServiceProvider serviceProvider,
        IDispatcherService dispatcherService)
    {
        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(
                nameof(serviceProvider));

        _dispatcherService = dispatcherService
            ?? throw new ArgumentNullException(
                nameof(dispatcherService));
    }

    public event Action<BaseViewModel>? CurrentChildViewModelChanged;

    public BaseViewModel? CurrentViewModel => _currentViewModel;

    public bool CanGoBack => _navigationHistory.Count > 0;

    public async Task NavigateTo<TViewModel>()
        where TViewModel : BaseViewModel
    {
        ThrowIfDisposed();

        var viewModel = _serviceProvider
            .GetRequiredService<TViewModel>();

        await InternalNavigateAsync(viewModel);
    }

    public async Task NavigateTo<TViewModel, TParameter>(
        TParameter parameter)
        where TViewModel : BaseViewModel
    {
        ThrowIfDisposed();

        var viewModel = _serviceProvider
            .GetRequiredService<TViewModel>();

        ApplyParameter(viewModel, parameter);

        await InternalNavigateAsync(viewModel);
    }

    public async Task NavigateToScreen(AppScreen screen)
    {
        ThrowIfDisposed();

        if (!_screenToViewModelMap.TryGetValue(
                screen,
                out var viewModelType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(screen),
                screen,
                "O ecrã solicitado não possui um ViewModel associado.");
        }

        var viewModel = _serviceProvider
            .GetRequiredService(viewModelType)
            as BaseViewModel;

        if (viewModel is null)
        {
            throw new InvalidOperationException(
                $"O tipo '{viewModelType.Name}' não é um BaseViewModel válido.");
        }

        await InternalNavigateAsync(viewModel);
    }

    public Task NavigateToLoginView()
    {
        return NavigateTo<LoginViewModel>();
    }

    public Task NavigateToRegisterView()
    {
        return NavigateTo<RegisterViewModel>();
    }

    public void GoBack()
    {
        ThrowIfDisposed();

        if (_navigationHistory.Count == 0)
        {
            return;
        }

        var previousViewModel = _navigationHistory.Pop();

        _currentViewModel = previousViewModel;

        CurrentChildViewModelChanged?.Invoke(previousViewModel);
    }

    public Task ShowModal<TViewModel>()
        where TViewModel : BaseViewModel
    {
        return ShowModal<TViewModel, object?>(null);
    }

    public Task ShowModal<TViewModel, TParameter>(
        TParameter parameter)
        where TViewModel : BaseViewModel
    {
        ThrowIfDisposed();

        return _dispatcherService.InvokeAsync(() =>
        {
            var viewModel = _serviceProvider
                .GetRequiredService<TViewModel>();

            ApplyParameter(viewModel, parameter);

            var window = new Window
            {
                Content = viewModel,

                SizeToContent = SizeToContent.WidthAndHeight,

                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner,

                Owner = Application.Current?.MainWindow,

                WindowStyle = WindowStyle.None,

                AllowsTransparency = true,

                Background = Brushes.Transparent
            };

            window.ShowDialog();
        });
    }

    public void CloseModal()
    {
        ThrowIfDisposed();

        _dispatcherService.Invoke(() =>
        {
            var application = Application.Current;

            if (application is null)
            {
                return;
            }

            var activeModal = application.Windows
                .OfType<Window>()
                .FirstOrDefault(window =>
                    window != application.MainWindow &&
                    window.IsActive);

            activeModal?.Close();
        });
    }

    private async Task InternalNavigateAsync(
        BaseViewModel viewModel)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(viewModel);

        if (ReferenceEquals(_currentViewModel, viewModel))
        {
            await InitializeViewModelAsync(viewModel);
            return;
        }

        // Inicializar antes de alterar o histórico.
        // Se falhar, o ecrã atual mantém-se.

        await InitializeViewModelAsync(viewModel);

        if (_currentViewModel is not null)
        {
            _navigationHistory.Push(_currentViewModel);
        }

        _currentViewModel = viewModel;

        CurrentChildViewModelChanged?.Invoke(viewModel);
    }

    private static async Task InitializeViewModelAsync(
        BaseViewModel viewModel)
    {
        if (viewModel is not IAsyncLoadable asyncLoadable)
        {
            return;
        }

        await asyncLoadable.InitializeAsync();
    }

    private static void ApplyParameter<TParameter>(
        BaseViewModel viewModel,
        TParameter parameter)
    {
        if (viewModel is IParameterReceiver<TParameter> receiver)
        {
            receiver.ReceiveParameter(parameter);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            this);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // Não libertar individualmente os ViewModels
        // resolvidos pelo DI: o âmbito da aplicação
        // gere a libertação dos serviços IDisposable.

        _navigationHistory.Clear();

        _currentViewModel = null;

        CurrentChildViewModelChanged = null;
    }

    private readonly Dictionary<AppScreen, Type>
        _screenToViewModelMap = new()
        {
            [AppScreen.UniversalFinance] =
                typeof(UniversalFinanceViewModel),

            [AppScreen.Dashboard] =
                typeof(DashboardViewModel),

            [AppScreen.Accounts] =
                typeof(AccountViewModel),

            [AppScreen.Transactions] =
                typeof(TransactionViewModel),

            [AppScreen.Category] =
                typeof(CategoryViewModel),

            [AppScreen.Family] =
                typeof(FamilyViewModel),

            [AppScreen.Budget] =
                typeof(BudgetViewModel),

            [AppScreen.Goal] =
                typeof(GoalViewModel),

            [AppScreen.Investment] =
                typeof(InvestmentViewModel),

            [AppScreen.RecurringTransaction] =
                typeof(RecurringTransactionViewModel),

            [AppScreen.UserManagement] =
                typeof(UserManagementViewModel),

            [AppScreen.FamilySetup] =
                typeof(FamilySetupViewModel),

            [AppScreen.AddNewSelection] =
                typeof(AddNewSelectionViewModel)
        };
}