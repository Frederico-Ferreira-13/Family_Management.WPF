using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Export;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using Family_Management.WPF.ViewModel.Dashboard.Models;
using FamilyManagement.Application.UseCases.Currencies.Queries.GetDefaultCurrency;
using FamilyManagement.Application.UseCases.Dashboard;
using FamilyManagement.Application.UseCases.Setup.Interfaces;
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Users.Queries.GetUserById;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Dashboard;

public sealed partial class DashboardViewModel : BaseViewModel
{
    private readonly CurrentUserService _session;
    private readonly GetUserByIdHandler _users;
    private readonly GetDashboardHandler _dashboard;
    private readonly GetDefaultCurrencyHandler _currencies;
    private readonly ISetupService _setup;
    private readonly ITransactionExportService _export;

    private IReadOnlyCollection<TransactionDTO> _transactions =
        Array.Empty<TransactionDTO>();

    private DateTime _loadedMonth = DateTime.Today;

    public DashboardViewModel(
        CurrentUserService currentUserService,
        GetUserByIdHandler getUserByIdHandler,
        GetDashboardHandler dashboard,
        GetUpcomingRecurringPaymentsHandler upcomingPayments,
        GetDefaultCurrencyHandler getDefaultCurrencyHandler,
        ISetupService setupService,
        ITransactionExportService transactionExportService,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(navigationService, notificationService)
    {
        _session = currentUserService;
        _users = getUserByIdHandler;
        _dashboard = dashboard;
        _upcomingPayments = upcomingPayments;
        _currencies = getDefaultCurrencyHandler;
        _setup = setupService;
        _export = transactionExportService;

        Title = "Dashboard";

        RefreshCommand = new AsyncRelayCommand(
            () => RefreshAsync(),
            () => !IsBusy);

        ExportCommand = new AsyncRelayCommand(
            ExportAsync,
            () => !IsBusy && _transactions.Count > 0);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsBusy))
                RaiseCommands();
        };
    }

    public ICommand RefreshCommand { get; }

    public ICommand ExportCommand { get; }

    public bool IsSetupRequired { get; private set; }

    public string WelcomeMessage { get; private set; } = "";

    public string DefaultCurrencySymbol { get; private set; } = "€";

    public decimal WorkingCapitalBalance { get; private set; }

    public decimal WorkingCapitalReservedThisMonth { get; private set; }

    public decimal TotalBalance { get; private set; }

    public decimal TotalIncome { get; private set; }

    public decimal TotalExpenses { get; private set; }

    public decimal FixedExpenses { get; private set; }

    public decimal VariableExpenses { get; private set; }

    public decimal AvailableToSave =>
        TotalIncome
        - TotalExpenses
        - WorkingCapitalReservedThisMonth;

    public int TransactionsThisMonth => _transactions.Count;

    public int GoalsCount { get; private set; }

    public decimal GoalsSaved { get; private set; }

    public decimal GoalsTarget { get; private set; }

    public ObservableCollection<MonthlyBalanceData> MonthlyBalanceData
    {
        get;
    } = new();

    public ObservableCollection<ExpenseCategoryData> TopExpenses
    {
        get;
    } = new();

    public override Task InitializeAsync(
        CancellationToken cancellationToken = default) =>
        RefreshAsync(cancellationToken);

    private async Task RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        await RunSafeAsync(async () =>
        {
            Reset();

            try
            {
                if (!_session.UserId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Não existe um utilizador autenticado.");
                }

                var userResult = await _users.HandleAsync(
                    new GetUserByIdQuery(_session.UserId.Value),
                    cancellationToken);

                if (userResult.IsFailure)
                {
                    throw new InvalidOperationException(
                        userResult.Error.Message);
                }

                var user = userResult.Value;

                var currencyResult = await _currencies.HandleAsync(
                    new GetDefaultCurrencyQuery(),
                    cancellationToken);

                var currency = currencyResult.IsSuccess
                    ? currencyResult.Value.Code
                    : "EUR";

                DefaultCurrencySymbol = currencyResult.IsSuccess
                    ? currencyResult.Value.Symbol
                    : "€";

                var today = DateTime.Today;

                _loadedMonth = new DateTime(
                    today.Year,
                    today.Month,
                    1);

                var result = await _dashboard.HandleAsync(
                    user.Id,
                    user.FamilyId,
                    _loadedMonth,
                    cancellationToken);

                if (result.IsFailure)
                {
                    throw new InvalidOperationException(
                        result.Error.Message);
                }

                var data = result.Value;

                // Aviso de pagamentos trimestrais e semestrais
                // previstos para o próximo mês.
                await LoadUpcomingPaymentsAsync(
                    user.Id,
                    user.FamilyId,
                    _loadedMonth,
                    cancellationToken);

                _transactions = data.Transactions
                    .Where(t => t.Currency == currency)
                    .ToList();

                var summary = data.Currencies
                    .FirstOrDefault(c => c.Currency == currency);

                WorkingCapitalBalance = summary?.Metrics
                    .FirstOrDefault(metric =>
                        metric.Label == "Fundo de maneio: saldo")
                    ?.Value ?? 0m;

                WorkingCapitalReservedThisMonth = summary?.Metrics
                    .FirstOrDefault(metric =>
                        metric.Label == "Fundo de maneio: reservado no mês")
                    ?.Value ?? 0m;

                TotalBalance = summary?.Metrics
                    .FirstOrDefault(m =>
                        m.Label == "Saldo atual das contas")
                    ?.Value ?? 0m;

                // Movimentos registados no mês, incluindo os pendentes.
                var entries = _transactions
                    .Select(t => (
                        Type: t.Type,
                        Amount: t.Amount,
                        Category: t.CategoryName ?? "Diversos",
                        Recurring:
                            t.IsRecurringGenerated ||
                            t.RecurringTransactionId.HasValue))
                    .ToList();

                // A identificação da ocorrência usa a data prevista original.
                var generated = _transactions
                    .Where(t => t.RecurringTransactionId.HasValue)
                    .Select(t => (
                        t.RecurringTransactionId!.Value,
                        (t.ScheduledDate ?? t.Date).Date))
                    .ToHashSet();

                // Acrescenta apenas as ocorrências ainda por gerar.
                foreach (var recurrence in data.Recurrences
                    .Where(r => r.Currency == currency))
                {
                    foreach (var date in GetDashboardHandler.Occurrences(
                        recurrence,
                        _loadedMonth))
                    {
                        if (generated.Contains((recurrence.Id, date)))
                            continue;

                        entries.Add((
                            recurrence.Type,
                            recurrence.Amount,
                            recurrence.CategoryName ?? "Diversos",
                            true));
                    }
                }

                TotalIncome = entries
                    .Where(e => e.Type == TransactionType.Income)
                    .Sum(e => e.Amount);

                TotalExpenses = entries
                    .Where(e => e.Type == TransactionType.Expense)
                    .Sum(e => e.Amount);

                FixedExpenses = entries
                    .Where(e =>
                        e.Type == TransactionType.Expense &&
                        e.Recurring)
                    .Sum(e => e.Amount);

                VariableExpenses = TotalExpenses - FixedExpenses;

                var topExpenses = entries
                    .Where(e => e.Type == TransactionType.Expense)
                    .GroupBy(e => e.Category)
                    .Select(g => new ExpenseCategoryData(
                        g.Key,
                        g.Sum(e => e.Amount)))
                    .OrderByDescending(g => g.Amount)
                    .Take(5);

                foreach (var category in topExpenses)
                    TopExpenses.Add(category);

                // O histórico apresenta apenas movimentos confirmados.
                if (summary is not null)
                {
                    foreach (var item in summary.History)
                    {
                        MonthlyBalanceData.Add(
                            new MonthlyBalanceData(
                                item.Month,
                                item.Balance));
                    }
                }
                else
                {
                    for (var offset = -5; offset <= 0; offset++)
                    {
                        MonthlyBalanceData.Add(
                            new MonthlyBalanceData(
                                _loadedMonth.AddMonths(offset).ToString(
                                    "MMM yyyy",
                                    CultureInfo.GetCultureInfo("pt-PT")),
                                0m));
                    }
                }

                var goals = data.Goals
                    .Where(goal =>
                        goal.Currency == currency &&
                        !goal.IsWorkingCapitalFund)
                    .ToList();

                GoalsCount = goals.Count;
                GoalsSaved = goals.Sum(goal => goal.CurrentAmount);
                GoalsTarget = goals.Sum(goal => goal.TargetAmount);

                var setup = user.FamilyId.HasValue
                    ? await _setup.IsFamilySetupCompleteAsync(
                        user.FamilyId.Value,
                        user.Id)
                    : await _setup.IsUserPersonalSetupCompleteAsync(
                        user.Id);

                IsSetupRequired = setup.IsFailure || !setup.Value;

                WelcomeMessage =
                    $"Bem-vindo, {user.UserName}! " +
                    "Receitas e despesas incluem os valores previstos do mês.";

                if (data.Currencies.Any(c => c.Currency != currency))
                    WelcomeMessage += $" A apresentar apenas {currency}.";

                IsInitialized = true;
            }
            catch
            {
                Reset();

                WelcomeMessage =
                    "Não foi possível carregar os valores. " +
                    "Clique em Atualizar para tentar novamente.";

                throw;
            }
            finally
            {
                // Atualiza também os bindings definidos
                // em DashboardViewModel.UpcomingPayments.cs.
                OnPropertyChanged(string.Empty);
            }
        }, cancellationToken: cancellationToken);

        RaiseCommands();
    }

    private void Reset()
    {
        ResetUpcomingPayments();

        IsSetupRequired = false;

        TotalBalance = 0m;
        TotalIncome = 0m;
        TotalExpenses = 0m;
        FixedExpenses = 0m;
        VariableExpenses = 0m;

        WorkingCapitalBalance = 0m;
        WorkingCapitalReservedThisMonth = 0m;

        GoalsSaved = 0m;
        GoalsTarget = 0m;
        GoalsCount = 0;

        _transactions = Array.Empty<TransactionDTO>();

        MonthlyBalanceData.Clear();
        TopExpenses.Clear();

        WelcomeMessage = "A carregar os valores…";

        OnPropertyChanged(string.Empty);
    }

    private async Task ExportAsync()
    {
        await RunSafeAsync(async () =>
        {
            if (await _export.ExportToCsvAsync(
                    _transactions,
                    _loadedMonth))
            {
                await NotificationService.ShowInformationAsync(
                    "Movimentos exportados.",
                    "Dashboard");
            }
        });

        RaiseCommands();
    }

    private void RaiseCommands()
    {
        ((AsyncRelayCommand)RefreshCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)ExportCommand).RaiseCanExecuteChanged();
    }
}