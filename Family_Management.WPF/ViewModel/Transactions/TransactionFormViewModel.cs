using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.Validation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;
using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetsByUserAndPeriod;
using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Queries.GetAvailableCategoriesForUser;
using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Queries.GetActiveGoalsByUser;
using FamilyManagement.Application.UseCases.Transactions.Commands.CreateTransaction;
using FamilyManagement.Application.UseCases.Transactions.Commands.CreateTransfer;
using FamilyManagement.Application.UseCases.Transactions.Commands.UpdateTransaction;
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Queries.GetTransactionById;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Transactions;

public sealed class TransactionFormViewModel :
    BaseViewModel,
    INotifyDataErrorInfo,
    IParameterReceiver<Guid>
{
    private readonly CurrentUserService _currentUserService;

    private readonly CreateTransactionHandler _createTransactionHandler;
    private readonly CreateTransferHandler _createTransferHandler;
    private readonly UpdateTransactionHandler _updateTransactionHandler;

    private readonly GetTransactionByIdHandler _getTransactionByIdHandler;
    private readonly GetAccountsByUserHandler _getAccountsByUserHandler;
    private readonly GetAvailableCategoriesForUserHandler _getCategoriesHandler;
    private readonly GetBudgetsByUserAndPeriodHandler _getBudgetsHandler;
    private readonly GetActiveGoalsByUserHandler _getGoalsHandler;

    private readonly TransactionValidator _validator = new();

    private Guid? _transactionId;

    private DateTime _date = DateTime.Today;
    private decimal _amount;
    private string _description = string.Empty;
    private string? _notes;
    private TransactionType _type = TransactionType.Expense;
    private bool _isConfirmed = true;

    private AccountDTO? _selectedAccount;
    private AccountDTO? _selectedTargetAccount;
    private CategoryDTO? _selectedCategory;
    private BudgetDTO? _selectedBudget;
    private GoalDTO? _selectedGoal;

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
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
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

        _validator.ErrorsChanged +=
            OnValidatorErrorsChanged;

        Title = "Nova transação";

        SaveCommand = new AsyncRelayCommand(
            ExecuteSaveAsync,
            CanExecuteSave);

        CancelCommand = new RelayCommand(
            ExecuteCancel);
    }

    public event EventHandler<DataErrorsChangedEventArgs>?
        ErrorsChanged;

    public bool HasErrors =>
        _validator.HasErrors;

    public bool IsEditMode =>
        _transactionId.HasValue &&
        _transactionId.Value != Guid.Empty;

    public bool CanEditTransactionStructure =>
        !IsEditMode;

    public bool IsTransfer =>
        Type == TransactionType.Transfer;

    public bool UsesCategory =>
        !IsTransfer;

    public bool CanSelectBudget =>
        Type == TransactionType.Expense;

    public bool CanSelectGoal =>
        Type == TransactionType.Expense;

    public ObservableCollection<AccountDTO> Accounts { get; } =
        new();

    public ObservableCollection<CategoryDTO> Categories { get; } =
        new();

    public ObservableCollection<BudgetDTO> Budgets { get; } =
        new();

    public ObservableCollection<GoalDTO> Goals { get; } =
        new();

    public IReadOnlyCollection<TransactionType>
        TransactionTypes { get; } =
        new[]
        {
            TransactionType.Income,
            TransactionType.Expense,
            TransactionType.Transfer
        };

    public DateTime Date
    {
        get => _date;

        set
        {
            if (!SetProperty(
                    ref _date,
                    value))
            {
                return;
            }

            _validator.ValidateDate(
                nameof(Date),
                value,
                IsConfirmed);

            _ = ReloadBudgetsForDateAsync();

            RaiseCommandStates();
        }
    }

    public decimal Amount
    {
        get => _amount;

        set
        {
            if (!SetProperty(
                    ref _amount,
                    value))
            {
                return;
            }

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

            if (!SetProperty(
                    ref _description,
                    value))
            {
                return;
            }

            _validator.ValidateDescription(
                nameof(Description),
                value);

            RaiseCommandStates();
        }
    }

    public string? Notes
    {
        get => _notes;

        set => SetProperty(
            ref _notes,
            value);
    }

    public TransactionType Type
    {
        get => _type;

        set
        {
            if (!SetProperty(
                    ref _type,
                    value))
            {
                return;
            }

            if (value == TransactionType.Transfer)
            {
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

            OnPropertyChanged(
                nameof(IsTransfer),
                nameof(UsesCategory),
                nameof(CanSelectBudget),
                nameof(CanSelectGoal));

            ValidateTransaction();

            RaiseCommandStates();
        }
    }

    public bool IsConfirmed
    {
        get => _isConfirmed;

        set
        {
            if (!SetProperty(
                    ref _isConfirmed,
                    value))
            {
                return;
            }

            _validator.ValidateDate(
                nameof(Date),
                Date,
                value);

            RaiseCommandStates();
        }
    }

    public AccountDTO? SelectedAccount
    {
        get => _selectedAccount;

        set
        {
            if (!SetProperty(
                    ref _selectedAccount,
                    value))
            {
                return;
            }

            _validator.ValidateSourceAccount(
                nameof(SelectedAccount),
                value);

            ValidateTargetAccount();

            RaiseCommandStates();
        }
    }

    public AccountDTO? SelectedTargetAccount
    {
        get => _selectedTargetAccount;

        set
        {
            if (!SetProperty(
                    ref _selectedTargetAccount,
                    value))
            {
                return;
            }

            ValidateTargetAccount();

            RaiseCommandStates();
        }
    }

    public CategoryDTO? SelectedCategory
    {
        get => _selectedCategory;

        set
        {
            if (!SetProperty(
                    ref _selectedCategory,
                    value))
            {
                return;
            }

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

        set => SetProperty(
            ref _selectedBudget,
            value);
    }

    public GoalDTO? SelectedGoal
    {
        get => _selectedGoal;

        set => SetProperty(
            ref _selectedGoal,
            value);
    }

    public AsyncRelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }

    public IEnumerable GetErrors(
        string? propertyName)
    {
        return _validator.GetErrors(
            propertyName);
    }

    public void ReceiveParameter(
        Guid transactionId)
    {
        if (transactionId == Guid.Empty)
        {
            return;
        }

        _transactionId = transactionId;

        OnPropertyChanged(
            nameof(IsEditMode),
            nameof(CanEditTransactionStructure));

        Title = "Editar transação";
    }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

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

                await LoadReferenceDataAsync(
                    userId.Value,
                    cancellationToken);

                if (IsEditMode)
                {
                    await LoadTransactionAsync(
                        _transactionId!.Value);
                }

                ValidateTransaction();

                IsInitialized = true;
            },
            cancellationToken: cancellationToken);

        RaiseCommandStates();
    }

    private async Task LoadReferenceDataAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var accountsResult =
            await _getAccountsByUserHandler.HandleAsync(
                new GetAccountsByUserQuery(
                    userId),
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

        var categoriesResult =
            await _getCategoriesHandler.HandleAsync(
                new GetAvailableCategoriesForUserQuery(
                    userId),
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

        var goalsResult =
            await _getGoalsHandler.HandleAsync(
                new GetActiveGoalsByUserQuery(
                    userId),
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

        await LoadBudgetsAsync(
            userId,
            Date,
            cancellationToken);
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
            !Budgets.Any(
                budget =>
                    budget.Id == SelectedBudget.Id))
        {
            SelectedBudget = null;
        }
    }

    private async Task ReloadBudgetsForDateAsync()
    {
        if (!IsInitialized)
        {
            return;
        }

        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            return;
        }

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

    private async Task LoadTransactionAsync(
        Guid transactionId)
    {
        var result =
            await _getTransactionByIdHandler.HandleAsync(
                new GetTransactionByIdQuery(
                    transactionId));

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Transação");

            return;
        }

        ApplyTransaction(
            result.Value);
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

        _selectedAccount =
            Accounts.FirstOrDefault(
                account =>
                    account.Id ==
                    transaction.AccountId);

        _selectedTargetAccount =
            transaction.TargetAccountId.HasValue
                ? Accounts.FirstOrDefault(
                    account =>
                        account.Id ==
                        transaction.TargetAccountId.Value)
                : null;

        _selectedCategory =
            transaction.CategoryId.HasValue
                ? Categories.FirstOrDefault(
                    category =>
                        category.Id ==
                        transaction.CategoryId.Value)
                : null;

        _selectedBudget =
            transaction.BudgetId.HasValue
                ? Budgets.FirstOrDefault(
                    budget =>
                        budget.Id ==
                        transaction.BudgetId.Value)
                : null;

        _selectedGoal =
            transaction.GoalId.HasValue
                ? Goals.FirstOrDefault(
                    goal =>
                        goal.Id ==
                        transaction.GoalId.Value)
                : null;

        OnPropertyChanged(
            nameof(Date),
            nameof(Amount),
            nameof(Description),
            nameof(Notes),
            nameof(Type),
            nameof(IsConfirmed),
            nameof(SelectedAccount),
            nameof(SelectedTargetAccount),
            nameof(SelectedCategory),
            nameof(SelectedBudget),
            nameof(SelectedGoal),
            nameof(IsTransfer),
            nameof(UsesCategory),
            nameof(CanSelectBudget),
            nameof(CanSelectGoal));

        ValidateTransaction();

        RaiseCommandStates();
    }

    private bool CanExecuteSave()
    {
        return !IsBusy &&
               !HasErrors &&
               Amount > 0 &&
               SelectedAccount is not null &&
               !string.IsNullOrWhiteSpace(
                   Description) &&
               (
                   IsTransfer
                       ? SelectedTargetAccount is not null
                       : SelectedCategory is not null
               );
    }

    private async Task ExecuteSaveAsync()
    {
        ValidateTransaction();

        if (HasErrors)
        {
            return;
        }

        var userId = GetCurrentUserId();

        if (!userId.HasValue ||
            SelectedAccount is null)
        {
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

                if (IsTransfer)
                {
                    await CreateTransferAsync(
                        userId.Value);

                    return;
                }

                await CreateIncomeOrExpenseAsync(
                    userId.Value);
            });

        RaiseCommandStates();
    }

    private async Task CreateIncomeOrExpenseAsync(
        Guid userId)
    {
        if (SelectedAccount is null ||
            SelectedCategory is null)
        {
            return;
        }

        var command =
            new CreateTransactionCommand(
                Date: Date,
                Amount: Amount,
                Currency: SelectedAccount.Currency,
                Description: Description.Trim(),
                Notes: NormalizeNotes(Notes),
                Type: Type,
                AccountId: SelectedAccount.Id,
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
                IsConfirmed: IsConfirmed);

        var result =
            await _createTransactionHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Transação");

            return;
        }

        await CompleteSaveAsync();
    }

    private async Task CreateTransferAsync(
        Guid userId)
    {
        if (SelectedAccount is null ||
            SelectedTargetAccount is null)
        {
            return;
        }

        var command =
            new CreateTransferCommand(
                Date: Date,
                Amount: Amount,
                Currency: SelectedAccount.Currency,
                Description: Description.Trim(),
                Notes: NormalizeNotes(Notes),
                UserId: userId,
                SourceAccountId: SelectedAccount.Id,
                TargetAccountId:
                    SelectedTargetAccount.Id,
                IsConfirmed: IsConfirmed);

        var result =
            await _createTransferHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Transferência");

            return;
        }

        await CompleteSaveAsync();
    }

    private async Task UpdateTransactionAsync()
    {
        if (!_transactionId.HasValue ||
            SelectedAccount is null)
        {
            return;
        }

        var command =
            new UpdateTransactionCommand(
                TransactionId:
                    _transactionId.Value,
                Description:
                    Description.Trim(),
                Notes:
                    NormalizeNotes(Notes),
                CategoryId:
                    IsTransfer
                        ? null
                        : SelectedCategory?.Id,
                Amount:
                    Amount,
                AccountId:
                    SelectedAccount.Id);

        var result =
            await _updateTransactionHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Transação");

            return;
        }

        await CompleteSaveAsync();
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
            SelectedAccount);

        _validator.ValidateCategory(
            nameof(SelectedCategory),
            Type,
            SelectedCategory);

        ValidateTargetAccount();

        _validator.ValidateDate(
            nameof(Date),
            Date,
            IsConfirmed);
    }

    private void ValidateTargetAccount()
    {
        _validator.ValidateTargetAccount(
            nameof(SelectedTargetAccount),
            Type,
            SelectedAccount,
            SelectedTargetAccount);
    }

    private Guid? GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated ||
            !_currentUserService.UserId.HasValue ||
            _currentUserService.UserId.Value ==
            Guid.Empty)
        {
            return null;
        }

        return _currentUserService.UserId.Value;
    }

    private static string? NormalizeNotes(
        string? notes)
    {
        return string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();
    }

    private void OnValidatorErrorsChanged(
        object? sender,
        DataErrorsChangedEventArgs e)
    {
        ErrorsChanged?.Invoke(
            this,
            e);

        OnPropertyChanged(
            nameof(HasErrors));

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        SaveCommand.RaiseCanExecuteChanged();
    }
}