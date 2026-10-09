using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Export;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using Family_Management.WPF.ViewModel.Dashboard.Models;
using FamilyManagement.Application.UseCases.Setup.Interfaces;
using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByFamily;
using FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;
using FamilyManagement.Application.UseCases.Currencies.Queries.GetDefaultCurrency;
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Queries.GetTransactionsByFamily;
using FamilyManagement.Application.UseCases.Transactions.Queries.GetTransactionsByUser;
using FamilyManagement.Application.UseCases.Users.Queries.GetUserById;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Common;

namespace Family_Management.WPF.ViewModel.Dashboard;

public sealed class DashboardViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;
    private readonly ISetupService _setupService;

    private readonly GetUserByIdHandler _getUserByIdHandler;
    private readonly GetAccountsByUserHandler _getAccountsByUserHandler;
    private readonly GetAccountsByFamilyHandler _getAccountsByFamilyHandler;
    private readonly GetTransactionsByUserHandler _getTransactionsByUserHandler;
    private readonly GetTransactionsByFamilyHandler _getTransactionsByFamilyHandler;
    private readonly GetDefaultCurrencyHandler _getDefaultCurrencyHandler;

    private readonly ITransactionExportService
        _transactionExportService;

    private bool _isSetupRequired;
    private string? _welcomeMessage;

    private decimal _totalBalance;
    private decimal _totalIncome;
    private decimal _totalExpenses;
    private decimal _fixedExpenses;
    private decimal _variableExpenses;
    private decimal _availableToSave;

    private int _transactionsThisMonth;

    private string _defaultCurrencySymbol = "€";

    private Guid? _currentFamilyId;

    private IReadOnlyCollection<TransactionDTO>
        _currentMonthTransactions =
            Array.Empty<TransactionDTO>();

    public DashboardViewModel(
        CurrentUserService currentUserService,
        ISetupService setupService,
        GetUserByIdHandler getUserByIdHandler,
        GetAccountsByUserHandler getAccountsByUserHandler,
        GetAccountsByFamilyHandler getAccountsByFamilyHandler,
        GetTransactionsByUserHandler getTransactionsByUserHandler,
        GetTransactionsByFamilyHandler getTransactionsByFamilyHandler,
        GetDefaultCurrencyHandler getDefaultCurrencyHandler,
        ITransactionExportService transactionExportService,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _setupService = setupService
            ?? throw new ArgumentNullException(
                nameof(setupService));

        _getUserByIdHandler = getUserByIdHandler
            ?? throw new ArgumentNullException(
                nameof(getUserByIdHandler));

        _getAccountsByUserHandler =
            getAccountsByUserHandler
            ?? throw new ArgumentNullException(
                nameof(getAccountsByUserHandler));

        _getAccountsByFamilyHandler =
            getAccountsByFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(getAccountsByFamilyHandler));

        _getTransactionsByUserHandler =
            getTransactionsByUserHandler
            ?? throw new ArgumentNullException(
                nameof(getTransactionsByUserHandler));

        _getTransactionsByFamilyHandler =
            getTransactionsByFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(getTransactionsByFamilyHandler));

        _getDefaultCurrencyHandler =
            getDefaultCurrencyHandler
            ?? throw new ArgumentNullException(
                nameof(getDefaultCurrencyHandler));

        _transactionExportService =
            transactionExportService
            ?? throw new ArgumentNullException(
                nameof(transactionExportService));

        Title = "Dashboard";

        RefreshCommand =
            new AsyncRelayCommand(
                RefreshDataAsync,
                () => !IsBusy);

        ExportCommand =
            new AsyncRelayCommand(
                ExportCurrentMonthAsync,
                CanExport);
    }

    public bool IsSetupRequired
    {
        get => _isSetupRequired;

        private set =>
            SetProperty(
                ref _isSetupRequired,
                value);
    }

    public string? WelcomeMessage
    {
        get => _welcomeMessage;

        private set =>
            SetProperty(
                ref _welcomeMessage,
                value);
    }

    public decimal TotalBalance
    {
        get => _totalBalance;

        private set =>
            SetProperty(
                ref _totalBalance,
                value);
    }

    public decimal TotalIncome
    {
        get => _totalIncome;

        private set =>
            SetProperty(
                ref _totalIncome,
                value);
    }

    public decimal TotalExpenses
    {
        get => _totalExpenses;

        private set =>
            SetProperty(
                ref _totalExpenses,
                value);
    }

    public decimal FixedExpenses
    {
        get => _fixedExpenses;

        private set =>
            SetProperty(
                ref _fixedExpenses,
                value);
    }

    public decimal VariableExpenses
    {
        get => _variableExpenses;

        private set =>
            SetProperty(
                ref _variableExpenses,
                value);
    }

    public decimal AvailableToSave
    {
        get => _availableToSave;

        private set =>
            SetProperty(
                ref _availableToSave,
                value);
    }

    public int TransactionsThisMonth
    {
        get => _transactionsThisMonth;

        private set =>
            SetProperty(
                ref _transactionsThisMonth,
                value);
    }

    public string DefaultCurrencySymbol
    {
        get => _defaultCurrencySymbol;

        private set =>
            SetProperty(
                ref _defaultCurrencySymbol,
                value);
    }

    public ObservableCollection<MonthlyBalanceData>
        MonthlyBalanceData { get; } = new();

    public ObservableCollection<ExpenseCategoryData>
        TopExpenses { get; } = new();

    public ICommand RefreshCommand { get; }

    public ICommand ExportCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        await LoadDashboardDataAsync(
            cancellationToken);

        IsInitialized = true;
    }

    private async Task RefreshDataAsync()
    {
        await RunSafeAsync(async () =>
        {
            await LoadDashboardDataAsync(
                CancellationToken.None);
        });

        RaiseCommandStates();
    }

    private async Task LoadDashboardDataAsync(
        CancellationToken cancellationToken)
    {
        ResetDashboard();

        var userId =
            _currentUserService.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            await NotificationService.ShowWarningAsync(
                "Não existe um utilizador autenticado.",
                "Dashboard");

            return;
        }

        var userResult =
            await _getUserByIdHandler.HandleAsync(
                new GetUserByIdQuery(
                    userId.Value));

        if (userResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                userResult.Error.Message,
                "Dashboard");

            return;
        }

        var user =
            userResult.Value;

        _currentFamilyId =
            user.FamilyId;

        WelcomeMessage =
            $"Bem-vindo, {user.UserName}!";

        await LoadDefaultCurrencyAsync(
            cancellationToken);

        await LoadSetupStatusAsync(
            userId.Value);

        await LoadAccountsAsync(
            userId.Value);

        await LoadTransactionsAsync(
            userId.Value);

        RaiseCommandStates();
    }

    private async Task LoadDefaultCurrencyAsync(
        CancellationToken cancellationToken)
    {
        var result =
            await _getDefaultCurrencyHandler.HandleAsync(
                new GetDefaultCurrencyQuery(),
                cancellationToken);

        DefaultCurrencySymbol =
            result.IsSuccess
                ? result.Value.Symbol
                : "€";
    }

    private async Task LoadSetupStatusAsync(
        Guid userId)
    {
        if (_currentFamilyId.HasValue)
        {
            var result =
                await _setupService
                    .IsFamilySetupCompleteAsync(
                        _currentFamilyId.Value,
                        userId);

            IsSetupRequired =
                result.IsFailure ||
                !result.Value;

            return;
        }

        var personalResult =
            await _setupService
                .IsUserPersonalSetupCompleteAsync(
                    userId);

        IsSetupRequired =
            personalResult.IsFailure ||
            !personalResult.Value;
    }

    private async Task LoadAccountsAsync(Guid userId)
    {
        Result<IEnumerable<AccountDTO>> accountsResult;

        if (_currentFamilyId.HasValue)
        {
            accountsResult =
                await _getAccountsByFamilyHandler.HandleAsync(
                    new GetAccountsByFamilyQuery(
                        _currentFamilyId.Value));
        }
        else
        {
            accountsResult =
                await _getAccountsByUserHandler.HandleAsync(
                    new GetAccountsByUserQuery(
                        userId));
        }

        if (accountsResult.IsFailure)
        {
            TotalBalance = 0m;
            return;
        }

        TotalBalance =
            accountsResult.Value
                .Where(account => account.IsActive)
                .Sum(account => account.Balance);
    }

    private async Task LoadTransactionsAsync(
        Guid userId)
    {
        var today =
            DateTime.Today;

        var currentMonthStart =
            new DateTime(
                today.Year,
                today.Month,
                1);

        var currentMonthEnd =
            currentMonthStart
                .AddMonths(1)
                .AddTicks(-1);

        var historyStart =
            currentMonthStart
                .AddMonths(-5);

        var transactionsResult =
            await GetTransactionsAsync(
                userId,
                historyStart,
                currentMonthEnd);

        if (transactionsResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                transactionsResult.Error.Message,
                "Dashboard");

            return;
        }

        var transactions =
            transactionsResult.Value
                .Where(transaction =>
                    transaction.IsActive)
                .ToList();

        _currentMonthTransactions =
            transactions
                .Where(transaction =>
                    transaction.Date >= currentMonthStart &&
                    transaction.Date <= currentMonthEnd)
                .ToList();

        CalculateCurrentMonthMetrics(
            _currentMonthTransactions);

        BuildTopExpenses(
            _currentMonthTransactions);

        BuildMonthlyHistory(
            transactions,
            currentMonthStart);
    }

    private async Task<
        Result<IEnumerable<TransactionDTO>>>
        GetTransactionsAsync(
            Guid userId,
            DateTime startDate,
            DateTime endDate)
    {
        if (_currentFamilyId.HasValue)
        {
            return await
                _getTransactionsByFamilyHandler
                    .HandleAsync(
                        new GetTransactionsByFamilyQuery(
                            _currentFamilyId.Value,
                            startDate,
                            endDate));
        }

        var filter =
            new TransactionFilter
            {
                StartDate = startDate,
                EndDate = endDate,
                IncludeDetails = true
            };

        return await
            _getTransactionsByUserHandler
                .HandleAsync(
                    new GetTransactionsByUserQuery(
                        userId,
                        filter));
    }

    private void CalculateCurrentMonthMetrics(
        IEnumerable<TransactionDTO> transactions)
    {
        var allTransactions =
            transactions.ToList();

        var confirmed =
            allTransactions
                .Where(transaction =>
                    transaction.IsConfirmed)
                .ToList();

        TotalIncome =
            confirmed
                .Where(transaction =>
                    transaction.Type ==
                    TransactionType.Income)
                .Sum(transaction =>
                    transaction.Amount);

        TotalExpenses =
            confirmed
                .Where(transaction =>
                    transaction.Type ==
                    TransactionType.Expense)
                .Sum(transaction =>
                    transaction.Amount);

        FixedExpenses =
            confirmed
                .Where(transaction =>
                    transaction.Type ==
                        TransactionType.Expense &&
                    transaction.IsRecurringGenerated)
                .Sum(transaction =>
                    transaction.Amount);

        VariableExpenses =
            TotalExpenses -
            FixedExpenses;

        AvailableToSave =
            TotalIncome -
            TotalExpenses;

        TransactionsThisMonth =
            allTransactions.Count;
    }

    private void BuildTopExpenses(
        IEnumerable<TransactionDTO> transactions)
    {
        TopExpenses.Clear();

        var topExpenses =
            transactions
                .Where(transaction =>
                    transaction.IsConfirmed &&
                    transaction.Type ==
                    TransactionType.Expense)
                .GroupBy(transaction =>
                    string.IsNullOrWhiteSpace(
                        transaction.CategoryName)
                        ? "Diversos"
                        : transaction.CategoryName!)
                .Select(group =>
                    new ExpenseCategoryData(
                        CategoryName:
                            group.Key,
                        Amount:
                            group.Sum(transaction =>
                                transaction.Amount)))
                .OrderByDescending(item =>
                    item.Amount)
                .Take(5);

        foreach (var expense in topExpenses)
        {
            TopExpenses.Add(expense);
        }
    }

    private void BuildMonthlyHistory(
        IEnumerable<TransactionDTO> transactions,
        DateTime currentMonthStart)
    {
        MonthlyBalanceData.Clear();

        var culture =
            CultureInfo.GetCultureInfo(
                "pt-PT");

        var confirmed =
            transactions
                .Where(transaction =>
                    transaction.IsConfirmed)
                .ToList();

        for (var offset = 5;
             offset >= 0;
             offset--)
        {
            var month =
                currentMonthStart
                    .AddMonths(-offset);

            var nextMonth =
                month.AddMonths(1);

            var monthlyTransactions =
                confirmed.Where(transaction =>
                    transaction.Date >= month &&
                    transaction.Date < nextMonth);

            var income =
                monthlyTransactions
                    .Where(transaction =>
                        transaction.Type ==
                        TransactionType.Income)
                    .Sum(transaction =>
                        transaction.Amount);

            var expenses =
                monthlyTransactions
                    .Where(transaction =>
                        transaction.Type ==
                        TransactionType.Expense)
                    .Sum(transaction =>
                        transaction.Amount);

            MonthlyBalanceData.Add(
                new MonthlyBalanceData(
                    Month:
                        month.ToString(
                                "MMM",
                                culture)
                            .ToUpper(culture),
                    Balance:
                        income - expenses));
        }
    }

    private bool CanExport()
    {
        return !IsBusy &&
               _currentUserService.IsAuthenticated &&
               _currentMonthTransactions.Count > 0;
    }

    private async Task ExportCurrentMonthAsync()
    {
        if (!CanExport())
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var exported =
                await _transactionExportService
                    .ExportToCsvAsync(
                        _currentMonthTransactions,
                        DateTime.Today);

            if (!exported)
            {
                return;
            }

            await NotificationService
                .ShowInformationAsync(
                    "Movimentos mensais exportados para um ficheiro compatível com Excel.",
                    "Exportação concluída");
        });

        RaiseCommandStates();
    }

    private void ResetDashboard()
    {
        IsSetupRequired = false;

        WelcomeMessage = null;

        TotalBalance = 0m;
        TotalIncome = 0m;
        TotalExpenses = 0m;
        FixedExpenses = 0m;
        VariableExpenses = 0m;
        AvailableToSave = 0m;

        TransactionsThisMonth = 0;

        DefaultCurrencySymbol = "€";

        _currentFamilyId = null;

        _currentMonthTransactions =
            Array.Empty<TransactionDTO>();

        MonthlyBalanceData.Clear();
        TopExpenses.Clear();
    }

    private void RaiseCommandStates()
    {
        if (RefreshCommand
            is AsyncRelayCommand refresh)
        {
            refresh.RaiseCanExecuteChanged();
        }

        if (ExportCommand
            is AsyncRelayCommand export)
        {
            export.RaiseCanExecuteChanged();
        }
    }
}