using System;
using System.Threading;
using System.Threading.Tasks;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Finance;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.FinancialPlans;

namespace Family_Management.WPF.ViewModel.Finance;

public sealed class UniversalFinanceViewModel : BaseViewModel
{
    private readonly FinanceContextService _financeContext;

    private readonly GetActiveFamilyFinancialPlanHandler
        _getActiveFamilyFinancialPlanHandler;

    private readonly GetFinancialPlanSummaryHandler
        _getFinancialPlanSummaryHandler;

    private FinanceScope _selectedScope;
    private bool _canUseFamilyScope;
    private string _contextDescription = string.Empty;

    private bool _isFinancialPlanLoading;
    private bool _hasFinancialPlanSummary;

    private string _financialPlanName = string.Empty;
    private string _financialPlanCurrency = "EUR";
    private string _financialPlanStatus = string.Empty;

    private decimal _totalIncome;
    private decimal _totalExpenses;
    private decimal _financialResult;

    private long _contextVersion;

    public UniversalFinanceViewModel(
        FinanceContextService financeContext,
        GetActiveFamilyFinancialPlanHandler
            getActiveFamilyFinancialPlanHandler,
        GetFinancialPlanSummaryHandler
            getFinancialPlanSummaryHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(navigationService, notificationService)
    {
        _financeContext = financeContext
            ?? throw new ArgumentNullException(
                nameof(financeContext));

        _getActiveFamilyFinancialPlanHandler =
            getActiveFamilyFinancialPlanHandler
            ?? throw new ArgumentNullException(
                nameof(getActiveFamilyFinancialPlanHandler));

        _getFinancialPlanSummaryHandler =
            getFinancialPlanSummaryHandler
            ?? throw new ArgumentNullException(
                nameof(getFinancialPlanSummaryHandler));

        Title = "Gestão Financeira";

        _selectedScope = FinanceScope.Personal;

        SelectPersonalCommand = new AsyncRelayCommand(
            () => SelectScopeAsync(FinanceScope.Personal),
            () => !IsBusy);

        SelectFamilyCommand = new AsyncRelayCommand(
            () => SelectScopeAsync(FinanceScope.Family),
            () => !IsBusy && CanUseFamilyScope);

        OpenAccountsCommand = CreateNavigationCommand(
            AppScreen.Accounts);

        OpenTransactionsCommand = CreateNavigationCommand(
            AppScreen.Transactions);

        OpenBudgetsCommand = CreateNavigationCommand(
            AppScreen.Budget);

        OpenGoalsCommand = CreateNavigationCommand(
            AppScreen.Goal);

        RefreshCommand = new AsyncRelayCommand(
            RefreshAsync,
            () => !IsBusy);

        _financeContext.ContextChanged += OnContextChanged;

        UpdateContextProperties();
    }

    public FinanceScope SelectedScope
    {
        get => _selectedScope;

        private set
        {
            if (SetProperty(ref _selectedScope, value))
            {
                OnPropertyChanged(
                    nameof(IsPersonalScope),
                    nameof(IsFamilyScope));
            }
        }
    }

    public bool IsPersonalScope =>
        SelectedScope == FinanceScope.Personal;

    public bool IsFamilyScope =>
        SelectedScope == FinanceScope.Family;

    public bool CanUseFamilyScope
    {
        get => _canUseFamilyScope;

        private set
        {
            if (SetProperty(ref _canUseFamilyScope, value))
            {
                SelectFamilyCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ContextDescription
    {
        get => _contextDescription;

        private set => SetProperty(
            ref _contextDescription,
            value);
    }

    public bool IsFinancialPlanLoading
    {
        get => _isFinancialPlanLoading;

        private set => SetProperty(
            ref _isFinancialPlanLoading,
            value);
    }

    public bool HasFinancialPlanSummary
    {
        get => _hasFinancialPlanSummary;

        private set => SetProperty(
            ref _hasFinancialPlanSummary,
            value);
    }

    public string FinancialPlanName
    {
        get => _financialPlanName;

        private set => SetProperty(
            ref _financialPlanName,
            value);
    }

    public string FinancialPlanCurrency
    {
        get => _financialPlanCurrency;

        private set => SetProperty(
            ref _financialPlanCurrency,
            value);
    }

    public string FinancialPlanStatus
    {
        get => _financialPlanStatus;

        private set => SetProperty(
            ref _financialPlanStatus,
            value);
    }

    public decimal TotalIncome
    {
        get => _totalIncome;

        private set => SetProperty(
            ref _totalIncome,
            value);
    }

    public decimal TotalExpenses
    {
        get => _totalExpenses;

        private set => SetProperty(
            ref _totalExpenses,
            value);
    }

    public decimal FinancialResult
    {
        get => _financialResult;

        private set => SetProperty(
            ref _financialResult,
            value);
    }

    public AsyncRelayCommand SelectPersonalCommand { get; }

    public AsyncRelayCommand SelectFamilyCommand { get; }

    public AsyncRelayCommand OpenAccountsCommand { get; }

    public AsyncRelayCommand OpenTransactionsCommand { get; }

    public AsyncRelayCommand OpenBudgetsCommand { get; }

    public AsyncRelayCommand OpenGoalsCommand { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await _financeContext.RefreshAsync(cancellationToken);

        UpdateContextProperties();

        if (IsFamilyScope)
        {
            await LoadFinancialPlanAsync();
        }

        IsInitialized = true;
    }

    private AsyncRelayCommand CreateNavigationCommand(
        AppScreen screen)
    {
        return new AsyncRelayCommand(
            () => NavigateAsync(screen),
            () => !IsBusy);
    }

    private async Task NavigateAsync(AppScreen screen)
    {
        await RunSafeAsync(async () =>
        {
            if (!_financeContext.UserId.HasValue)
            {
                await NotificationService.ShowErrorAsync(
                    "É necessário iniciar sessão.",
                    "Sessão inválida");

                return;
            }

            await NavigationService.NavigateToScreen(screen);
        });
    }

    private async Task SelectScopeAsync(FinanceScope scope)
    {
        await RunSafeAsync(async () =>
        {
            if (!_financeContext.TrySelectScope(scope))
            {
                await NotificationService.ShowErrorAsync(
                    "O contexto financeiro selecionado não está disponível.",
                    "Contexto indisponível");

                return;
            }

            UpdateContextProperties();

            if (IsFamilyScope)
            {
                await LoadFinancialPlanAsync();
            }
        });
    }

    private async Task RefreshAsync()
    {
        await RunSafeAsync(async () =>
        {
            await _financeContext.RefreshAsync();

            UpdateContextProperties();

            if (IsFamilyScope)
            {
                await LoadFinancialPlanAsync();
            }
        });
    }

    private async Task LoadFinancialPlanAsync()
    {
        var familyId = _financeContext.ActiveFamilyId;
        var userId = _financeContext.UserId;

        if (!familyId.HasValue || !userId.HasValue)
        {
            ClearFinancialPlan();
            return;
        }

        var version = _contextVersion;

        IsFinancialPlanLoading = true;
        HasFinancialPlanSummary = false;

        FinancialPlanStatus =
            "A carregar o plano financeiro familiar...";

        try
        {
            var planResult =
                await _getActiveFamilyFinancialPlanHandler
                    .HandleAsync(
                        familyId.Value,
                        userId.Value);

            if (!IsCurrentContext(
                    version,
                    familyId.Value,
                    userId.Value))
            {
                return;
            }

            if (planResult.IsFailure)
            {
                ClearFinancialPlan();

                FinancialPlanStatus =
                    "Não existe um plano financeiro familiar ativo " +
                    "disponível para consulta.";

                return;
            }

            var summaryResult =
                await _getFinancialPlanSummaryHandler
                    .HandleAsync(
                        planResult.Value.Id,
                        userId.Value);

            if (!IsCurrentContext(
                    version,
                    familyId.Value,
                    userId.Value))
            {
                return;
            }

            if (summaryResult.IsFailure)
            {
                ClearFinancialPlan();

                FinancialPlanStatus =
                    "Não foi possível consultar o resumo " +
                    "do plano financeiro familiar.";

                return;
            }

            var summary = summaryResult.Value;

            FinancialPlanName = summary.Name;
            FinancialPlanCurrency = summary.CurrencyCode;

            TotalIncome = summary.TotalIncome;
            TotalExpenses = summary.TotalExpenses;
            FinancialResult = summary.Balance;

            HasFinancialPlanSummary = true;

            FinancialPlanStatus = string.Empty;
        }
        catch (Exception)
        {
            if (IsCurrentContext(
                    version,
                    familyId.Value,
                    userId.Value))
            {
                ClearFinancialPlan();

                FinancialPlanStatus =
                    "Ocorreu um erro ao carregar " +
                    "o plano financeiro familiar.";
            }
        }
        finally
        {
            if (IsCurrentContext(
                    version,
                    familyId.Value,
                    userId.Value))
            {
                IsFinancialPlanLoading = false;
            }
        }
    }

    private bool IsCurrentContext(
        long version,
        Guid familyId,
        Guid userId)
    {
        return version == _contextVersion &&
               _financeContext.Scope == FinanceScope.Family &&
               _financeContext.ActiveFamilyId == familyId &&
               _financeContext.UserId == userId;
    }

    private void OnContextChanged()
    {
        _contextVersion++;

        UpdateContextProperties();

        ClearFinancialPlan();
    }

    private void ClearFinancialPlan()
    {
        IsFinancialPlanLoading = false;
        HasFinancialPlanSummary = false;

        FinancialPlanName = string.Empty;
        FinancialPlanCurrency = string.Empty;

        TotalIncome = 0m;
        TotalExpenses = 0m;
        FinancialResult = 0m;

        FinancialPlanStatus = string.Empty;
    }

    private void UpdateContextProperties()
    {
        SelectedScope = _financeContext.Scope;

        CanUseFamilyScope = _financeContext.CanUseFamilyScope;

        ContextDescription = SelectedScope switch
        {
            FinanceScope.Family =>
                "Contexto familiar selecionado. " +
                "As operações devem respeitar as permissões da família.",

            _ =>
                "Contexto pessoal selecionado. " +
                "As operações devem apresentar apenas os dados pessoais."
        };

        SelectPersonalCommand.RaiseCanExecuteChanged();
        SelectFamilyCommand.RaiseCanExecuteChanged();
    }
}