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
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.Validation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;
using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Queries.GetAvailableCategoriesForUser;
using FamilyManagement.Application.UseCases.FinancialPlans;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.AssociateRecurringTransactionWithFamily;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.CreateRecurringTransaction;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.DeactivateRecurringTransaction;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.GeneratePendingRecurringTransactions;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.ReactivateRecurringTransaction;
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.UpdateRecurringTransaction;
using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Queries.GetRecurringTransactionsByUser;
using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Queries.GetUserById;
using FamilyManagement.Application.UseCases.Users.Queries.GetUsersByFamily;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Model;

namespace Family_Management.WPF.ViewModel.RecurringTransactions;

public sealed class RecurringTransactionViewModel :
    BaseViewModel,
    INotifyDataErrorInfo
{
    private readonly CurrentUserService _currentUserService;
    private readonly GetRecurringTransactionsByUserHandler _getRecurringTransactionsByUserHandler;
    private readonly GetAccountsByUserHandler _getAccountsByUserHandler;
    private readonly GetAvailableCategoriesForUserHandler _getAvailableCategoriesForUserHandler;
    private readonly CreateRecurringTransactionHandler _createRecurringTransactionHandler;
    private readonly UpdateRecurringTransactionHandler _updateRecurringTransactionHandler;
    private readonly DeactivateRecurringTransactionHandler _deactivateRecurringTransactionHandler;
    private readonly ReactivateRecurringTransactionHandler _reactivateRecurringTransactionHandler;
    private readonly GeneratePendingRecurringTransactionsHandler _generatePendingRecurringTransactionsHandler;
    private readonly GetUserByIdHandler _getUserByIdHandler;
    private readonly GetUsersByFamilyHandler _getUsersByFamilyHandler;
    private readonly GetActiveFamilyFinancialPlanHandler _getActiveFamilyFinancialPlanHandler;
    private readonly AssociateRecurringTransactionWithFamilyHandler _associateRecurringTransactionWithFamilyHandler;

    private readonly RecurringTransactionValidator _validator = new();

    private ObservableCollection<RecurringTransactionDTO> _recurringTransactions = new();
    private ObservableCollection<AccountDTO> _accounts = new();
    private ObservableCollection<CategoryDTO> _categories = new();
    private ObservableCollection<UserDTO> _familyMembers = new();

    private Guid? _currentFamilyId;
    private FinancialPlan? _activeFamilyFinancialPlan;
    private RecurringTransactionDTO? _selectedRecurringTransaction;
    private UserDTO? _selectedFamilyBeneficiary;
    private string _familyPlanStatus = string.Empty;
    private bool _showInactiveRecurringTransactions;

    private bool _newIsFamily;
    private string _newDescription = string.Empty;
    private decimal _newAmount;
    private TransactionType _newType = TransactionType.Expense;
    private Frequency _newFrequency = Frequency.Monthly;
    private DateTime _newStartDate = DateTime.Today;
    private DateTime? _newEndDate;
    private AccountDTO? _newSelectedAccount;
    private CategoryDTO? _newSelectedCategory;
    private UserDTO? _newSelectedBeneficiary;

    private string _editDescription = string.Empty;
    private decimal _editAmount;
    private TransactionType _editType = TransactionType.Expense;
    private Frequency _editFrequency = Frequency.Monthly;
    private DateTime _editStartDate;
    private DateTime? _editEndDate;
    private AccountDTO? _editSelectedAccount;
    private CategoryDTO? _editSelectedCategory;
    private UserDTO? _editSelectedBeneficiary;

    public RecurringTransactionViewModel(
        CurrentUserService currentUserService,
        GetRecurringTransactionsByUserHandler getRecurringTransactionsByUserHandler,
        GetAccountsByUserHandler getAccountsByUserHandler,
        GetAvailableCategoriesForUserHandler getAvailableCategoriesForUserHandler,
        CreateRecurringTransactionHandler createRecurringTransactionHandler,
        UpdateRecurringTransactionHandler updateRecurringTransactionHandler,
        DeactivateRecurringTransactionHandler deactivateRecurringTransactionHandler,
        ReactivateRecurringTransactionHandler reactivateRecurringTransactionHandler,
        GeneratePendingRecurringTransactionsHandler generatePendingRecurringTransactionsHandler,
        GetUserByIdHandler getUserByIdHandler,
        GetUsersByFamilyHandler getUsersByFamilyHandler,
        GetActiveFamilyFinancialPlanHandler getActiveFamilyFinancialPlanHandler,
        AssociateRecurringTransactionWithFamilyHandler associateRecurringTransactionWithFamilyHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(navigationService, notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _getRecurringTransactionsByUserHandler = getRecurringTransactionsByUserHandler
            ?? throw new ArgumentNullException(nameof(getRecurringTransactionsByUserHandler));

        _getAccountsByUserHandler = getAccountsByUserHandler
            ?? throw new ArgumentNullException(nameof(getAccountsByUserHandler));

        _getAvailableCategoriesForUserHandler = getAvailableCategoriesForUserHandler
            ?? throw new ArgumentNullException(nameof(getAvailableCategoriesForUserHandler));

        _createRecurringTransactionHandler = createRecurringTransactionHandler
            ?? throw new ArgumentNullException(nameof(createRecurringTransactionHandler));

        _updateRecurringTransactionHandler = updateRecurringTransactionHandler
            ?? throw new ArgumentNullException(nameof(updateRecurringTransactionHandler));

        _deactivateRecurringTransactionHandler = deactivateRecurringTransactionHandler
            ?? throw new ArgumentNullException(nameof(deactivateRecurringTransactionHandler));

        _reactivateRecurringTransactionHandler = reactivateRecurringTransactionHandler
            ?? throw new ArgumentNullException(nameof(reactivateRecurringTransactionHandler));

        _generatePendingRecurringTransactionsHandler = generatePendingRecurringTransactionsHandler
            ?? throw new ArgumentNullException(nameof(generatePendingRecurringTransactionsHandler));

        _getUserByIdHandler = getUserByIdHandler
            ?? throw new ArgumentNullException(nameof(getUserByIdHandler));

        _getUsersByFamilyHandler = getUsersByFamilyHandler
            ?? throw new ArgumentNullException(nameof(getUsersByFamilyHandler));

        _getActiveFamilyFinancialPlanHandler = getActiveFamilyFinancialPlanHandler
            ?? throw new ArgumentNullException(nameof(getActiveFamilyFinancialPlanHandler));

        _associateRecurringTransactionWithFamilyHandler = associateRecurringTransactionWithFamilyHandler
            ?? throw new ArgumentNullException(nameof(associateRecurringTransactionWithFamilyHandler));

        Title = "Movimentos recorrentes";

        _validator.ErrorsChanged += OnValidatorErrorsChanged;

        // A lambda identifica explicitamente a operação sem parâmetros.
        LoadDataCommand = new AsyncRelayCommand(
            () => LoadDataAsync(CancellationToken.None),
            () => !IsBusy);

        AddRecurringTransactionCommand = new AsyncRelayCommand(
            AddRecurringTransactionAsync,
            CanAddRecurringTransaction);

        UpdateRecurringTransactionCommand = new AsyncRelayCommand(
            UpdateRecurringTransactionAsync,
            CanUpdateRecurringTransaction);

        DeactivateRecurringTransactionCommand = new AsyncRelayCommand(
            DeactivateRecurringTransactionAsync,
            CanDeactivateRecurringTransaction);

        ReactivateRecurringTransactionCommand = new AsyncRelayCommand(
            ReactivateRecurringTransactionAsync,
            CanReactivateRecurringTransaction);

        AssociateRecurringTransactionWithFamilyCommand = new AsyncRelayCommand(
            AssociateRecurringTransactionWithFamilyAsync,
            CanAssociateRecurringTransactionWithFamily);

        GenerateTransactionsCommand = new AsyncRelayCommand(
            GeneratePendingTransactionsAsync,
            () => !IsBusy && _currentUserService.IsAuthenticated);

        OpenFamilyCommand = new AsyncRelayCommand(
            () => RunSafeAsync(
                () => NavigationService.NavigateToScreen(
                    _currentFamilyId.HasValue
                        ? AppScreen.Family
                        : AppScreen.FamilySetup)),
            () => !IsBusy);

        ClearNewAccountCommand = new RelayCommand(
            () => NewSelectedAccount = null,
            () => !IsBusy && NewIsFamily);

        ClearEditAccountCommand = new RelayCommand(
            () => EditSelectedAccount = null,
            () => !IsBusy &&
                  SelectedRecurringTransaction?.FinancialPlanId.HasValue == true);

        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IsBusy))
            {
                RaiseCommandStates();
            }
        };
    }

    // Validação

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _validator.HasErrors;

    public IEnumerable GetErrors(string? propertyName)
    {
        return _validator.GetErrors(propertyName);
    }

    // Coleções

    public ObservableCollection<RecurringTransactionDTO> RecurringTransactions
    {
        get => _recurringTransactions;
        private set => SetProperty(ref _recurringTransactions, value);
    }

    public ObservableCollection<AccountDTO> Accounts
    {
        get => _accounts;
        private set => SetProperty(ref _accounts, value);
    }

    public ObservableCollection<CategoryDTO> Categories
    {
        get => _categories;
        private set => SetProperty(ref _categories, value);
    }

    public ObservableCollection<UserDTO> FamilyMembers
    {
        get => _familyMembers;
        private set => SetProperty(ref _familyMembers, value);
    }

    public IReadOnlyCollection<TransactionType> TransactionTypes { get; } =
        new[]
        {
            TransactionType.Income,
            TransactionType.Expense
        };

    public IReadOnlyCollection<Frequency> Frequencies { get; } =
        Enum.GetValues<Frequency>();

    // Plano familiar

    public FinancialPlan? ActiveFamilyFinancialPlan
    {
        get => _activeFamilyFinancialPlan;

        private set
        {
            if (!SetProperty(ref _activeFamilyFinancialPlan, value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(HasActiveFamilyFinancialPlan),
                nameof(ActiveFamilyFinancialPlanName),
                nameof(CanChooseFamilyScope),
                nameof(ShowNewBeneficiary),
                nameof(ShowFamilyAssociation));

            RaiseCommandStates();
        }
    }

    public bool HasActiveFamilyFinancialPlan =>
        ActiveFamilyFinancialPlan is not null;

    public string ActiveFamilyFinancialPlanName =>
        ActiveFamilyFinancialPlan is null
            ? "Sem plano financeiro familiar ativo"
            : $"{ActiveFamilyFinancialPlan.Name} ({ActiveFamilyFinancialPlan.CurrencyCode})";

    public string FamilyPlanStatus
    {
        get => _familyPlanStatus;
        private set => SetProperty(ref _familyPlanStatus, value);
    }

    public bool CanChooseFamilyScope =>
        _currentFamilyId.HasValue &&
        HasActiveFamilyFinancialPlan;

    public bool ShowFamilyAssociation =>
        SelectedRecurringTransaction is
        {
            IsActive: true,
            Scope: RecurringTransactionScope.Personal,
            Type: TransactionType.Income
        } &&
        CanChooseFamilyScope;

    public UserDTO? SelectedFamilyBeneficiary
    {
        get => _selectedFamilyBeneficiary;

        set
        {
            if (SetProperty(ref _selectedFamilyBeneficiary, value))
            {
                RaiseCommandStates();
            }
        }
    }

    // Seleção e filtro

    public RecurringTransactionDTO? SelectedRecurringTransaction
    {
        get => _selectedRecurringTransaction;

        set
        {
            if (!SetProperty(ref _selectedRecurringTransaction, value))
            {
                return;
            }

            LoadSelectedTransactionForEdit();

            SelectedFamilyBeneficiary = null;

            OnPropertyChanged(
                nameof(ShowFamilyAssociation),
                nameof(ShowEditBeneficiary));

            RaiseCommandStates();
        }
    }

    public bool ShowInactiveRecurringTransactions
    {
        get => _showInactiveRecurringTransactions;

        set
        {
            if (!SetProperty(ref _showInactiveRecurringTransactions, value))
            {
                return;
            }

            SelectedRecurringTransaction = null;

            _ = LoadDataAsync();
        }
    }

    public bool IsSelectedRecurringTransactionActive =>
        SelectedRecurringTransaction?.IsActive == true;

    public bool IsSelectedRecurringTransactionInactive =>
        SelectedRecurringTransaction is not null &&
        !SelectedRecurringTransaction.IsActive;

    // Formulário de criação

    public bool NewIsFamily
    {
        get => _newIsFamily;

        set
        {
            if (!SetProperty(ref _newIsFamily, value))
            {
                return;
            }

            NewSelectedBeneficiary = null;

            _validator.ValidateAccount(
                nameof(NewSelectedAccount),
                NewSelectedAccount,
                isRequired: !NewIsFamily);

            OnPropertyChanged(nameof(ShowNewBeneficiary));

            RaiseCommandStates();
        }
    }

    public string NewDescription
    {
        get => _newDescription;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(ref _newDescription, value))
            {
                return;
            }

            _validator.ValidateDescription(nameof(NewDescription), value);

            RaiseCommandStates();
        }
    }

    public decimal NewAmount
    {
        get => _newAmount;

        set
        {
            if (!SetProperty(ref _newAmount, value))
            {
                return;
            }

            _validator.ValidateAmount(nameof(NewAmount), value);

            RaiseCommandStates();
        }
    }

    public TransactionType NewType
    {
        get => _newType;

        set
        {
            if (!SetProperty(ref _newType, value))
            {
                return;
            }

            if (value != TransactionType.Income)
            {
                NewSelectedBeneficiary = null;
            }

            OnPropertyChanged(nameof(ShowNewBeneficiary));

            RaiseCommandStates();
        }
    }

    public Frequency NewFrequency
    {
        get => _newFrequency;

        set
        {
            if (SetProperty(ref _newFrequency, value))
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
            if (!SetProperty(ref _newStartDate, value))
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
            if (!SetProperty(ref _newEndDate, value))
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
            if (!SetProperty(ref _newSelectedAccount, value))
            {
                return;
            }

            _validator.ValidateAccount(
                nameof(NewSelectedAccount),
                value,
                isRequired: !NewIsFamily);

            RaiseCommandStates();
        }
    }

    public CategoryDTO? NewSelectedCategory
    {
        get => _newSelectedCategory;

        set
        {
            if (!SetProperty(ref _newSelectedCategory, value))
            {
                return;
            }

            _validator.ValidateCategory(
                nameof(NewSelectedCategory),
                value);

            RaiseCommandStates();
        }
    }

    public bool ShowNewBeneficiary =>
        NewIsFamily &&
        NewType == TransactionType.Income &&
        CanChooseFamilyScope &&
        FamilyMembers.Count > 0;

    public UserDTO? NewSelectedBeneficiary
    {
        get => _newSelectedBeneficiary;

        set
        {
            if (SetProperty(ref _newSelectedBeneficiary, value))
            {
                RaiseCommandStates();
            }
        }
    }

    // Formulário de edição

    public string EditDescription
    {
        get => _editDescription;

        set
        {
            value ??= string.Empty;

            if (!SetProperty(ref _editDescription, value))
            {
                return;
            }

            _validator.ValidateDescription(nameof(EditDescription), value);

            RaiseCommandStates();
        }
    }

    public decimal EditAmount
    {
        get => _editAmount;

        set
        {
            if (!SetProperty(ref _editAmount, value))
            {
                return;
            }

            _validator.ValidateAmount(nameof(EditAmount), value);

            RaiseCommandStates();
        }
    }

    public TransactionType EditType
    {
        get => _editType;

        set
        {
            if (!SetProperty(ref _editType, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ShowEditBeneficiary));

            RaiseCommandStates();
        }
    }

    public Frequency EditFrequency
    {
        get => _editFrequency;

        set
        {
            if (SetProperty(ref _editFrequency, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public DateTime EditStartDate
    {
        get => _editStartDate;

        set
        {
            if (!SetProperty(ref _editStartDate, value))
                return;

            _validator.ValidateEndDate(
                nameof(EditEndDate),
                EditStartDate,
                EditEndDate);

            RaiseCommandStates();
        }
    }

    public string EditScheduleDateLabel =>
        SelectedRecurringTransaction?.LastGenerateDate.HasValue == true
            ? "Próxima ocorrência"
            : "Data de início";

    public string EditScheduleDateHelp =>
        SelectedRecurringTransaction?.LastGenerateDate.HasValue == true
            ? "A nova data substitui a próxima ocorrência ainda não gerada "
            + "e passa a orientar as seguintes. Os movimentos já gerados mantêm-se."
            : "Corrige a data de início. A primeira ocorrência passa a usar esta data.";

    public DateTime? EditEndDate
    {
        get => _editEndDate;

        set
        {
            if (!SetProperty(ref _editEndDate, value))
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
            if (!SetProperty(ref _editSelectedAccount, value))
            {
                return;
            }

            _validator.ValidateAccount(
                nameof(EditSelectedAccount),
                value,
                isRequired:
                    SelectedRecurringTransaction?.FinancialPlanId is null);

            RaiseCommandStates();
        }
    }

    public CategoryDTO? EditSelectedCategory
    {
        get => _editSelectedCategory;

        set
        {
            if (!SetProperty(ref _editSelectedCategory, value))
            {
                return;
            }

            _validator.ValidateCategory(
                nameof(EditSelectedCategory),
                value);

            RaiseCommandStates();
        }
    }

    public bool ShowEditBeneficiary =>
        EditType == TransactionType.Income &&
        SelectedRecurringTransaction?.Scope == RecurringTransactionScope.Family &&
        FamilyMembers.Count > 0;

    public UserDTO? EditSelectedBeneficiary
    {
        get => _editSelectedBeneficiary;

        set
        {
            if (SetProperty(ref _editSelectedBeneficiary, value))
            {
                RaiseCommandStates();
            }
        }
    }

    // Comandos

    public AsyncRelayCommand LoadDataCommand { get; }
    public AsyncRelayCommand AddRecurringTransactionCommand { get; }
    public AsyncRelayCommand UpdateRecurringTransactionCommand { get; }
    public AsyncRelayCommand DeactivateRecurringTransactionCommand { get; }
    public AsyncRelayCommand ReactivateRecurringTransactionCommand { get; }
    public AsyncRelayCommand AssociateRecurringTransactionWithFamilyCommand { get; }
    public AsyncRelayCommand GenerateTransactionsCommand { get; }
    public AsyncRelayCommand OpenFamilyCommand { get; }
    public RelayCommand ClearNewAccountCommand { get; }
    public RelayCommand ClearEditAccountCommand { get; }

    // Inicialização

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        var userId = _currentUserService.UserId;

        if (!_currentUserService.IsAuthenticated ||
            !userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            await NotificationService.ShowWarningAsync(
                "É necessário iniciar sessão.",
                "Sessão");

            return;
        }

        await LoadDataAsync(cancellationToken);

        IsInitialized = true;
    }

    // Carregamento

    private Task LoadDataAsync()
    {
        return LoadDataAsync(CancellationToken.None);
    }

    private Task LoadDataAsync(CancellationToken cancellationToken)
    {
        return RunSafeAsync(
            () => LoadDataCoreAsync(cancellationToken),
            cancellationToken: cancellationToken);
    }

    private async Task LoadDataCoreAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (!_currentUserService.IsAuthenticated ||
            !userId.HasValue)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var selectedId = SelectedRecurringTransaction?.Id;
        var newAccountId = NewSelectedAccount?.Id;
        var newCategoryId = NewSelectedCategory?.Id;
        var newBeneficiaryId = NewSelectedBeneficiary?.Id;

        var userResult = await _getUserByIdHandler.HandleAsync(
            new GetUserByIdQuery(userId.Value),
            cancellationToken);

        if (userResult.IsFailure)
        {
            throw new InvalidOperationException(userResult.Error.Message);
        }

        _currentFamilyId = userResult.Value.FamilyId;

        ActiveFamilyFinancialPlan = null;
        FamilyMembers = new ObservableCollection<UserDTO>();

        FamilyPlanStatus =
            "Configura a família para utilizares um plano familiar.";

        if (_currentFamilyId.HasValue)
        {
            var membersResult = await _getUsersByFamilyHandler.HandleAsync(
                new GetUsersByFamilyQuery(_currentFamilyId.Value));

            if (membersResult.IsFailure)
            {
                throw new InvalidOperationException(membersResult.Error.Message);
            }

            FamilyMembers = new ObservableCollection<UserDTO>(
                membersResult.Value
                    .Where(member => member.IsActive)
                    .OrderBy(member => member.UserName));

            var planResult = await _getActiveFamilyFinancialPlanHandler.HandleAsync(
                _currentFamilyId.Value,
                userId.Value);

            if (planResult.IsSuccess)
            {
                ActiveFamilyFinancialPlan = planResult.Value;

                FamilyPlanStatus =
                    "Plano disponível. A conta bancária é opcional " +
                    "nas recorrências familiares.";
            }
            else if (planResult.Error.Code == "FinancialPlan.NotConfigured")
            {
                FamilyPlanStatus =
                    "A família ainda não tem plano. Abre a gestão " +
                    "da família para o criador configurar o plano.";
            }
            else
            {
                FamilyPlanStatus = planResult.Error.Message;

                throw new InvalidOperationException(planResult.Error.Message);
            }
        }

        var accountsResult = await _getAccountsByUserHandler.HandleAsync(
            new GetAccountsByUserQuery(userId.Value),
            cancellationToken);

        if (accountsResult.IsFailure)
        {
            throw new InvalidOperationException(accountsResult.Error.Message);
        }

        var categoriesResult =
            await _getAvailableCategoriesForUserHandler.HandleAsync(
                new GetAvailableCategoriesForUserQuery(userId.Value),
                cancellationToken);

        if (categoriesResult.IsFailure)
        {
            throw new InvalidOperationException(categoriesResult.Error.Message);
        }

        var recurringResult =
            await _getRecurringTransactionsByUserHandler.HandleAsync(
                new GetRecurringTransactionsByUserQuery(userId.Value),
                cancellationToken);

        if (recurringResult.IsFailure)
        {
            throw new InvalidOperationException(recurringResult.Error.Message);
        }

        Accounts = new ObservableCollection<AccountDTO>(
            accountsResult.Value
                .Where(account => account.IsActive)
                .OrderBy(account => account.Name));

        Categories = new ObservableCollection<CategoryDTO>(
            categoriesResult.Value
                .Where(category => category.IsActive)
                .OrderBy(category => category.Name));

        RecurringTransactions = new ObservableCollection<RecurringTransactionDTO>(
            recurringResult.Value
                .Where(item =>
                    ShowInactiveRecurringTransactions || item.IsActive)
                .OrderBy(item => item.NextDueDate)
                .ThenBy(item => item.Description));

        if (!CanChooseFamilyScope)
        {
            NewIsFamily = false;
        }

        NewSelectedAccount = Accounts.FirstOrDefault(
            account => account.Id == newAccountId);

        NewSelectedCategory = Categories.FirstOrDefault(
            category => category.Id == newCategoryId);

        NewSelectedBeneficiary = NewIsFamily
            ? FamilyMembers.FirstOrDefault(
                member => member.Id == newBeneficiaryId)
            : null;

        SelectedRecurringTransaction = RecurringTransactions.FirstOrDefault(
            item => item.Id == selectedId);

        OnPropertyChanged(
            nameof(CanChooseFamilyScope),
            nameof(ShowNewBeneficiary),
            nameof(ShowEditBeneficiary),
            nameof(ShowFamilyAssociation));

        RaiseCommandStates();
    }

    // Criar recorrência

    private async Task AddRecurringTransactionAsync()
    {
        ValidateCreateForm();

        if (!CanAddRecurringTransaction())
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var command = new CreateRecurringTransactionCommand(
                Description: NewDescription.Trim(),
                Amount: NewAmount,
                Currency: NewIsFamily
                    ? ActiveFamilyFinancialPlan!.CurrencyCode
                    : NewSelectedAccount!.Currency,
                Type: NewType,
                Frequency: NewFrequency,
                StartDate: NewStartDate,
                EndDate: NewEndDate,
                CategoryId: NewSelectedCategory!.Id,
                AccountId: NewSelectedAccount?.Id,
                UserId: _currentUserService.UserId!.Value,
                FamilyId: NewIsFamily ? _currentFamilyId : null,
                FinancialPlanId: NewIsFamily
                    ? ActiveFamilyFinancialPlan!.Id
                    : null,
                BeneficiaryUserId:
                    NewIsFamily && NewType == TransactionType.Income
                        ? NewSelectedBeneficiary!.Id
                        : null,
                Scope: NewIsFamily
                    ? RecurringTransactionScope.Family
                    : RecurringTransactionScope.Personal);

            var result = await _createRecurringTransactionHandler.HandleAsync(command);

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Criar recorrência");

                return;
            }

            ResetCreateForm();

            await LoadDataCoreAsync();

            SelectedRecurringTransaction = RecurringTransactions.FirstOrDefault(
                item => item.Id == result.Value.Id);

            await NotificationService.ShowInformationAsync(
                "Recorrência criada com sucesso.",
                "Recorrências");
        });
    }

    // Editar recorrência

    private async Task UpdateRecurringTransactionAsync()
    {
        ValidateEditForm();

        if (!CanUpdateRecurringTransaction())
        {
            return;
        }

        var selectedId = SelectedRecurringTransaction!.Id;

        await RunSafeAsync(async () =>
        {
            var command = new UpdateRecurringTransactionCommand(
                RecurringTransactionId: selectedId,
                Description: EditDescription.Trim(),
                Amount: EditAmount,
                Type: EditType,
                Frequency: EditFrequency,
                CategoryId: EditSelectedCategory!.Id,
                AccountId: EditSelectedAccount?.Id,
                EndDate: EditEndDate,
                BeneficiaryUserId: ShowEditBeneficiary
                    ? EditSelectedBeneficiary?.Id
                    : null,
                ScheduleDate: EditStartDate);

            var result = await _updateRecurringTransactionHandler.HandleAsync(command);

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Atualizar recorrência");

                return;
            }

            await LoadDataCoreAsync();

            SelectedRecurringTransaction = RecurringTransactions.FirstOrDefault(
                item => item.Id == selectedId);

            await NotificationService.ShowInformationAsync(
                "Recorrência atualizada com sucesso.",
                "Recorrências");
        });
    }

    // Associar receita pessoal ao plano familiar

    private bool CanAssociateRecurringTransactionWithFamily()
    {
        var userId = _currentUserService.UserId;

        return !IsBusy &&
               _currentUserService.IsAuthenticated &&
               userId.HasValue &&
               userId.Value != Guid.Empty &&
               ShowFamilyAssociation &&
               ActiveFamilyFinancialPlan is not null &&
               SelectedFamilyBeneficiary is not null &&
               FamilyMembers.Any(
                   member => member.Id == SelectedFamilyBeneficiary.Id);
    }

    private async Task AssociateRecurringTransactionWithFamilyAsync()
    {
        if (!CanAssociateRecurringTransactionWithFamily())
        {
            return;
        }

        var selected = SelectedRecurringTransaction;
        var userId = _currentUserService.UserId;
        var familyId = _currentFamilyId;
        var plan = ActiveFamilyFinancialPlan;
        var beneficiary = SelectedFamilyBeneficiary;

        if (selected is null ||
            !userId.HasValue ||
            !familyId.HasValue ||
            plan is null ||
            beneficiary is null)
        {
            return;
        }

        var confirmed = await NotificationService.ShowConfirmationAsync(
            $"Pretendes associar '{selected.Description}' ao plano familiar?\n\n" +
            $"Plano: {plan.Name}\n" +
            $"Beneficiário: {beneficiary.UserName}\n\n" +
            "A alteração aplica-se às próximas ocorrências. " +
            "Os movimentos já gerados e a titularidade da conta permanecem iguais.",
            "Associar receita ao plano familiar");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var command = new AssociateRecurringTransactionWithFamilyCommand(
                RecurringTransactionId: selected.Id,
                FamilyId: familyId.Value,
                BeneficiaryUserId: beneficiary.Id,
                RequestedByUserId: userId.Value,
                FinancialPlanId: plan.Id);

            var result = await _associateRecurringTransactionWithFamilyHandler
                .HandleAsync(command);

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Associar receita familiar");

                return;
            }

            await LoadDataCoreAsync();

            SelectedRecurringTransaction = RecurringTransactions.FirstOrDefault(
                item => item.Id == selected.Id);

            SelectedFamilyBeneficiary = null;

            await NotificationService.ShowInformationAsync(
                "A receita recorrente foi associada ao plano familiar.",
                "Associação concluída");
        });
    }

    // Desativar e reativar

    private async Task DeactivateRecurringTransactionAsync()
    {
        var selected = SelectedRecurringTransaction;

        if (selected is null || !selected.IsActive)
        {
            return;
        }

        var confirmed = await NotificationService.ShowConfirmationAsync(
            $"Deseja desativar '{selected.Description}'?",
            "Desativar recorrência");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result = await _deactivateRecurringTransactionHandler.HandleAsync(
                new DeactivateRecurringTransactionCommand(selected.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Desativar recorrência");

                return;
            }

            SelectedRecurringTransaction = null;

            var existing = RecurringTransactions.FirstOrDefault(
                item => item.Id == selected.Id);

            if (existing is not null)
            {
                RecurringTransactions.Remove(existing);
            }

            await NotificationService.ShowInformationAsync(
                "Recorrência desativada com sucesso.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    private async Task ReactivateRecurringTransactionAsync()
    {
        var selected = SelectedRecurringTransaction;

        if (selected is null || selected.IsActive)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result = await _reactivateRecurringTransactionHandler.HandleAsync(
                new ReactivateRecurringTransactionCommand(selected.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Reativar recorrência");

                return;
            }

            SelectedRecurringTransaction = null;

            var existing = RecurringTransactions.FirstOrDefault(
                item => item.Id == selected.Id);

            if (existing is not null)
            {
                RecurringTransactions.Remove(existing);
            }

            await NotificationService.ShowInformationAsync(
                "Recorrência reativada com sucesso.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    // Gerar movimentos

    private async Task GeneratePendingTransactionsAsync()
    {
        var userId = _currentUserService.UserId;

        if (!_currentUserService.IsAuthenticated ||
            !userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result = await _generatePendingRecurringTransactionsHandler.HandleAsync(
                new GeneratePendingRecurringTransactionsCommand(userId.Value));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Gerar movimentos");

                return;
            }

            await LoadDataCoreAsync();

            var message = result.Value switch
            {
                0 => "Não existem transações pendentes para gerar.",
                1 => "Foi gerada 1 transação pendente.",
                _ => $"Foram geradas {result.Value} transações pendentes."
            };

            await NotificationService.ShowInformationAsync(
                message,
                "Geração concluída");
        });
    }

    // Preenchimento dos formulários

    private void LoadSelectedTransactionForEdit()
    {
        _validator.ClearEditErrors();

        var selected = SelectedRecurringTransaction;

        if (selected is null)
        {
            ClearEditForm();
            return;
        }

        EditDescription = selected.Description;
        EditAmount = selected.Amount;
        EditType = selected.Type;
        EditFrequency = selected.Frequency;
        
        var scheduleDate = selected.LastGenerateDate.HasValue
        ? selected.NextDueDate
        : selected.StartDate;

        EditStartDate = scheduleDate.Kind == DateTimeKind.Utc
            ? scheduleDate.ToLocalTime().Date
            : scheduleDate.Date;

        EditEndDate = selected.EndDate.HasValue
            ? selected.EndDate.Value.Kind == DateTimeKind.Utc
                ? selected.EndDate.Value.ToLocalTime().Date
                : selected.EndDate.Value.Date
            : null;        

        EditSelectedAccount = Accounts.FirstOrDefault(
            account => account.Id == selected.AccountId);

        EditSelectedCategory = Categories.FirstOrDefault(
            category => category.Id == selected.CategoryId);

        EditSelectedBeneficiary = FamilyMembers.FirstOrDefault(
            member => member.Id == selected.BeneficiaryUserId);

        SelectedFamilyBeneficiary = null;

        OnPropertyChanged(
            nameof(ShowEditBeneficiary),
            nameof(ShowFamilyAssociation),
            nameof(IsSelectedRecurringTransactionActive),
            nameof(IsSelectedRecurringTransactionInactive),
            nameof(EditScheduleDateLabel),
            nameof(EditScheduleDateHelp));

        _validator.ClearEditErrors();
    }

    private void RebindSelections()
    {
        if (SelectedRecurringTransaction is null)
        {
            return;
        }

        var selectedId = SelectedRecurringTransaction.Id;

        SelectedRecurringTransaction = RecurringTransactions.FirstOrDefault(
            item => item.Id == selectedId);
    }

    private void ReplaceRecurringTransaction(RecurringTransactionDTO updated)
    {
        var existing = RecurringTransactions.FirstOrDefault(
            item => item.Id == updated.Id);

        if (existing is null)
        {
            RecurringTransactions.Add(updated);
            SortRecurringTransactions();
            return;
        }

        var index = RecurringTransactions.IndexOf(existing);

        RecurringTransactions[index] = updated;

        SortRecurringTransactions();
    }

    private void SortRecurringTransactions()
    {
        var ordered = RecurringTransactions
            .OrderBy(item => item.NextDueDate)
            .ThenBy(item => item.Description)
            .ToList();

        RecurringTransactions.Clear();

        foreach (var item in ordered)
        {
            RecurringTransactions.Add(item);
        }
    }

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
        NewSelectedBeneficiary = null;
        NewIsFamily = false;

        _validator.ClearCreateErrors();

        OnPropertyChanged(nameof(ShowNewBeneficiary));

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
        EditSelectedBeneficiary = null;
        SelectedFamilyBeneficiary = null;

        _validator.ClearEditErrors();

        OnPropertyChanged(
            nameof(ShowEditBeneficiary),
            nameof(ShowFamilyAssociation),
            nameof(IsSelectedRecurringTransactionActive),
            nameof(IsSelectedRecurringTransactionInactive));

        RaiseCommandStates();
    }

    // Validação dos formulários

    private void ValidateCreateForm()
    {
        _validator.ValidateDescription(nameof(NewDescription), NewDescription);
        _validator.ValidateAmount(nameof(NewAmount), NewAmount);

        _validator.ValidateEndDate(
            nameof(NewEndDate),
            NewStartDate,
            NewEndDate);

        _validator.ValidateAccount(
            nameof(NewSelectedAccount),
            NewSelectedAccount,
            isRequired: !NewIsFamily);

        _validator.ValidateCategory(
            nameof(NewSelectedCategory),
            NewSelectedCategory);
    }

    private void ValidateEditForm()
    {
        _validator.ValidateDescription(nameof(EditDescription), EditDescription);
        _validator.ValidateAmount(nameof(EditAmount), EditAmount);

        _validator.ValidateEndDate(
            nameof(EditEndDate),
            EditStartDate,
            EditEndDate);

        _validator.ValidateAccount(
            nameof(EditSelectedAccount),
            EditSelectedAccount,
            isRequired:
                SelectedRecurringTransaction?.FinancialPlanId is null);

        _validator.ValidateCategory(
            nameof(EditSelectedCategory),
            EditSelectedCategory);
    }

    // Disponibilidade dos comandos

    private bool CanAddRecurringTransaction()
    {
        if (IsBusy ||
            !_currentUserService.IsAuthenticated ||
            !_currentUserService.UserId.HasValue ||
            string.IsNullOrWhiteSpace(NewDescription) ||
            NewDescription.Trim().Length < 2 ||
            NewAmount <= 0 ||
            NewSelectedCategory is null ||
            NewStartDate == default ||
            (NewEndDate.HasValue &&
             NewEndDate.Value.Date < NewStartDate.Date))
        {
            return false;
        }

        if (!NewIsFamily)
        {
            return NewSelectedAccount is not null &&
                   !NewSelectedAccount.FamilyId.HasValue;
        }

        if (!CanChooseFamilyScope)
        {
            return false;
        }

        if (NewSelectedAccount is not null)
        {
            var belongsToAnotherFamily =
                NewSelectedAccount.FamilyId.HasValue &&
                NewSelectedAccount.FamilyId != _currentFamilyId;

            var currencyMatches = string.Equals(
                NewSelectedAccount.Currency,
                ActiveFamilyFinancialPlan!.CurrencyCode,
                StringComparison.OrdinalIgnoreCase);

            if (belongsToAnotherFamily || !currencyMatches)
            {
                return false;
            }
        }

        return NewType != TransactionType.Income ||
               (NewSelectedBeneficiary is not null &&
                FamilyMembers.Any(
                    member => member.Id == NewSelectedBeneficiary.Id));
    }

    private bool CanUpdateRecurringTransaction()
    {
        var selected = SelectedRecurringTransaction;

        if (IsBusy ||
            selected is null ||
            !selected.IsActive ||
            string.IsNullOrWhiteSpace(EditDescription) ||
            EditDescription.Trim().Length < 2 ||
            EditAmount <= 0 ||
            EditStartDate == default ||
            EditSelectedCategory is null ||
            (EditSelectedAccount is null &&
            !selected.FinancialPlanId.HasValue) ||
            (EditEndDate.HasValue &&
            EditEndDate.Value.Date < EditStartDate.Date))
        {
            return false;
        }

        if (selected.Scope == RecurringTransactionScope.Family &&
            EditType == TransactionType.Income &&
            (EditSelectedBeneficiary is null ||
            !FamilyMembers.Any(
                member => member.Id == EditSelectedBeneficiary.Id)))
        {
            return false;
        }

        // Sem histórico, editamos a data de início.
        // Com histórico, editamos a próxima ocorrência.
        var originalScheduleDate = selected.LastGenerateDate.HasValue
            ? selected.NextDueDate
            : selected.StartDate;

        var originalScheduleLocalDate =
            originalScheduleDate.Kind == DateTimeKind.Utc
                ? originalScheduleDate.ToLocalTime().Date
                : originalScheduleDate.Date;

        DateTime? originalEndLocalDate = selected.EndDate.HasValue
            ? selected.EndDate.Value.Kind == DateTimeKind.Utc
                ? selected.EndDate.Value.ToLocalTime().Date
                : selected.EndDate.Value.Date
            : null;

        return
            EditStartDate.Date != originalScheduleLocalDate ||
            EditDescription.Trim() != selected.Description ||
            EditAmount != selected.Amount ||
            EditType != selected.Type ||
            EditFrequency != selected.Frequency ||
            EditEndDate?.Date != originalEndLocalDate ||
            EditSelectedAccount?.Id != selected.AccountId ||
            EditSelectedCategory.Id != selected.CategoryId ||
            (ShowEditBeneficiary &&
            EditSelectedBeneficiary?.Id != selected.BeneficiaryUserId);
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
        OpenFamilyCommand?.RaiseCanExecuteChanged();
        ClearNewAccountCommand?.RaiseCanExecuteChanged();
        ClearEditAccountCommand?.RaiseCanExecuteChanged();

        LoadDataCommand.RaiseCanExecuteChanged();
        AddRecurringTransactionCommand.RaiseCanExecuteChanged();
        UpdateRecurringTransactionCommand.RaiseCanExecuteChanged();
        DeactivateRecurringTransactionCommand.RaiseCanExecuteChanged();
        ReactivateRecurringTransactionCommand.RaiseCanExecuteChanged();

        AssociateRecurringTransactionWithFamilyCommand
            .RaiseCanExecuteChanged();

        GenerateTransactionsCommand.RaiseCanExecuteChanged();
    }

    private void OnValidatorErrorsChanged(
        object? sender,
        DataErrorsChangedEventArgs e)
    {
        ErrorsChanged?.Invoke(this, e);

        OnPropertyChanged(nameof(HasErrors));

        RaiseCommandStates();
    }
}