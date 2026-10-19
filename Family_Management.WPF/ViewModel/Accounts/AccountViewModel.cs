using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Finance;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using Family_Management.WPF.ViewModel.Models;
using FamilyManagement.Application.UseCases.Accounts.Commands.CreateAccount;
using FamilyManagement.Application.UseCases.Accounts.Commands.DeactivateAccount;
using FamilyManagement.Application.UseCases.Accounts.Commands.UpdateAccount;
using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;
using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Queries.GetCurrencies;
using FamilyManagement.Application.UseCases.Currencies.Queries.GetDefaultCurrency;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Accounts;

public sealed class AccountViewModel : BaseViewModel, IDisposable
{
    private readonly CreateAccountHandler _createAccountHandler;
    private readonly UpdateAccountHandler _updateAccountHandler;
    private readonly DeactivateAccountHandler _deactivateAccountHandler;
    private readonly GetAccountsByUserHandler _getAccountsByUserHandler;
    private readonly GetCurrenciesHandler _getCurrenciesHandler;
    private readonly GetDefaultCurrencyHandler _getDefaultCurrencyHandler;
    private readonly CurrentUserService _currentUserService;
    private readonly FinanceContextService _financeContext;

    private readonly SemaphoreSlim _operationLock = new(1, 1);

    private ObservableCollection<AccountDTO> _accounts = new();
    private ObservableCollection<CurrencyDTO> _availableCurrencies = new();

    private AccountDTO? _selectedAccount;
    private AccountFormModel _form = new();

    private int _contextVersion;
    private bool _isDisposed;

    public AccountViewModel(
        CreateAccountHandler createAccountHandler,
        UpdateAccountHandler updateAccountHandler,
        DeactivateAccountHandler deactivateAccountHandler,
        GetAccountsByUserHandler getAccountsByUserHandler,
        GetCurrenciesHandler getCurrenciesHandler,
        GetDefaultCurrencyHandler getDefaultCurrencyHandler,
        CurrentUserService currentUserService,
        FinanceContextService financeContext,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(navigationService, notificationService)
    {
        _createAccountHandler = createAccountHandler
            ?? throw new ArgumentNullException(nameof(createAccountHandler));

        _updateAccountHandler = updateAccountHandler
            ?? throw new ArgumentNullException(nameof(updateAccountHandler));

        _deactivateAccountHandler = deactivateAccountHandler
            ?? throw new ArgumentNullException(nameof(deactivateAccountHandler));

        _getAccountsByUserHandler = getAccountsByUserHandler
            ?? throw new ArgumentNullException(nameof(getAccountsByUserHandler));

        _getCurrenciesHandler = getCurrenciesHandler
            ?? throw new ArgumentNullException(nameof(getCurrenciesHandler));

        _getDefaultCurrencyHandler = getDefaultCurrencyHandler
            ?? throw new ArgumentNullException(nameof(getDefaultCurrencyHandler));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _financeContext = financeContext
            ?? throw new ArgumentNullException(nameof(financeContext));

        Title = "Contas bancárias";

        RefreshCommand = new AsyncRelayCommand(
            RefreshAsync,
            () => !_isDisposed && !IsBusy);

        SaveCommand = new AsyncRelayCommand(
            SaveAsync,
            CanSave);

        DeactivateAccountCommand = new AsyncRelayCommand(
            DeactivateSelectedAccountAsync,
            CanDeactivateAccount);

        CancelEditCommand = new RelayCommand(CancelEdit);

        SelectAccountCommand = new RelayCommand<AccountDTO?>(
            SelectAccount);

        _form.PropertyChanged += OnFormPropertyChanged;
        _financeContext.ContextChanged += OnFinanceContextChanged;
    }

    public IReadOnlyList<string> AvailableBanks { get; } =
    [
        "ActivoBank",
        "Banco BPI",
        "Banco CTT",
        "Bankinter",
        "Caixa Geral de Depósitos",
        "Crédito Agrícola",
        "Millennium BCP",
        "Montepio",
        "Novo Banco",
        "Santander",
        "Outro banco"
    ];

    public ObservableCollection<AccountDTO> Accounts
    {
        get => _accounts;
        private set => SetProperty(ref _accounts, value);
    }

    public ObservableCollection<CurrencyDTO> AvailableCurrencies
    {
        get => _availableCurrencies;
        private set => SetProperty(ref _availableCurrencies, value);
    }

    public AccountDTO? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (!SetProperty(ref _selectedAccount, value))
                return;

            RaiseCommandStates();
        }
    }

    public AccountFormModel Form
    {
        get => _form;
        private set
        {
            if (ReferenceEquals(_form, value))
                return;

            _form.PropertyChanged -= OnFormPropertyChanged;

            SetProperty(ref _form, value);

            _form.PropertyChanged += OnFormPropertyChanged;

            OnPropertyChanged(nameof(IsEditing));
            RaiseCommandStates();
        }
    }

    public bool IsEditing => Form.IsEditMode;

    public bool IsFamilyScope =>
        _financeContext.Scope == FinanceScope.Family;

    public bool IsPersonalScope =>
        _financeContext.Scope == FinanceScope.Personal;

    public string ActiveContextDescription =>
        IsFamilyScope
            ? "Contas familiares"
            : "Contas pessoais";

    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeactivateAccountCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand SelectAccountCommand { get; }

    private void OnFormPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (_isDisposed)
            return;

        if (e.PropertyName == nameof(AccountFormModel.AccountId) ||
            e.PropertyName == nameof(AccountFormModel.IsEditMode))
        {
            OnPropertyChanged(nameof(IsEditing));
        }

        RaiseCommandStates();
    }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized || _isDisposed)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        await RunSafeAsync(async () =>
        {
            await _financeContext.RefreshAsync(cancellationToken);

            if (!TryGetCurrentUserId(out var userId))
            {
                await NotificationService.ShowErrorAsync(
                    "Não existe um utilizador autenticado.",
                    "Sessão");
                return;
            }

            await LoadCurrenciesAsync(cancellationToken);
            await LoadAccountsAsync(userId, cancellationToken);
            await ResetFormAsync(cancellationToken);

            IsInitialized = true;
        });
    }

    private async Task RefreshAsync()
    {
        if (_isDisposed)
            return;

        await RunSafeAsync(async () =>
        {
            await _financeContext.RefreshAsync();

            if (!TryGetCurrentUserId(out var userId))
            {
                Accounts = new ObservableCollection<AccountDTO>();
                SelectedAccount = null;

                await NotificationService.ShowErrorAsync(
                    "Não existe um utilizador autenticado.",
                    "Sessão");
                return;
            }

            await LoadAccountsAsync(userId, CancellationToken.None);
            await ResetFormAsync(CancellationToken.None);
        });

        RaiseCommandStates();
    }

    private async Task LoadAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var version = Volatile.Read(ref _contextVersion);
        var scope = _financeContext.Scope;
        var familyId = _financeContext.ActiveFamilyId;

        if (scope == FinanceScope.Family && !familyId.HasValue)
        {
            Accounts = new ObservableCollection<AccountDTO>();
            return;
        }

        var result = await _getAccountsByUserHandler.HandleAsync(
            new GetAccountsByUserQuery(userId, familyId),
            cancellationToken);

        if (_isDisposed ||
            version != Volatile.Read(ref _contextVersion) ||
            scope != _financeContext.Scope ||
            familyId != _financeContext.ActiveFamilyId)
        {
            return;
        }

        if (result.IsFailure)
        {
            Accounts = new ObservableCollection<AccountDTO>();

            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Contas");
            return;
        }

        Accounts = new ObservableCollection<AccountDTO>(result.Value);
    }

    private async Task LoadCurrenciesAsync(
        CancellationToken cancellationToken)
    {
        var result = await _getCurrenciesHandler.HandleAsync(
            new GetCurrenciesQuery(OnlyActive: true),
            cancellationToken);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Moedas");
            return;
        }

        AvailableCurrencies =
            new ObservableCollection<CurrencyDTO>(result.Value);
    }

    private async Task ResetFormAsync(
        CancellationToken cancellationToken)
    {
        var result = await _getDefaultCurrencyHandler.HandleAsync(
            new GetDefaultCurrencyQuery(),
            cancellationToken);

        var currencyCode = result.IsSuccess
            ? result.Value.Code
            : AvailableCurrencies.FirstOrDefault()?.Code
              ?? string.Empty;

        SelectedAccount = null;
        Form = AccountFormModel.CreateNew(currencyCode);
    }

    private void SelectAccount(AccountDTO? account)
    {
        if (account is null || IsBusy || _isDisposed)
            return;

        if (!Accounts.Any(existing => existing.Id == account.Id))
            return;

        SelectedAccount = account;
        Form = AccountFormModel.FromAccount(account);
    }

    private bool CanSave()
    {
        if (_isDisposed || IsBusy)
            return false;

        if (IsFamilyScope && Form.IsEditMode)
            return false;

        if (IsFamilyScope && !_financeContext.CanUseFamilyScope)
            return false;

        if (string.IsNullOrWhiteSpace(Form.Name) ||
            Form.Name.Trim().Length > 100)
            return false;

        if (string.IsNullOrWhiteSpace(Form.BankName) ||
            Form.BankName.Trim().Length > 120)
            return false;

        if (Form.Type == AccountType.NotSpecified)
            return false;

        if (!Form.IsEditMode &&
            string.IsNullOrWhiteSpace(Form.Currency))
            return false;

        return true;
    }

    private async Task SaveAsync()
    {
        if (!CanSave())
            return;

        if (!TryGetCurrentUserId(out var userId))
        {
            await NotificationService.ShowErrorAsync(
                "Não existe um utilizador autenticado.",
                "Sessão");
            return;
        }

        var version = Volatile.Read(ref _contextVersion);

        await RunSafeAsync(async () =>
        {
            await _operationLock.WaitAsync();

            try
            {
                if (_isDisposed ||
                    version != Volatile.Read(ref _contextVersion))
                    return;

                if (Form.IsEditMode)
                {
                    if (IsFamilyScope)
                    {
                        await NotificationService.ShowErrorAsync(
                            "A edição de contas familiares ainda não está disponível.",
                            "Operação indisponível");
                        return;
                    }

                    await UpdateAccountAsync();
                }
                else
                {
                    await CreateAccountAsync(userId);
                }
            }
            finally
            {
                _operationLock.Release();
            }
        });

        RaiseCommandStates();
    }

    private async Task CreateAccountAsync(Guid userId)
    {
        var scope = _financeContext.Scope;
        var familyId = _financeContext.ActiveFamilyId;
        var version = Volatile.Read(ref _contextVersion);

        if (scope == FinanceScope.Family && !familyId.HasValue)
        {
            await NotificationService.ShowErrorAsync(
                "Não existe uma família ativa selecionada.",
                "Contexto financeiro");
            return;
        }

        var command = new CreateAccountCommand(
            Form.Name.Trim(),
            NormalizeDescription(Form.Description),
            AccountType.Checking,
            Form.InitialBalance,
            Form.Currency.Trim().ToUpperInvariant(),
            userId,
            FamilyId: familyId,
            BankName: Form.BankName.Trim());

        var result = await _createAccountHandler.HandleAsync(command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Criar conta");
            return;
        }

        if (version == Volatile.Read(ref _contextVersion) &&
            scope == _financeContext.Scope &&
            familyId == _financeContext.ActiveFamilyId)
        {
            Accounts.Add(result.Value);
            await ResetFormAsync(CancellationToken.None);
        }

        await NotificationService.ShowInformationAsync(
            "Conta criada com sucesso.",
            "Sucesso");
    }

    private async Task UpdateAccountAsync()
    {
        if (!Form.AccountId.HasValue)
            return;

        if (IsFamilyScope)
        {
            await NotificationService.ShowErrorAsync(
                "A edição de contas familiares ainda não está disponível.",
                "Operação indisponível");
            return;
        }

        var command = new UpdateAccountCommand(
            Form.AccountId.Value,
            Form.Name.Trim(),
            NormalizeDescription(Form.Description),
            Form.Type,
            BankName: Form.BankName.Trim());

        var result = await _updateAccountHandler.HandleAsync(command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Atualizar conta");
            return;
        }

        ReplaceAccount(result.Value);

        await NotificationService.ShowInformationAsync(
            "Conta atualizada com sucesso.",
            "Sucesso");

        await ResetFormAsync(CancellationToken.None);
    }

    private bool CanDeactivateAccount()
    {
        return !_isDisposed &&
               !IsBusy &&
               !IsFamilyScope &&
               SelectedAccount is not null &&
               SelectedAccount.IsActive;
    }

    private async Task DeactivateSelectedAccountAsync()
    {
        if (!CanDeactivateAccount())
            return;

        var account = SelectedAccount!;
        var version = Volatile.Read(ref _contextVersion);

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Deseja desativar a conta '{account.Name}'?",
                "Confirmar");

        if (!confirmed)
            return;

        if (_isDisposed ||
            version != Volatile.Read(ref _contextVersion) ||
            IsFamilyScope)
            return;

        await RunSafeAsync(async () =>
        {
            await _operationLock.WaitAsync();

            try
            {
                if (_isDisposed ||
                    version != Volatile.Read(ref _contextVersion) ||
                    IsFamilyScope)
                    return;

                var result =
                    await _deactivateAccountHandler.HandleAsync(
                        new DeactivateAccountCommand(account.Id));

                if (result.IsFailure)
                {
                    await NotificationService.ShowErrorAsync(
                        result.Error.Message,
                        "Desativar conta");
                    return;
                }

                if (TryGetCurrentUserId(out var userId))
                {
                    await LoadAccountsAsync(
                        userId,
                        CancellationToken.None);
                }

                await ResetFormAsync(CancellationToken.None);

                await NotificationService.ShowInformationAsync(
                    "Conta desativada com sucesso.",
                    "Sucesso");
            }
            finally
            {
                _operationLock.Release();
            }
        });

        RaiseCommandStates();
    }

    private void CancelEdit()
    {
        _ = CancelEditAsync();
    }

    private async Task CancelEditAsync()
    {
        if (_isDisposed)
            return;

        await RunSafeAsync(async () =>
        {
            await ResetFormAsync(CancellationToken.None);
        });

        RaiseCommandStates();
    }

    private void ReplaceAccount(AccountDTO updatedAccount)
    {
        var existing = Accounts.FirstOrDefault(
            account => account.Id == updatedAccount.Id);

        if (existing is null)
        {
            Accounts.Add(updatedAccount);
            return;
        }

        var index = Accounts.IndexOf(existing);
        Accounts[index] = updatedAccount;
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = _currentUserService.UserId ?? Guid.Empty;

        return _currentUserService.IsAuthenticated &&
               userId != Guid.Empty;
    }

    private void OnFinanceContextChanged()
    {
        if (_isDisposed)
            return;

        Interlocked.Increment(ref _contextVersion);

        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.HasShutdownStarted)
            return;

        _ = dispatcher.InvokeAsync(async () =>
        {
            if (_isDisposed)
                return;

            await RefreshForFinanceContextAsync();
        });
    }

    private async Task RefreshForFinanceContextAsync()
    {
        if (_isDisposed)
            return;

        Accounts = new ObservableCollection<AccountDTO>();
        SelectedAccount = null;

        OnPropertyChanged(nameof(IsFamilyScope));
        OnPropertyChanged(nameof(IsPersonalScope));
        OnPropertyChanged(nameof(ActiveContextDescription));

        RaiseCommandStates();

        await RunSafeAsync(async () =>
        {
            await _operationLock.WaitAsync();

            try
            {
                if (_isDisposed)
                    return;

                await ResetFormAsync(CancellationToken.None);

                if (!TryGetCurrentUserId(out var userId))
                    return;

                await LoadAccountsAsync(
                    userId,
                    CancellationToken.None);
            }
            finally
            {
                _operationLock.Release();
            }
        });

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        if (RefreshCommand is AsyncRelayCommand refreshCommand)
            refreshCommand.RaiseCanExecuteChanged();

        if (SaveCommand is AsyncRelayCommand saveCommand)
            saveCommand.RaiseCanExecuteChanged();

        if (DeactivateAccountCommand
            is AsyncRelayCommand deactivateCommand)
        {
            deactivateCommand.RaiseCanExecuteChanged();
        }
    }

    private static string? NormalizeDescription(
        string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _form.PropertyChanged -= OnFormPropertyChanged;
        _financeContext.ContextChanged -= OnFinanceContextChanged;

        // O SemaphoreSlim não é libertado aqui porque
        // pode existir uma operação assíncrona em curso.
    }
}