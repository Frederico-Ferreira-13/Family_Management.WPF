using System.Collections.ObjectModel;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using Family_Management.WPF.ViewModel.Models;
using FamilyManagement.Application.UseCases.Budgets.Commands.DeactivateBudget;
using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetsByUserAndPeriod;
using FamilyManagement.Application.UseCases.Budgets.Queries.GetMonthlyBudgetSummary;

namespace Family_Management.WPF.ViewModel.Budgets;

public sealed class BudgetViewModel : BaseViewModel
{
    private readonly GetBudgetsByUserAndPeriodHandler
        _getBudgetsHandler;

    private readonly GetMonthlyBudgetSummaryHandler
        _getSummaryHandler;

    private readonly DeactivateBudgetHandler
        _deactivateBudgetHandler;

    private readonly CurrentUserService
        _currentUserService;

    private BudgetDTO? _selectedBudget;
    private MonthlyBudgetSummaryDTO? _summary;
    private int _selectedMonth;
    private int _selectedYear;

    public BudgetViewModel(
        BudgetFormViewModel budgetForm,
        GetBudgetsByUserAndPeriodHandler getBudgetsHandler,
        GetMonthlyBudgetSummaryHandler getSummaryHandler,
        DeactivateBudgetHandler deactivateBudgetHandler,
        CurrentUserService currentUserService,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        BudgetForm = budgetForm
            ?? throw new ArgumentNullException(nameof(budgetForm));

        _getBudgetsHandler = getBudgetsHandler
            ?? throw new ArgumentNullException(nameof(getBudgetsHandler));

        _getSummaryHandler = getSummaryHandler
            ?? throw new ArgumentNullException(nameof(getSummaryHandler));

        _deactivateBudgetHandler = deactivateBudgetHandler
            ?? throw new ArgumentNullException(nameof(deactivateBudgetHandler));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        Title = "Orçamentos";

        var now = DateTime.Now;

        _selectedMonth = now.Month;
        _selectedYear = now.Year;

        Months = new[]
        {
            new MonthOption(1, "Janeiro"),
            new MonthOption(2, "Fevereiro"),
            new MonthOption(3, "Março"),
            new MonthOption(4, "Abril"),
            new MonthOption(5, "Maio"),
            new MonthOption(6, "Junho"),
            new MonthOption(7, "Julho"),
            new MonthOption(8, "Agosto"),
            new MonthOption(9, "Setembro"),
            new MonthOption(10, "Outubro"),
            new MonthOption(11, "Novembro"),
            new MonthOption(12, "Dezembro")
        };

        var currentYear = now.Year;

        Years = Enumerable
            .Range(currentYear - 1, 4)
            .ToArray();

        RefreshCommand =
            new AsyncRelayCommand(
                RefreshAsync,
                () => !IsBusy);

        DeactivateBudgetCommand =
            new AsyncRelayCommand(
                DeactivateSelectedBudgetAsync,
                CanDeactivateBudget);

        ShowAddBudgetFormCommand =
            new AsyncRelayCommand(
                ShowAddBudgetFormAsync,
                () => !IsBusy);

        BudgetForm.BudgetSaved +=
            OnBudgetSaved;

        BudgetForm.Cancelled +=
            OnBudgetFormCancelled;
    }

    public ObservableCollection<BudgetDTO> Budgets { get; }
        = new();

    public BudgetFormViewModel BudgetForm { get; }

    public IReadOnlyList<MonthOption> Months { get; }

    public IReadOnlyList<int> Years { get; }

    public BudgetDTO? SelectedBudget
    {
        get => _selectedBudget;

        set
        {
            if (!SetProperty(ref _selectedBudget, value))
            {
                return;
            }

            if (value is not null)
            {
                BudgetForm.SetBudgetForEdit(value);
            }

            RaiseCommandStates();
        }
    }

    public MonthlyBudgetSummaryDTO? Summary
    {
        get => _summary;

        private set =>
            SetProperty(ref _summary, value);
    }

    public int SelectedMonth
    {
        get => _selectedMonth;

        set
        {
            if (!SetProperty(ref _selectedMonth, value))
            {
                return;
            }

            _ = RefreshAsync();
        }
    }

    public int SelectedYear
    {
        get => _selectedYear;

        set
        {
            if (!SetProperty(ref _selectedYear, value))
            {
                return;
            }

            _ = RefreshAsync();
        }
    }

    public ICommand RefreshCommand { get; }

    public ICommand DeactivateBudgetCommand { get; }

    public ICommand ShowAddBudgetFormCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        await BudgetForm.LoadDependenciesAsync(
            cancellationToken);

        await LoadDataAsync(
            cancellationToken);

        await BudgetForm.PrepareNewAsync(
            SelectedMonth,
            SelectedYear,
            cancellationToken);

        IsInitialized = true;
    }

    private async Task RefreshAsync()
    {
        await RunSafeAsync(async () =>
        {
            await LoadDataAsync(
                CancellationToken.None);
        });

        RaiseCommandStates();
    }

    private async Task LoadDataAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            await NotificationService.ShowErrorAsync(
                "Não existe um utilizador autenticado.",
                "Sessão");

            return;
        }

        var budgetsResult =
            await _getBudgetsHandler.HandleAsync(
                new GetBudgetsByUserAndPeriodQuery(
                    userId,
                    SelectedMonth,
                    SelectedYear),
                cancellationToken);

        if (budgetsResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                budgetsResult.Error.Message,
                "Orçamentos");

            return;
        }

        Budgets.Clear();

        foreach (var budget in budgetsResult.Value)
        {
            Budgets.Add(budget);
        }

        var summaryResult =
            await _getSummaryHandler.HandleAsync(
                new GetMonthlyBudgetSummaryQuery(
                    userId,
                    SelectedMonth,
                    SelectedYear),
                cancellationToken);

        if (summaryResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                summaryResult.Error.Message,
                "Resumo mensal");

            Summary = null;
            return;
        }

        Summary = summaryResult.Value;

        SelectedBudget = null;
    }

    private async Task ShowAddBudgetFormAsync()
    {
        SelectedBudget = null;

        await BudgetForm.PrepareNewAsync(
            SelectedMonth,
            SelectedYear,
            CancellationToken.None);

        RaiseCommandStates();
    }

    private bool CanDeactivateBudget()
    {
        return !IsBusy &&
               SelectedBudget is not null &&
               SelectedBudget.IsActive;
    }

    private async Task DeactivateSelectedBudgetAsync()
    {
        if (SelectedBudget is null)
        {
            return;
        }

        var budget = SelectedBudget;

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Deseja desativar o orçamento '{budget.Name}'?",
                "Confirmar");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _deactivateBudgetHandler.HandleAsync(
                    new DeactivateBudgetCommand(
                        budget.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Desativar orçamento");

                return;
            }

            await LoadDataAsync(
                CancellationToken.None);

            await BudgetForm.PrepareNewAsync(
                SelectedMonth,
                SelectedYear,
                CancellationToken.None);

            await NotificationService.ShowInformationAsync(
                "Orçamento desativado com sucesso.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    private void OnBudgetSaved(
        BudgetDTO savedBudget)
    {
        _ = HandleBudgetSavedAsync(savedBudget);
    }

    private async Task HandleBudgetSavedAsync(
        BudgetDTO savedBudget)
    {
        await LoadDataAsync(
            CancellationToken.None);

        await BudgetForm.PrepareNewAsync(
            SelectedMonth,
            SelectedYear,
            CancellationToken.None);

        SelectedBudget = null;

        await NotificationService.ShowInformationAsync(
            "Orçamento guardado com sucesso.",
            "Sucesso");
    }

    private void OnBudgetFormCancelled()
    {
        _ = CancelBudgetFormAsync();
    }

    private async Task CancelBudgetFormAsync()
    {
        SelectedBudget = null;

        await BudgetForm.PrepareNewAsync(
            SelectedMonth,
            SelectedYear,
            CancellationToken.None);
    }

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        userId =
            _currentUserService.UserId
            ?? Guid.Empty;

        return userId != Guid.Empty;
    }

    private void RaiseCommandStates()
    {
        if (RefreshCommand is AsyncRelayCommand refresh)
        {
            refresh.RaiseCanExecuteChanged();
        }

        if (DeactivateBudgetCommand
            is AsyncRelayCommand deactivate)
        {
            deactivate.RaiseCanExecuteChanged();
        }

        if (ShowAddBudgetFormCommand
            is AsyncRelayCommand add)
        {
            add.RaiseCanExecuteChanged();
        }
    }
}