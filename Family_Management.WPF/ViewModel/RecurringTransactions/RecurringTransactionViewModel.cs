using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.Validation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;
using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Queries.GetAvailableCategoriesForUser;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.CreateRecurringTransaction;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.DeactivateRecurringTransaction;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.GeneratePendingRecurringTransactions;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.ReactivateRecurringTransaction;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.UpdateRecurringTransaction;
using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Queries.GetRecurringTransactionsByUser;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.RecurringTransactions;

public sealed class RecurringTransactionViewModel :
    BaseViewModel,
    INotifyDataErrorInfo
{
    private readonly CurrentUserService _currentUserService;
    private readonly GetRecurringTransactionsByUserHandler
        _getRecurringTransactionsByUserHandler;
    private readonly GetAccountsByUserHandler
        _getAccountsByUserHandler;
    private readonly GetAvailableCategoriesForUserHandler
        _getAvailableCategoriesForUserHandler;
    private readonly CreateRecurringTransactionHandler
        _createRecurringTransactionHandler;
    private readonly UpdateRecurringTransactionHandler
        _updateRecurringTransactionHandler;
    private readonly DeactivateRecurringTransactionHandler
        _deactivateRecurringTransactionHandler;
    private readonly ReactivateRecurringTransactionHandler
        _reactivateRecurringTransactionHandler;
    private readonly GeneratePendingRecurringTransactionsHandler
        _generatePendingRecurringTransactionsHandler;

    private readonly RecurringTransactionValidator _validator = new();

    private ObservableCollection<RecurringTransactionDTO>
        _recurringTransactions = new();

    private ObservableCollection<AccountDTO>
        _accounts = new();

    private ObservableCollection<CategoryDTO>
        _categories = new();

    private RecurringTransactionDTO?
        _selectedRecurringTransaction;

    // CREATE
    private string _newDescription = string.Empty;
    private decimal _newAmount;
    private TransactionType _newType = TransactionType.Expense;
    private Frequency _newFrequency = Frequency.Monthly;
    private DateTime _newStartDate = DateTime.Today;
    private DateTime? _newEndDate;
    private AccountDTO? _newSelectedAccount;
    private CategoryDTO? _newSelectedCategory;

    // EDIT
    private string _editDescription = string.Empty;
    private decimal _editAmount;
    private TransactionType _editType = TransactionType.Expense;
    private Frequency _editFrequency = Frequency.Monthly;
    private DateTime _editStartDate;
    private DateTime? _editEndDate;
    private AccountDTO? _editSelectedAccount;
    private CategoryDTO? _editSelectedCategory;

    public RecurringTransactionViewModel(
        CurrentUserService currentUserService,
        GetRecurringTransactionsByUserHandler
            getRecurringTransactionsByUserHandler,
        GetAccountsByUserHandler
            getAccountsByUserHandler,
        GetAvailableCategoriesForUserHandler
            getAvailableCategoriesForUserHandler,
        CreateRecurringTransactionHandler
            createRecurringTransactionHandler,
        UpdateRecurringTransactionHandler
            updateRecurringTransactionHandler,
        DeactivateRecurringTransactionHandler
            deactivateRecurringTransactionHandler,
        ReactivateRecurringTransactionHandler
            reactivateRecurringTransactionHandler,
        GeneratePendingRecurringTransactionsHandler
            generatePendingRecurringTransactionsHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _getRecurringTransactionsByUserHandler =
            getRecurringTransactionsByUserHandler
            ?? throw new ArgumentNullException(
                nameof(getRecurringTransactionsByUserHandler));

        _getAccountsByUserHandler =
            getAccountsByUserHandler
            ?? throw new ArgumentNullException(
                nameof(getAccountsByUserHandler));

        _getAvailableCategoriesForUserHandler =
            getAvailableCategoriesForUserHandler
            ?? throw new ArgumentNullException(
                nameof(getAvailableCategoriesForUserHandler));

        _createRecurringTransactionHandler =
            createRecurringTransactionHandler
            ?? throw new ArgumentNullException(
                nameof(createRecurringTransactionHandler));

        _updateRecurringTransactionHandler =
            updateRecurringTransactionHandler
            ?? throw new ArgumentNullException(
                nameof(updateRecurringTransactionHandler));

        _deactivateRecurringTransactionHandler =
            deactivateRecurringTransactionHandler
            ?? throw new ArgumentNullException(
                nameof(deactivateRecurringTransactionHandler));

        _reactivateRecurringTransactionHandler =
            reactivateRecurringTransactionHandler
            ?? throw new ArgumentNullException(
                nameof(reactivateRecurringTransactionHandler));

        _generatePendingRecurringTransactionsHandler =
            generatePendingRecurringTransactionsHandler
            ?? throw new ArgumentNullException(
                nameof(generatePendingRecurringTransactionsHandler));

        Title = "Gestão de Transações Recorrentes";

        _validator.ErrorsChanged +=
            OnValidatorErrorsChanged;

        LoadDataCommand =
            new AsyncRelayCommand(
                LoadDataAsync,
                () => !IsBusy);

        AddRecurringTransactionCommand =
            new AsyncRelayCommand(
                AddRecurringTransactionAsync,
                CanAddRecurringTransaction);

        UpdateRecurringTransactionCommand =
            new AsyncRelayCommand(
                UpdateRecurringTransactionAsync,
                CanUpdateRecurringTransaction);

        DeactivateRecurringTransactionCommand =
            new AsyncRelayCommand(
                DeactivateRecurringTransactionAsync,
                CanDeactivateRecurringTransaction);

        ReactivateRecurringTransactionCommand =
            new AsyncRelayCommand(
                ReactivateRecurringTransactionAsync,
                CanReactivateRecurringTransaction);

        GenerateTransactionsCommand =
            new AsyncRelayCommand(
                GeneratePendingTransactionsAsync,
                () => !IsBusy &&
                      _currentUserService.IsAuthenticated);
    }

    public event EventHandler<DataErrorsChangedEventArgs>?
        ErrorsChanged;

    public bool HasErrors =>
        _validator.HasErrors;

    public IEnumerable GetErrors(
        string? propertyName)
    {
        return _validator.GetErrors(
            propertyName);
    }

    public ObservableCollection<RecurringTransactionDTO>
        RecurringTransactions
    {
        get => _recurringTransactions;

        private set =>
            SetProperty(
                ref _recurringTransactions,
                value);
    }

    public ObservableCollection<AccountDTO> Accounts
    {
        get => _accounts;

        private set =>
            SetProperty(
                ref _accounts,
                value);
    }

    public ObservableCollection<CategoryDTO> Categories
    {
        get => _categories;

        private set =>
            SetProperty(
                ref _categories,
                value);
    }

    public IReadOnlyCollection<TransactionType>
        TransactionTypes { get; } =
        new[]
        {
            TransactionType.Income,
            TransactionType.Expense
        };

    public IReadOnlyCollection<Frequency>
        Frequencies { get; } =
        Enum.GetValues<Frequency>();

    public RecurringTransactionDTO?
        SelectedRecurringTransaction
    {
        get => _selectedRecurringTransaction;

        set
        {
            if (!SetProperty(
                    ref _selectedRecurringTransaction,
                    value))
            {
                return;
            }

            LoadSelectedTransactionForEdit();

            RaiseCommandStates();
        }
    }

    // =========================================================
    // CREATE
    // =========================================================

    public string NewDescription
    {
        get => _newDescription;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(
                    ref _newDescription,
                    value))
            {
                return;
            }

            _validator.ValidateDescription(
                nameof(NewDescription),
                value);

            RaiseCommandStates();
        }
    }

    public decimal NewAmount
    {
        get => _newAmount;

        set
        {
            if (!SetProperty(
                    ref _newAmount,
                    value))
            {
                return;
            }

            _validator.ValidateAmount(
                nameof(NewAmount),
                value);

            RaiseCommandStates();
        }
    }

    public TransactionType NewType
    {
        get => _newType;

        set
        {
            if (SetProperty(
                    ref _newType,
                    value))
            {
                RaiseCommandStates();
            }
        }
    }

    public Frequency NewFrequency
    {
        get => _newFrequency;

        set
        {
            if (SetProperty(
                    ref _newFrequency,
                    value))
            {
                RaiseCommandStates();
            }
        }
    }

    public DateTime NewStartDate
    {
        get => _newStartDate;

        set
        {
            if (!SetProperty(
                    ref _newStartDate,
                    value))
            {
                return;
            }

            _validator.ValidateEndDate(
                nameof(NewEndDate),
                NewStartDate,
                NewEndDate);

            RaiseCommandStates();
        }
    }

    public DateTime? NewEndDate
    {
        get => _newEndDate;

        set
        {
            if (!SetProperty(
                    ref _newEndDate,
                    value))
            {
                return;
            }

            _validator.ValidateEndDate(
                nameof(NewEndDate),
                NewStartDate,
                value);

            RaiseCommandStates();
        }
    }

    public AccountDTO? NewSelectedAccount
    {
        get => _newSelectedAccount;

        set
        {
            if (!SetProperty(
                    ref _newSelectedAccount,
                    value))
            {
                return;
            }

            _validator.ValidateAccount(
                nameof(NewSelectedAccount),
                value);

            RaiseCommandStates();
        }
    }

    public CategoryDTO? NewSelectedCategory
    {
        get => _newSelectedCategory;

        set
        {
            if (!SetProperty(
                    ref _newSelectedCategory,
                    value))
            {
                return;
            }

            _validator.ValidateCategory(
                nameof(NewSelectedCategory),
                value);

            RaiseCommandStates();
        }
    }

    // =========================================================
    // EDIT
    // =========================================================

    public string EditDescription
    {
        get => _editDescription;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(
                    ref _editDescription,
                    value))
            {
                return;
            }

            _validator.ValidateDescription(
                nameof(EditDescription),
                value);

            RaiseCommandStates();
        }
    }

    public decimal EditAmount
    {
        get => _editAmount;

        set
        {
            if (!SetProperty(
                    ref _editAmount,
                    value))
            {
                return;
            }

            _validator.ValidateAmount(
                nameof(EditAmount),
                value);

            RaiseCommandStates();
        }
    }

    public TransactionType EditType
    {
        get => _editType;

        set
        {
            if (SetProperty(
                    ref _editType,
                    value))
            {
                RaiseCommandStates();
            }
        }
    }

    public Frequency EditFrequency
    {
        get => _editFrequency;

        set
        {
            if (SetProperty(
                    ref _editFrequency,
                    value))
            {
                RaiseCommandStates();
            }
        }
    }

    public DateTime EditStartDate
    {
        get => _editStartDate;

        private set =>
            SetProperty(
                ref _editStartDate,
                value);
    }

    public DateTime? EditEndDate
    {
        get => _editEndDate;

        set
        {
            if (!SetProperty(
                    ref _editEndDate,
                    value))
            {
                return;
            }

            _validator.ValidateEndDate(
                nameof(EditEndDate),
                EditStartDate,
                value);

            RaiseCommandStates();
        }
    }

    public AccountDTO? EditSelectedAccount
    {
        get => _editSelectedAccount;

        set
        {
            if (!SetProperty(
                    ref _editSelectedAccount,
                    value))
            {
                return;
            }

            _validator.ValidateAccount(
                nameof(EditSelectedAccount),
                value);

            RaiseCommandStates();
        }
    }

    public CategoryDTO? EditSelectedCategory
    {
        get => _editSelectedCategory;

        set
        {
            if (!SetProperty(
                    ref _editSelectedCategory,
                    value))
            {
                return;
            }

            _validator.ValidateCategory(
                nameof(EditSelectedCategory),
                value);

            RaiseCommandStates();
        }
    }

    public bool IsSelectedRecurringTransactionActive =>
        SelectedRecurringTransaction?.IsActive == true;

    public bool IsSelectedRecurringTransactionInactive =>
        SelectedRecurringTransaction is not null &&
        !SelectedRecurringTransaction.IsActive;

    // =========================================================
    // COMMANDS
    // =========================================================

    public AsyncRelayCommand LoadDataCommand { get; }

    public AsyncRelayCommand
        AddRecurringTransactionCommand { get; }

    public AsyncRelayCommand
        UpdateRecurringTransactionCommand { get; }

    public AsyncRelayCommand
        DeactivateRecurringTransactionCommand { get; }

    public AsyncRelayCommand
        ReactivateRecurringTransactionCommand { get; }

    public AsyncRelayCommand
        GenerateTransactionsCommand { get; }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        var userId =
            _currentUserService.UserId;

        if (!_currentUserService.IsAuthenticated ||
            !userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            await NotificationService.ShowWarningAsync(
                "É necessário iniciar sessão.",
                "Sessão");

            return;
        }

        await LoadDataAsync(
            cancellationToken);

        IsInitialized = true;
    }

    private Task LoadDataAsync()
    {
        return LoadDataAsync(
            CancellationToken.None);
    }

    private async Task LoadDataAsync(
        CancellationToken cancellationToken)
    {
        var currentUserId =
            _currentUserService.UserId;

        if (!currentUserId.HasValue ||
            currentUserId.Value == Guid.Empty)
        {
            return;
        }

        var userId =
            currentUserId.Value;

        await RunSafeAsync(
            async () =>
            {
                var recurringResult =
                    await _getRecurringTransactionsByUserHandler
                        .HandleAsync(
                            new GetRecurringTransactionsByUserQuery(
                                userId),
                            cancellationToken);

                if (recurringResult.IsFailure)
                {
                    await NotificationService.ShowErrorAsync(
                        recurringResult.Error.Message,
                        "Transações recorrentes");

                    return;
                }

                var accountsResult =
                    await _getAccountsByUserHandler
                        .HandleAsync(
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

                var categoriesResult =
                    await _getAvailableCategoriesForUserHandler
                        .HandleAsync(
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

                RecurringTransactions =
                    new ObservableCollection<RecurringTransactionDTO>(
                        recurringResult.Value
                            .OrderBy(item => item.NextDueDate)
                            .ThenBy(item => item.Description));

                Accounts =
                    new ObservableCollection<AccountDTO>(
                        accountsResult.Value
                            .Where(item => item.IsActive)
                            .OrderBy(item => item.Name));

                Categories =
                    new ObservableCollection<CategoryDTO>(
                        categoriesResult.Value
                            .Where(item => item.IsActive)
                            .OrderBy(item => item.Name));

                RebindSelections();

                RaiseCommandStates();
            },
            showErrorNotification: true,
            cancellationToken: cancellationToken);
    }

    // =========================================================
    // CREATE
    // =========================================================

    private async Task AddRecurringTransactionAsync()
    {
        ValidateCreateForm();

        if (!CanAddRecurringTransaction())
            return;

        var currentUserId =
            _currentUserService.UserId;

        if (!currentUserId.HasValue ||
            currentUserId.Value == Guid.Empty)
        {
            return;
        }

        var userId =
            currentUserId.Value;

        var account =
            NewSelectedAccount!;

        var category =
            NewSelectedCategory!;

        await RunSafeAsync(async () =>
        {
            var command =
                new CreateRecurringTransactionCommand(
                    Description:
                        NewDescription.Trim(),
                    Amount:
                        NewAmount,
                    Currency:
                        account.Currency,
                    Type:
                        NewType,
                    Frequency:
                        NewFrequency,
                    StartDate:
                        NewStartDate,
                    EndDate:
                        NewEndDate,
                    CategoryId:
                        category.Id,
                    AccountId:
                        account.Id,
                    UserId:
                        userId);

            var result =
                await _createRecurringTransactionHandler
                    .HandleAsync(command);

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Criar recorrência");

                return;
            }

            RecurringTransactions.Add(
                result.Value);

            SortRecurringTransactions();

            ResetCreateForm();

            await NotificationService.ShowInformationAsync(
                "Transação recorrente criada com sucesso.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private async Task UpdateRecurringTransactionAsync()
    {
        ValidateEditForm();

        if (!CanUpdateRecurringTransaction())
            return;

        var selected =
            SelectedRecurringTransaction!;

        var account =
            EditSelectedAccount!;

        var category =
            EditSelectedCategory!;

        await RunSafeAsync(async () =>
        {
            var command =
                new UpdateRecurringTransactionCommand(
                    RecurringTransactionId:
                        selected.Id,
                    Description:
                        EditDescription.Trim(),
                    Amount:
                        EditAmount,
                    Type:
                        EditType,
                    Frequency:
                        EditFrequency,
                    CategoryId:
                        category.Id,
                    AccountId:
                        account.Id,
                    EndDate:
                        EditEndDate);

            var result =
                await _updateRecurringTransactionHandler
                    .HandleAsync(command);

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Atualizar recorrência");

                return;
            }

            ReplaceRecurringTransaction(
                result.Value);

            SelectedRecurringTransaction =
                result.Value;

            await NotificationService.ShowInformationAsync(
                "Transação recorrente atualizada com sucesso.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    // =========================================================
    // DEACTIVATE
    // =========================================================

    private async Task DeactivateRecurringTransactionAsync()
    {
        var selected =
            SelectedRecurringTransaction;

        if (selected is null ||
            !selected.IsActive)
        {
            return;
        }

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Deseja desativar '{selected.Description}'?",
                "Desativar recorrência");

        if (!confirmed)
            return;

        await RunSafeAsync(async () =>
        {
            var result =
                await _deactivateRecurringTransactionHandler
                    .HandleAsync(
                        new DeactivateRecurringTransactionCommand(
                            selected.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Desativar recorrência");

                return;
            }

            await LoadDataAsync();

            SelectedRecurringTransaction =
                RecurringTransactions.FirstOrDefault(
                    item => item.Id == selected.Id);

            await NotificationService.ShowInformationAsync(
                "Transação recorrente desativada.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    // =========================================================
    // REACTIVATE
    // =========================================================

    private async Task ReactivateRecurringTransactionAsync()
    {
        var selected =
            SelectedRecurringTransaction;

        if (selected is null ||
            selected.IsActive)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _reactivateRecurringTransactionHandler
                    .HandleAsync(
                        new ReactivateRecurringTransactionCommand(
                            selected.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Reativar recorrência");

                return;
            }

            await LoadDataAsync();

            SelectedRecurringTransaction =
                RecurringTransactions.FirstOrDefault(
                    item => item.Id == selected.Id);

            await NotificationService.ShowInformationAsync(
                "Transação recorrente reativada.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    // =========================================================
    // GENERATE
    // =========================================================

    private async Task GeneratePendingTransactionsAsync()
    {
        var currentUserId =
            _currentUserService.UserId;

        if (!currentUserId.HasValue ||
            currentUserId.Value == Guid.Empty)
        {
            return;
        }

        var userId =
            currentUserId.Value;

        await RunSafeAsync(async () =>
        {
            var result =
                await _generatePendingRecurringTransactionsHandler
                    .HandleAsync(
                        new GeneratePendingRecurringTransactionsCommand(
                            userId));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Gerar movimentos");

                return;
            }

            await LoadDataAsync();

            var message =
                result.Value switch
                {
                    0 => "Não existem transações pendentes para gerar.",
                    1 => "Foi gerada 1 transação pendente.",
                    _ => $"Foram geradas {result.Value} transações pendentes."
                };

            await NotificationService.ShowInformationAsync(
                message,
                "Geração concluída");
        });

        RaiseCommandStates();
    }

    // =========================================================
    // SELECTION
    // =========================================================

    private void LoadSelectedTransactionForEdit()
    {
        _validator.ClearEditErrors();

        var selected =
            SelectedRecurringTransaction;

        if (selected is null)
        {
            ClearEditForm();
            return;
        }

        EditDescription =
            selected.Description;

        EditAmount =
            selected.Amount;

        EditType =
            selected.Type;

        EditFrequency =
            selected.Frequency;

        EditStartDate =
            selected.StartDate;

        EditEndDate =
            selected.EndDate;

        EditSelectedAccount =
            Accounts.FirstOrDefault(
                account =>
                    account.Id == selected.AccountId);

        EditSelectedCategory =
            Categories.FirstOrDefault(
                category =>
                    category.Id == selected.CategoryId);

        _validator.ClearEditErrors();

        OnPropertyChanged(
            nameof(IsSelectedRecurringTransactionActive),
            nameof(IsSelectedRecurringTransactionInactive));
    }

    private void RebindSelections()
    {
        if (SelectedRecurringTransaction is null)
            return;

        var selectedId =
            SelectedRecurringTransaction.Id;

        SelectedRecurringTransaction =
            RecurringTransactions.FirstOrDefault(
                item => item.Id == selectedId);
    }

    private void ReplaceRecurringTransaction(
        RecurringTransactionDTO updated)
    {
        var existing =
            RecurringTransactions.FirstOrDefault(
                item => item.Id == updated.Id);

        if (existing is null)
        {
            RecurringTransactions.Add(
                updated);

            SortRecurringTransactions();

            return;
        }

        var index =
            RecurringTransactions.IndexOf(
                existing);

        RecurringTransactions[index] =
            updated;

        SortRecurringTransactions();
    }

    private void SortRecurringTransactions()
    {
        var ordered =
            RecurringTransactions
                .OrderBy(item => item.NextDueDate)
                .ThenBy(item => item.Description)
                .ToList();

        RecurringTransactions.Clear();

        foreach (var recurringTransaction in ordered)
        {
            RecurringTransactions.Add(
                recurringTransaction);
        }
    }

    // =========================================================
    // RESET
    // =========================================================

    private void ResetCreateForm()
    {
        NewDescription = string.Empty;
        NewAmount = 0;
        NewType = TransactionType.Expense;
        NewFrequency = Frequency.Monthly;
        NewStartDate = DateTime.Today;
        NewEndDate = null;
        NewSelectedAccount = null;
        NewSelectedCategory = null;

        _validator.ClearCreateErrors();

        RaiseCommandStates();
    }

    private void ClearEditForm()
    {
        EditDescription = string.Empty;
        EditAmount = 0;
        EditType = TransactionType.Expense;
        EditFrequency = Frequency.Monthly;
        EditStartDate = default;
        EditEndDate = null;
        EditSelectedAccount = null;
        EditSelectedCategory = null;

        _validator.ClearEditErrors();

        OnPropertyChanged(
            nameof(IsSelectedRecurringTransactionActive),
            nameof(IsSelectedRecurringTransactionInactive));

        RaiseCommandStates();
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private void ValidateCreateForm()
    {
        _validator.ValidateDescription(
            nameof(NewDescription),
            NewDescription);

        _validator.ValidateAmount(
            nameof(NewAmount),
            NewAmount);

        _validator.ValidateEndDate(
            nameof(NewEndDate),
            NewStartDate,
            NewEndDate);

        _validator.ValidateAccount(
            nameof(NewSelectedAccount),
            NewSelectedAccount);

        _validator.ValidateCategory(
            nameof(NewSelectedCategory),
            NewSelectedCategory);
    }

    private void ValidateEditForm()
    {
        _validator.ValidateDescription(
            nameof(EditDescription),
            EditDescription);

        _validator.ValidateAmount(
            nameof(EditAmount),
            EditAmount);

        _validator.ValidateEndDate(
            nameof(EditEndDate),
            EditStartDate,
            EditEndDate);

        _validator.ValidateAccount(
            nameof(EditSelectedAccount),
            EditSelectedAccount);

        _validator.ValidateCategory(
            nameof(EditSelectedCategory),
            EditSelectedCategory);
    }

    // =========================================================
    // CAN EXECUTE
    // =========================================================

    private bool CanAddRecurringTransaction()
    {
        var userId =
            _currentUserService.UserId;

        return !IsBusy &&
               _currentUserService.IsAuthenticated &&
               userId.HasValue &&
               userId.Value != Guid.Empty &&
               !HasErrors &&
               !string.IsNullOrWhiteSpace(NewDescription) &&
               NewDescription.Trim().Length >= 2 &&
               NewAmount > 0 &&
               NewSelectedAccount is not null &&
               NewSelectedCategory is not null &&
               (!NewEndDate.HasValue ||
                NewEndDate.Value.Date >= NewStartDate.Date);
    }

    private bool CanUpdateRecurringTransaction()
    {
        var selected =
            SelectedRecurringTransaction;

        if (IsBusy ||
            selected is null ||
            !selected.IsActive ||
            string.IsNullOrWhiteSpace(EditDescription) ||
            EditDescription.Trim().Length < 2 ||
            EditAmount <= 0 ||
            EditSelectedAccount is null ||
            EditSelectedCategory is null)
        {
            return false;
        }

        if (EditEndDate.HasValue &&
            EditEndDate.Value.Date < EditStartDate.Date)
        {
            return false;
        }

        return
            !string.Equals(
                EditDescription.Trim(),
                selected.Description,
                StringComparison.Ordinal) ||
            EditAmount != selected.Amount ||
            EditType != selected.Type ||
            EditFrequency != selected.Frequency ||
            EditEndDate?.Date != selected.EndDate?.Date ||
            EditSelectedAccount.Id != selected.AccountId ||
            EditSelectedCategory.Id != selected.CategoryId;
    }

    private bool CanDeactivateRecurringTransaction()
    {
        return !IsBusy &&
               SelectedRecurringTransaction?.IsActive == true;
    }

    private bool CanReactivateRecurringTransaction()
    {
        return !IsBusy &&
               SelectedRecurringTransaction is not null &&
               !SelectedRecurringTransaction.IsActive;
    }

    private void RaiseCommandStates()
    {
        LoadDataCommand
            .RaiseCanExecuteChanged();

        AddRecurringTransactionCommand
            .RaiseCanExecuteChanged();

        UpdateRecurringTransactionCommand
            .RaiseCanExecuteChanged();

        DeactivateRecurringTransactionCommand
            .RaiseCanExecuteChanged();

        ReactivateRecurringTransactionCommand
            .RaiseCanExecuteChanged();

        GenerateTransactionsCommand
            .RaiseCanExecuteChanged();
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
}