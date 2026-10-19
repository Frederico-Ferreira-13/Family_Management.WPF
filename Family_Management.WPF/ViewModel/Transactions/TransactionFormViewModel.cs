using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Finance;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.Validation;
using Family_Management.WPF.ViewModel.Common;

using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;
using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetsByUserAndPeriod;
using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Queries.GetAvailableCategoriesForUser;
using FamilyManagement.Application.UseCases.FinancialPlans;
using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Queries.GetActiveGoalsByUser;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.CreateRecurringTransaction;
using FamilyManagement.Application.UseCases.Transactions.Commands.CreateTransaction;
using FamilyManagement.Application.UseCases.Transactions.Commands.CreateTransfer;
using FamilyManagement.Application.UseCases.Transactions.Commands.UpdateTransaction;
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Queries.GetTransactionById;

// BENEFICIÁRIOS
using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Queries.GetUsersByFamily;

using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Transactions;

public sealed class TransactionFormViewModel :
    BaseViewModel,
    INotifyDataErrorInfo,
    IParameterReceiver<Guid>
{
    // ============================================================
    // SERVICES
    // ============================================================

    private readonly CurrentUserService _currentUserService;
    private readonly FinanceContextService _financeContextService;

    // ============================================================
    // COMMAND HANDLERS
    // ============================================================

    private readonly CreateTransactionHandler _createTransactionHandler;
    private readonly CreateTransferHandler _createTransferHandler;
    private readonly UpdateTransactionHandler _updateTransactionHandler;

    private readonly CreateRecurringTransactionHandler
        _createRecurringTransactionHandler;

    // ============================================================
    // QUERY HANDLERS
    // ============================================================

    private readonly GetTransactionByIdHandler
        _getTransactionByIdHandler;

    private readonly GetAccountsByUserHandler
        _getAccountsByUserHandler;

    private readonly GetAvailableCategoriesForUserHandler
        _getCategoriesHandler;

    private readonly GetBudgetsByUserAndPeriodHandler
        _getBudgetsHandler;

    private readonly GetActiveGoalsByUserHandler
        _getGoalsHandler;

    private readonly GetActiveFamilyFinancialPlanHandler
        _getActiveFamilyFinancialPlanHandler;

    // NOVO: carregar membros da família.
    private readonly GetUsersByFamilyHandler
        _getUsersByFamilyHandler;

    // ============================================================
    // VALIDATOR
    // ============================================================

    private readonly TransactionValidator _validator = new();

    // ============================================================
    // PRIVATE FIELDS
    // ============================================================

    private Guid? _transactionId;

    private DateTime _date = DateTime.Today;
    private decimal _amount;
    private string _description = string.Empty;
    private string? _notes;

    private TransactionType _type = TransactionType.Expense;
    private bool _isConfirmed = true;

    // ============================================================
    // RECURRENCE FIELDS
    // ============================================================

    private bool _isRecurring;
    private Frequency _frequency = Frequency.Monthly;
    private DateTime _startDate = DateTime.Today;
    private DateTime? _endDate;

    // ============================================================
    // FINANCIAL PLAN FIELDS
    // ============================================================

    private Guid? _activeFinancialPlanId;
    private string? _activeFinancialPlanName;
    private string? _activeFinancialPlanCurrency;
    private Guid? _activeFamilyId;

    // ============================================================
    // SELECTION FIELDS
    // ============================================================

    private AccountDTO? _selectedAccount;
    private AccountDTO? _selectedTargetAccount;
    private CategoryDTO? _selectedCategory;
    private BudgetDTO? _selectedBudget;
    private GoalDTO? _selectedGoal;

    // NOVO: membro que recebe a receita recorrente.
    private UserDTO? _selectedBeneficiary;

    private bool _referenceDataLoaded;

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public TransactionFormViewModel(
        CurrentUserService currentUserService,
        CreateTransactionHandler createTransactionHandler,
        CreateTransferHandler createTransferHandler,
        UpdateTransactionHandler updateTransactionHandler,
        GetTransactionByIdHandler getTransactionByIdHandler,
        GetAccountsByUserHandler getAccountsByUserHandler,
        GetAvailableCategoriesForUserHandler getCategoriesHandler,
        GetBudgetsByUserAndPeriodHandler getBudgetsHandler,
        GetActiveGoalsByUserHandler getGoalsHandler,
        INavigationService navigationService,
        INotificationService notificationService,
        FinanceContextService financeContextService,
        GetActiveFamilyFinancialPlanHandler
            getActiveFamilyFinancialPlanHandler,
        CreateRecurringTransactionHandler
            createRecurringTransactionHandler,
        GetUsersByFamilyHandler getUsersByFamilyHandler)
        : base(navigationService, notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _createTransactionHandler = createTransactionHandler
            ?? throw new ArgumentNullException(
                nameof(createTransactionHandler));

        _createTransferHandler = createTransferHandler
            ?? throw new ArgumentNullException(
                nameof(createTransferHandler));

        _updateTransactionHandler = updateTransactionHandler
            ?? throw new ArgumentNullException(
                nameof(updateTransactionHandler));

        _getTransactionByIdHandler = getTransactionByIdHandler
            ?? throw new ArgumentNullException(
                nameof(getTransactionByIdHandler));

        _getAccountsByUserHandler = getAccountsByUserHandler
            ?? throw new ArgumentNullException(
                nameof(getAccountsByUserHandler));

        _getCategoriesHandler = getCategoriesHandler
            ?? throw new ArgumentNullException(
                nameof(getCategoriesHandler));

        _getBudgetsHandler = getBudgetsHandler
            ?? throw new ArgumentNullException(
                nameof(getBudgetsHandler));

        _getGoalsHandler = getGoalsHandler
            ?? throw new ArgumentNullException(
                nameof(getGoalsHandler));

        _financeContextService = financeContextService
            ?? throw new ArgumentNullException(
                nameof(financeContextService));

        _getActiveFamilyFinancialPlanHandler =
            getActiveFamilyFinancialPlanHandler
            ?? throw new ArgumentNullException(
                nameof(getActiveFamilyFinancialPlanHandler));

        _createRecurringTransactionHandler =
            createRecurringTransactionHandler
            ?? throw new ArgumentNullException(
                nameof(createRecurringTransactionHandler));

        // NOVO
        _getUsersByFamilyHandler = getUsersByFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(getUsersByFamilyHandler));

        _validator.ErrorsChanged += OnValidatorErrorsChanged;

        Title = "Nova transação";

        SaveCommand = new AsyncRelayCommand(
            ExecuteSaveAsync,
            CanExecuteSave);

        CancelCommand = new RelayCommand(
            ExecuteCancel);
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    public event EventHandler<DataErrorsChangedEventArgs>?
        ErrorsChanged;

    public bool HasErrors => _validator.HasErrors;

    public IEnumerable GetErrors(string? propertyName)
    {
        return _validator.GetErrors(propertyName);
    }

    // ============================================================
    // FORM STATE
    // ============================================================

    public bool IsEditMode =>
        _transactionId.HasValue &&
        _transactionId.Value != Guid.Empty;

    public bool CanEditTransactionStructure => !IsEditMode;

    public bool IsTransfer =>
        Type == TransactionType.Transfer;

    public bool UsesCategory => !IsTransfer;

    public bool IsOneTime
    {
        get => !IsRecurring;

        set
        {
            if (value)
            {
                IsRecurring = false;
            }
        }
    }

    public bool CanSelectBudget =>
        !IsRecurring &&
        Type == TransactionType.Expense;

    public bool CanSelectGoal =>
        !IsRecurring &&
        Type == TransactionType.Expense;

    public bool HasActiveFinancialPlan =>
        _activeFinancialPlanId.HasValue;

    public bool CanCreateWithoutAccount =>
        !IsTransfer &&
        !IsEditMode &&
        _activeFamilyId.HasValue &&
        _activeFinancialPlanId.HasValue;

    public string FinancialPlanDisplay =>
        HasActiveFinancialPlan
            ? $"{_activeFinancialPlanName} ({_activeFinancialPlanCurrency})"
            : "Sem plano financeiro familiar ativo";

    public string TransactionModeDescription =>
        IsRecurring
            ? "Define um movimento que se repete automaticamente."
            : "Regista um movimento numa data específica.";

    // ============================================================
    // BENEFICIARY STATE
    // ============================================================

    /// <summary>
    /// Indica se o formulário deve apresentar
    /// a seleção do beneficiário.
    ///
    /// Apenas para novas receitas recorrentes familiares.
    /// </summary>
    public bool CanSelectBeneficiary =>
        !IsEditMode &&
        IsRecurring &&
        Type == TransactionType.Income &&
        _activeFamilyId.HasValue;

    public bool HasFamilyMembers =>
        FamilyMembers.Count > 0;

    public bool HasMultipleFamilyMembers =>
        FamilyMembers.Count > 1;

    // ============================================================
    // COLLECTIONS
    // ============================================================

    public ObservableCollection<AccountDTO> Accounts { get; } =
        new();

    public ObservableCollection<CategoryDTO> Categories { get; } =
        new();

    public ObservableCollection<CategoryDTO>
        FilteredCategories { get; } = new();

    public ObservableCollection<BudgetDTO> Budgets { get; } =
        new();

    public ObservableCollection<GoalDTO> Goals { get; } =
        new();

    // NOVO
    public ObservableCollection<UserDTO> FamilyMembers { get; } =
        new();

    public IReadOnlyCollection<TransactionType>
        TransactionTypes { get; } =
            new[]
            {
                TransactionType.Income,
                TransactionType.Expense,
                TransactionType.Transfer
            };

    public IReadOnlyList<FrequencyOption> Frequencies { get; } =
        TransactionFormOptions.Frequencies.ToList();

    public IReadOnlyList<TransactionTypeOption>
        TransactionTypeOptions { get; } =
            TransactionFormOptions.TransactionTypes.ToList();

    // ============================================================
    // GENERAL PROPERTIES
    // ============================================================

    public DateTime Date
    {
        get => _date;

        set
        {
            if (!SetProperty(ref _date, value))
                return;

            ValidateTransaction();

            _ = ReloadBudgetsForDateAsync();

            RaiseCommandStates();
        }
    }

    public decimal Amount
    {
        get => _amount;

        set
        {
            if (!SetProperty(ref _amount, value))
                return;

            _validator.ValidateAmount(
                nameof(Amount),
                value);

            RaiseCommandStates();
        }
    }

    public string Description
    {
        get => _description;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(ref _description, value))
                return;

            _validator.ValidateDescription(
                nameof(Description),
                value);

            RaiseCommandStates();
        }
    }

    public string? Notes
    {
        get => _notes;

        set => SetProperty(ref _notes, value);
    }

    public TransactionType Type
    {
        get => _type;

        set
        {
            if (IsEditMode)
                return;

            if (!SetProperty(ref _type, value))
                return;

            if (value == TransactionType.Transfer)
            {
                if (IsRecurring)
                {
                    _isRecurring = false;

                    OnPropertyChanged(
                        nameof(IsRecurring),
                        nameof(IsOneTime),
                        nameof(TransactionModeDescription));
                }

                SelectedCategory = null;
                SelectedBudget = null;
                SelectedGoal = null;
            }
            else
            {
                SelectedTargetAccount = null;

                if (value != TransactionType.Expense)
                {
                    SelectedBudget = null;
                    SelectedGoal = null;
                }
            }

            RefreshFilteredCategories();

            NotifyTransactionTypeChanged();

            ValidateTransaction();

            RaiseCommandStates();
        }
    }

    public bool IsConfirmed
    {
        get => _isConfirmed;

        set
        {
            if (!SetProperty(ref _isConfirmed, value))
                return;

            ValidateTransaction();

            RaiseCommandStates();
        }
    }

    // ============================================================
    // RECURRENCE
    // ============================================================

    public bool IsRecurring
    {
        get => _isRecurring;

        set
        {
            if (IsEditMode)
                return;

            // Transferências recorrentes não são suportadas.
            if (value && IsTransfer)
                return;

            if (!SetProperty(ref _isRecurring, value))
                return;

            if (value)
            {
                SelectedBudget = null;
                SelectedGoal = null;
            }

            OnPropertyChanged(
                nameof(IsOneTime),
                nameof(CanSelectBudget),
                nameof(CanSelectGoal),
                nameof(CanSelectBeneficiary),
                nameof(TransactionModeDescription));

            ValidateTransaction();

            RaiseCommandStates();
        }
    }

    public Frequency Frequency
    {
        get => _frequency;

        set
        {
            if (!SetProperty(ref _frequency, value))
                return;

            ValidateRecurrence();

            RaiseCommandStates();
        }
    }

    public DateTime StartDate
    {
        get => _startDate;

        set
        {
            if (!SetProperty(ref _startDate, value))
                return;

            ValidateRecurrence();

            RaiseCommandStates();
        }
    }

    public DateTime? EndDate
    {
        get => _endDate;

        set
        {
            if (!SetProperty(ref _endDate, value))
                return;

            ValidateRecurrence();

            RaiseCommandStates();
        }
    }

    // ============================================================
    // SELECTIONS
    // ============================================================

    public AccountDTO? SelectedAccount
    {
        get => _selectedAccount;

        set
        {
            if (!SetProperty(ref _selectedAccount, value))
                return;

            ValidateTransaction();

            RaiseCommandStates();
        }
    }

    public AccountDTO? SelectedTargetAccount
    {
        get => _selectedTargetAccount;

        set
        {
            if (!SetProperty(ref _selectedTargetAccount, value))
                return;

            ValidateTargetAccount();

            RaiseCommandStates();
        }
    }

    public CategoryDTO? SelectedCategory
    {
        get => _selectedCategory;

        set
        {
            if (!SetProperty(ref _selectedCategory, value))
                return;

            _validator.ValidateCategory(
                nameof(SelectedCategory),
                Type,
                value);

            RaiseCommandStates();
        }
    }

    public BudgetDTO? SelectedBudget
    {
        get => _selectedBudget;

        set => SetProperty(ref _selectedBudget, value);
    }

    public GoalDTO? SelectedGoal
    {
        get => _selectedGoal;

        set => SetProperty(ref _selectedGoal, value);
    }

    // ============================================================
    // BENEFICIARY SELECTION
    // ============================================================

    /// <summary>
    /// Membro da família a quem pertence a receita recorrente.
    /// </summary>
    public UserDTO? SelectedBeneficiary
    {
        get => _selectedBeneficiary;

        set
        {
            if (!SetProperty(ref _selectedBeneficiary, value))
                return;

            RaiseCommandStates();
        }
    }

    // ============================================================
    // COMMANDS
    // ============================================================

    public AsyncRelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }

    // ============================================================
    // NAVIGATION PARAMETERS
    // ============================================================

    public void ReceiveParameter(Guid transactionId)
    {
        if (transactionId == Guid.Empty)
            return;

        _transactionId = transactionId;

        _isRecurring = false;

        OnPropertyChanged(
            nameof(IsEditMode),
            nameof(CanEditTransactionStructure),
            nameof(IsRecurring),
            nameof(IsOneTime),
            nameof(CanCreateWithoutAccount),
            nameof(CanSelectBeneficiary));

        Title = "Editar transação";

        RaiseCommandStates();
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        await RunSafeAsync(
            async () =>
            {
                var userId = GetCurrentUserId();

                if (!userId.HasValue)
                {
                    await NotificationService.ShowErrorAsync(
                        "Não existe um utilizador autenticado.",
                        "Transação");

                    return;
                }

                // 1. Contas, categorias, objetivos e orçamentos.
                await LoadReferenceDataAsync(
                    userId.Value,
                    cancellationToken);

                if (!_referenceDataLoaded)
                    return;

                // 2. Plano financeiro familiar.
                await LoadFinancialPlanAsync();

                // 3. NOVO: membros da família.
                await LoadFamilyMembersAsync(
                    cancellationToken);

                // 4. Carregar transação em edição.
                if (IsEditMode)
                {
                    await LoadTransactionAsync(
                        _transactionId!.Value);
                }

                RefreshFilteredCategories();

                ValidateTransaction();

                IsInitialized = true;
            },
            cancellationToken: cancellationToken);

        RaiseCommandStates();
    }

    // ============================================================
    // REFERENCE DATA
    // ============================================================

    private async Task LoadReferenceDataAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        _referenceDataLoaded = false;

        // ========================================================
        // ACCOUNTS
        // ========================================================

        var accountsResult =
            await _getAccountsByUserHandler.HandleAsync(
                new GetAccountsByUserQuery(userId),
                cancellationToken);

        if (accountsResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                accountsResult.Error.Message,
                "Contas");

            return;
        }

        Accounts.Clear();

        foreach (var account in accountsResult.Value
                     .Where(account => account.IsActive)
                     .OrderBy(account => account.Name))
        {
            Accounts.Add(account);
        }

        // ========================================================
        // CATEGORIES
        // ========================================================

        Guid? activeFamilyId =
            _financeContextService.Scope == FinanceScope.Family
                ? _financeContextService.ActiveFamilyId
                : null;

        var categoriesResult =
            await _getCategoriesHandler.HandleAsync(
                new GetAvailableCategoriesForUserQuery(
                    UserId: userId,
                    FamilyId: activeFamilyId),
                cancellationToken);

        if (categoriesResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                categoriesResult.Error.Message,
                "Categorias");

            return;
        }

        Categories.Clear();

        foreach (var category in categoriesResult.Value
                     .Where(category => category.IsActive)
                     .OrderBy(category => category.Name))
        {
            Categories.Add(category);
        }

        RefreshFilteredCategories();

        // ========================================================
        // GOALS
        // ========================================================

        var goalsResult =
            await _getGoalsHandler.HandleAsync(
                new GetActiveGoalsByUserQuery(userId),
                cancellationToken);

        if (goalsResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                goalsResult.Error.Message,
                "Metas");

            return;
        }

        Goals.Clear();

        foreach (var goal in goalsResult.Value)
        {
            Goals.Add(goal);
        }

        // ========================================================
        // BUDGETS
        // ========================================================

        await LoadBudgetsAsync(
            userId,
            Date,
            cancellationToken);

        _referenceDataLoaded = true;
    }

    private async Task LoadBudgetsAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _getBudgetsHandler.HandleAsync(
                new GetBudgetsByUserAndPeriodQuery(
                    userId,
                    date.Month,
                    date.Year),
                cancellationToken);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Orçamentos");

            return;
        }

        Budgets.Clear();

        foreach (var budget in result.Value)
        {
            Budgets.Add(budget);
        }

        if (SelectedBudget is not null &&
            !Budgets.Any(budget =>
                budget.Id == SelectedBudget.Id))
        {
            SelectedBudget = null;
        }
    }

    private async Task ReloadBudgetsForDateAsync()
    {
        if (!IsInitialized || IsRecurring)
            return;

        var userId = GetCurrentUserId();

        if (!userId.HasValue)
            return;

        try
        {
            await LoadBudgetsAsync(
                userId.Value,
                Date);
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync(
                $"Não foi possível carregar os orçamentos. {ex.Message}",
                "Orçamentos");
        }
    }

    // ============================================================
    // FINANCIAL PLAN
    // ============================================================

    private async Task LoadFinancialPlanAsync()
    {
        _activeFinancialPlanId = null;
        _activeFinancialPlanName = null;
        _activeFinancialPlanCurrency = null;
        _activeFamilyId = null;

        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            NotifyFinancialPlanChanged();
            return;
        }

        if (_financeContextService.Scope != FinanceScope.Family ||
            !_financeContextService.ActiveFamilyId.HasValue)
        {
            NotifyFinancialPlanChanged();
            return;
        }

        var familyId =
            _financeContextService.ActiveFamilyId.Value;

        var result =
            await _getActiveFamilyFinancialPlanHandler.HandleAsync(
                familyId,
                userId.Value);

        if (result.IsSuccess)
        {
            _activeFamilyId = familyId;
            _activeFinancialPlanId = result.Value.Id;
            _activeFinancialPlanName = result.Value.Name;
            _activeFinancialPlanCurrency = result.Value.CurrencyCode;
        }

        NotifyFinancialPlanChanged();
    }

    private void NotifyFinancialPlanChanged()
    {
        OnPropertyChanged(
            nameof(HasActiveFinancialPlan),
            nameof(FinancialPlanDisplay),
            nameof(CanCreateWithoutAccount),
            nameof(CanSelectBeneficiary));

        ValidateTransaction();

        RaiseCommandStates();
    }

    // ============================================================
    // FAMILY MEMBERS
    // ============================================================

    /// <summary>
    /// Carrega os utilizadores ativos da família
    /// para permitir associar receitas recorrentes
    /// ao respetivo beneficiário.
    /// </summary>
    private async Task LoadFamilyMembersAsync(
        CancellationToken cancellationToken = default)
    {
        FamilyMembers.Clear();

        SelectedBeneficiary = null;

        OnPropertyChanged(
            nameof(HasFamilyMembers),
            nameof(HasMultipleFamilyMembers));

        if (!_activeFamilyId.HasValue)
        {
            OnPropertyChanged(
                nameof(CanSelectBeneficiary));

            RaiseCommandStates();

            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var query = new GetUsersByFamilyQuery(
            _activeFamilyId.Value);

        // O handler atual não recebe CancellationToken.
        var result =
            await _getUsersByFamilyHandler.HandleAsync(query);

        cancellationToken.ThrowIfCancellationRequested();

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Membros da família");

            return;
        }

        foreach (var member in result.Value
                     .OrderBy(member => member.UserName))
        {
            FamilyMembers.Add(member);
        }

        // Quando só existe um membro, selecioná-lo
        // automaticamente para facilitar o registo.
        if (FamilyMembers.Count == 1)
        {
            SelectedBeneficiary = FamilyMembers[0];
        }

        OnPropertyChanged(
            nameof(HasFamilyMembers),
            nameof(HasMultipleFamilyMembers),
            nameof(CanSelectBeneficiary));

        RaiseCommandStates();
    }

    // ============================================================
    // CATEGORY FILTER
    // ============================================================

    private void RefreshFilteredCategories()
    {
        FilteredCategories.Clear();

        foreach (var category in
                 TransactionCategoryFilter.Filter(
                     Categories,
                     Type))
        {
            FilteredCategories.Add(category);
        }

        if (SelectedCategory is not null &&
            !FilteredCategories.Any(category =>
                category.Id == SelectedCategory.Id))
        {
            SelectedCategory = null;
        }
    }

    private void NotifyTransactionTypeChanged()
    {
        OnPropertyChanged(
            nameof(IsTransfer),
            nameof(UsesCategory),
            nameof(CanSelectBudget),
            nameof(CanSelectGoal),
            nameof(CanCreateWithoutAccount),
            nameof(CanSelectBeneficiary));
    }

    // ============================================================
    // LOAD EXISTING TRANSACTION
    // ============================================================

    private async Task LoadTransactionAsync(
        Guid transactionId)
    {
        var result =
            await _getTransactionByIdHandler.HandleAsync(
                new GetTransactionByIdQuery(transactionId));

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Transação");

            return;
        }

        ApplyTransaction(result.Value);
    }

    private void ApplyTransaction(
        TransactionDTO transaction)
    {
        _date = transaction.Date;
        _amount = transaction.Amount;
        _description = transaction.Description;
        _notes = transaction.Notes;
        _type = transaction.Type;
        _isConfirmed = transaction.IsConfirmed;
        _isRecurring = false;

        _selectedAccount =
            Accounts.FirstOrDefault(account =>
                account.Id == transaction.AccountId);

        _selectedTargetAccount =
            transaction.TargetAccountId.HasValue
                ? Accounts.FirstOrDefault(account =>
                    account.Id ==
                    transaction.TargetAccountId.Value)
                : null;

        _selectedCategory =
            transaction.CategoryId.HasValue
                ? Categories.FirstOrDefault(category =>
                    category.Id ==
                    transaction.CategoryId.Value)
                : null;

        _selectedBudget =
            transaction.BudgetId.HasValue
                ? Budgets.FirstOrDefault(budget =>
                    budget.Id ==
                    transaction.BudgetId.Value)
                : null;

        _selectedGoal =
            transaction.GoalId.HasValue
                ? Goals.FirstOrDefault(goal =>
                    goal.Id ==
                    transaction.GoalId.Value)
                : null;

        RefreshFilteredCategories();

        OnPropertyChanged(
            nameof(Date),
            nameof(Amount),
            nameof(Description),
            nameof(Notes),
            nameof(Type),
            nameof(IsConfirmed),
            nameof(IsRecurring),
            nameof(IsOneTime),
            nameof(SelectedAccount),
            nameof(SelectedTargetAccount),
            nameof(SelectedCategory),
            nameof(SelectedBudget),
            nameof(SelectedGoal),
            nameof(CanSelectBeneficiary),
            nameof(TransactionModeDescription));

        NotifyTransactionTypeChanged();

        ValidateTransaction();

        RaiseCommandStates();
    }

    // ============================================================
    // SAVE VALIDATION
    // ============================================================

    private bool CanExecuteSave()
    {
        if (IsBusy || HasErrors || !_referenceDataLoaded)
            return false;

        if (!GetCurrentUserId().HasValue)
            return false;

        if (Amount <= 0 ||
            string.IsNullOrWhiteSpace(Description))
        {
            return false;
        }

        // ========================================================
        // EDIT MODE
        // ========================================================

        if (IsEditMode)
        {
            // O handler atual de edição exige uma conta.
            return SelectedAccount is not null &&
                   (IsTransfer || SelectedCategory is not null);
        }

        // ========================================================
        // TRANSFER
        // ========================================================

        if (IsTransfer)
        {
            return !IsRecurring &&
                   SelectedAccount is not null &&
                   SelectedTargetAccount is not null;
        }

        // ========================================================
        // CATEGORY
        // ========================================================

        if (SelectedCategory is null)
            return false;

        // ========================================================
        // ACCOUNT OR FINANCIAL PLAN
        // ========================================================

        if (SelectedAccount is null &&
            !CanCreateWithoutAccount)
        {
            return false;
        }

        // ========================================================
        // RECURRING TRANSACTION
        // ========================================================

        if (IsRecurring)
        {
            // NOVO:
            // Receitas recorrentes familiares exigem beneficiário.
            if (CanSelectBeneficiary &&
                SelectedBeneficiary is null)
            {
                return false;
            }

            return StartDate != default &&
                   (!EndDate.HasValue ||
                    EndDate.Value.Date >= StartDate.Date) &&
                   Enum.IsDefined(Frequency);
        }

        // ========================================================
        // ONE-TIME TRANSACTION
        // ========================================================

        return Date != default &&
               (!IsConfirmed || Date.Date <= DateTime.Today);
    }

    // ============================================================
    // SAVE
    // ============================================================

    private async Task ExecuteSaveAsync()
    {
        ValidateTransaction();

        if (!CanExecuteSave())
            return;

        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            await NotificationService.ShowErrorAsync(
                "Não existe um utilizador autenticado.",
                "Transação");

            return;
        }

        await RunSafeAsync(
            async () =>
            {
                if (IsEditMode)
                {
                    await UpdateTransactionAsync();
                    return;
                }

                if (IsRecurring)
                {
                    await CreateRecurringAsync(userId.Value);
                    return;
                }

                if (IsTransfer)
                {
                    await CreateTransferAsync(userId.Value);
                    return;
                }

                await CreateIncomeOrExpenseAsync(userId.Value);
            });

        RaiseCommandStates();
    }

    // ============================================================
    // CREATE INCOME / EXPENSE
    // ============================================================

    private async Task CreateIncomeOrExpenseAsync(
        Guid userId)
    {
        if (SelectedCategory is null)
            return;

        var currency =
            SelectedAccount?.Currency ??
            _activeFinancialPlanCurrency;

        if (string.IsNullOrWhiteSpace(currency))
        {
            await NotificationService.ShowErrorAsync(
                "Não foi possível determinar a moeda do movimento.",
                "Transação");

            return;
        }

        var command = new CreateTransactionCommand(
            Date: Date,
            Amount: Amount,
            Currency: currency,
            Description: Description.Trim(),
            Notes: NormalizeNotes(Notes),
            Type: Type,
            AccountId: SelectedAccount?.Id,
            UserId: userId,
            CategoryId: SelectedCategory.Id,
            BudgetId:
                Type == TransactionType.Expense
                    ? SelectedBudget?.Id
                    : null,
            GoalId:
                Type == TransactionType.Expense
                    ? SelectedGoal?.Id
                    : null,
            IsConfirmed: IsConfirmed,
            FamilyId: _activeFamilyId,
            FinancialPlanId: _activeFinancialPlanId);

        var result =
            await _createTransactionHandler.HandleAsync(command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Transação");

            return;
        }

        await CompleteSaveAsync();
    }

    // ============================================================
    // CREATE RECURRING TRANSACTION
    // ============================================================

    private async Task CreateRecurringAsync(Guid userId)
    {
        if (SelectedCategory is null)
            return;

        if (Type == TransactionType.Transfer)
        {
            await NotificationService.ShowErrorAsync(
                "As transferências recorrentes ainda não estão disponíveis.",
                "Recorrência");

            return;
        }

        // ========================================================
        // VALIDATE BENEFICIARY
        // ========================================================

        if (CanSelectBeneficiary &&
            SelectedBeneficiary is null)
        {
            await NotificationService.ShowErrorAsync(
                "Seleciona o membro da família que recebe este rendimento.",
                "Beneficiário");

            return;
        }

        // ========================================================
        // CURRENCY
        // ========================================================

        var currency =
            SelectedAccount?.Currency ??
            _activeFinancialPlanCurrency;

        if (string.IsNullOrWhiteSpace(currency))
        {
            await NotificationService.ShowErrorAsync(
                "Não foi possível determinar a moeda da recorrência.",
                "Recorrência");

            return;
        }

        // ========================================================
        // CREATE COMMAND
        // ========================================================

        var command = new CreateRecurringTransactionCommand(
            Description: Description.Trim(),
            Amount: Amount,
            Currency: currency,
            Type: Type,
            Frequency: Frequency,
            StartDate: StartDate,
            EndDate: EndDate,
            CategoryId: SelectedCategory.Id,
            AccountId: SelectedAccount?.Id,
            UserId: userId,
            FamilyId: _activeFamilyId,
            FinancialPlanId: _activeFinancialPlanId,

            // NOVO: membro beneficiário.
            BeneficiaryUserId: CanSelectBeneficiary
                ? SelectedBeneficiary?.Id
                : null);

        // ========================================================
        // EXECUTE HANDLER
        // ========================================================

        var result =
            await _createRecurringTransactionHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Recorrência");

            return;
        }

        await NotificationService.ShowInformationAsync(
            "Recorrência criada com sucesso.",
            "Sucesso");

        NavigationService.GoBack();
    }

    // ============================================================
    // CREATE TRANSFER
    // ============================================================

    private async Task CreateTransferAsync(Guid userId)
    {
        if (SelectedAccount is null ||
            SelectedTargetAccount is null)
        {
            return;
        }

        var command = new CreateTransferCommand(
            Date: Date,
            Amount: Amount,
            Currency: SelectedAccount.Currency,
            Description: Description.Trim(),
            Notes: NormalizeNotes(Notes),
            UserId: userId,
            SourceAccountId: SelectedAccount.Id,
            TargetAccountId: SelectedTargetAccount.Id,
            IsConfirmed: IsConfirmed);

        var result =
            await _createTransferHandler.HandleAsync(command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Transferência");

            return;
        }

        await CompleteSaveAsync();
    }

    // ============================================================
    // UPDATE TRANSACTION
    // ============================================================

    private async Task UpdateTransactionAsync()
    {
        if (!_transactionId.HasValue ||
            SelectedAccount is null)
        {
            return;
        }

        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            await NotificationService.ShowErrorAsync(
                "Não existe um utilizador autenticado.",
                "Transação");

            return;
        }

        var command = new UpdateTransactionCommand(
            TransactionId: _transactionId.Value,
            Description: Description.Trim(),
            Notes: NormalizeNotes(Notes),
            CategoryId:
                IsTransfer
                    ? null
                    : SelectedCategory?.Id,
            Amount: Amount,
            AccountId: SelectedAccount.Id,
            UserId: userId.Value);

        var result =
            await _updateTransactionHandler.HandleAsync(command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Transação");

            return;
        }

        await CompleteSaveAsync();
    }

    // ============================================================
    // VALIDATION METHODS
    // ============================================================

    private void ValidateTransaction()
    {
        _validator.ValidateAmount(
            nameof(Amount),
            Amount);

        _validator.ValidateDescription(
            nameof(Description),
            Description);

        _validator.ValidateSourceAccount(
            nameof(SelectedAccount),
            SelectedAccount,
            !IsEditMode ? _activeFinancialPlanId : null,
            Type);

        _validator.ValidateCategory(
            nameof(SelectedCategory),
            Type,
            SelectedCategory);

        ValidateTargetAccount();

        if (IsRecurring)
        {
            // A data pontual não deve bloquear a recorrência.
            _validator.ClearDateErrors();

            ValidateRecurrence();
        }
        else
        {
            _validator.ClearRecurrenceErrors();

            _validator.ValidateDate(
                nameof(Date),
                Date,
                IsConfirmed);
        }
    }

    private void ValidateRecurrence()
    {
        if (!IsRecurring)
        {
            _validator.ClearRecurrenceErrors();
            return;
        }

        _validator.ValidateRecurrenceDates(
            nameof(EndDate),
            StartDate,
            EndDate);

        _validator.ValidateFrequency(
            nameof(Frequency),
            Frequency);
    }

    private void ValidateTargetAccount()
    {
        _validator.ValidateTargetAccount(
            nameof(SelectedTargetAccount),
            Type,
            SelectedAccount,
            SelectedTargetAccount);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private Guid? GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated ||
            !_currentUserService.UserId.HasValue ||
            _currentUserService.UserId.Value == Guid.Empty)
        {
            return null;
        }

        return _currentUserService.UserId.Value;
    }

    private static string? NormalizeNotes(string? notes)
    {
        return string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();
    }

    private async Task CompleteSaveAsync()
    {
        await NotificationService.ShowInformationAsync(
            IsEditMode
                ? "Transação atualizada com sucesso."
                : "Transação criada com sucesso.",
            "Sucesso");

        NavigationService.GoBack();
    }

    private void ExecuteCancel()
    {
        NavigationService.GoBack();
    }

    private void OnValidatorErrorsChanged(
        object? sender,
        DataErrorsChangedEventArgs e)
    {
        ErrorsChanged?.Invoke(this, e);

        OnPropertyChanged(nameof(HasErrors));

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        SaveCommand.RaiseCanExecuteChanged();
    }
}