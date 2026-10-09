using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
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

public sealed class AccountViewModel : BaseViewModel
{
    private readonly CreateAccountHandler _createAccountHandler;
    private readonly UpdateAccountHandler _updateAccountHandler;
    private readonly DeactivateAccountHandler _deactivateAccountHandler;
    private readonly GetAccountsByUserHandler _getAccountsByUserHandler;
    private readonly GetCurrenciesHandler _getCurrenciesHandler;
    private readonly GetDefaultCurrencyHandler _getDefaultCurrencyHandler;
    private readonly CurrentUserService _currentUserService;

    private ObservableCollection<AccountDTO> _accounts = new();
    private ObservableCollection<CurrencyDTO> _availableCurrencies = new();

    private AccountDTO? _selectedAccount;
    private AccountFormModel _form = new();

    public AccountViewModel(
        CreateAccountHandler createAccountHandler,
        UpdateAccountHandler updateAccountHandler,
        DeactivateAccountHandler deactivateAccountHandler,
        GetAccountsByUserHandler getAccountsByUserHandler,
        GetCurrenciesHandler getCurrenciesHandler,
        GetDefaultCurrencyHandler getDefaultCurrencyHandler,
        CurrentUserService currentUserService,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _createAccountHandler = createAccountHandler
            ?? throw new ArgumentNullException(
                nameof(createAccountHandler));

        _updateAccountHandler = updateAccountHandler
            ?? throw new ArgumentNullException(
                nameof(updateAccountHandler));

        _deactivateAccountHandler = deactivateAccountHandler
            ?? throw new ArgumentNullException(
                nameof(deactivateAccountHandler));

        _getAccountsByUserHandler = getAccountsByUserHandler
            ?? throw new ArgumentNullException(
                nameof(getAccountsByUserHandler));

        _getCurrenciesHandler = getCurrenciesHandler
            ?? throw new ArgumentNullException(
                nameof(getCurrenciesHandler));

        _getDefaultCurrencyHandler = getDefaultCurrencyHandler
            ?? throw new ArgumentNullException(
                nameof(getDefaultCurrencyHandler));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        Title = "Contas";

        RefreshCommand = new AsyncRelayCommand(
            RefreshAsync,
            () => !IsBusy);

        SaveCommand = new AsyncRelayCommand(
            SaveAsync,
            CanSave);

        DeactivateAccountCommand =
            new AsyncRelayCommand(
                DeactivateSelectedAccountAsync,
                CanDeactivateAccount);

        CancelEditCommand =
            new RelayCommand(
                CancelEdit);

        SelectAccountCommand =
            new RelayCommand<AccountDTO?>(
                SelectAccount);
    }

    public ObservableCollection<AccountDTO> Accounts
    {
        get => _accounts;

        private set =>
            SetProperty(ref _accounts, value);
    }

    public ObservableCollection<CurrencyDTO> AvailableCurrencies
    {
        get => _availableCurrencies;

        private set =>
            SetProperty(ref _availableCurrencies, value);
    }

    public AccountDTO? SelectedAccount
    {
        get => _selectedAccount;

        private set
        {
            if (!SetProperty(ref _selectedAccount, value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public AccountFormModel Form
    {
        get => _form;

        private set
        {
            if (!SetProperty(ref _form, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsEditing));

            RaiseCommandStates();
        }
    }

    public bool IsEditing => Form.IsEditMode;

    public IReadOnlyList<AccountType> AccountTypes { get; } =
        Enum.GetValues<AccountType>()
            .Where(type =>
                type != AccountType.NotSpecified)
            .ToArray();

    public ICommand RefreshCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand DeactivateAccountCommand { get; }

    public ICommand CancelEditCommand { get; }

    public ICommand SelectAccountCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await RunSafeAsync(async () =>
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                await NotificationService.ShowErrorAsync(
                    "Não existe um utilizador autenticado.",
                    "Sessão");

                return;
            }

            await LoadCurrenciesAsync(
                cancellationToken);

            await LoadAccountsAsync(
                userId,
                cancellationToken);

            await ResetFormAsync(
                cancellationToken);

            IsInitialized = true;
        });
    }

    private async Task RefreshAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            await NotificationService.ShowErrorAsync(
                "Não existe um utilizador autenticado.",
                "Sessão");

            return;
        }

        await RunSafeAsync(async () =>
        {
            await LoadAccountsAsync(
                userId,
                CancellationToken.None);

            SelectedAccount = null;
        });

        RaiseCommandStates();
    }

    private async Task LoadAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result =
            await _getAccountsByUserHandler.HandleAsync(
                new GetAccountsByUserQuery(userId),
                cancellationToken);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Contas");

            return;
        }

        Accounts = new ObservableCollection<AccountDTO>(
            result.Value);
    }

    private async Task LoadCurrenciesAsync(
        CancellationToken cancellationToken)
    {
        var result =
            await _getCurrenciesHandler.HandleAsync(
                new GetCurrenciesQuery(
                    OnlyActive: true),
                cancellationToken);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Moedas");

            return;
        }

        AvailableCurrencies =
            new ObservableCollection<CurrencyDTO>(
                result.Value);
    }

    private async Task ResetFormAsync(
        CancellationToken cancellationToken)
    {
        var defaultCurrencyResult =
            await _getDefaultCurrencyHandler.HandleAsync(
                new GetDefaultCurrencyQuery(),
                cancellationToken);

        var currencyCode =
            defaultCurrencyResult.IsSuccess
                ? defaultCurrencyResult.Value.Code
                : AvailableCurrencies
                    .FirstOrDefault()
                    ?.Code
                  ?? string.Empty;

        SelectedAccount = null;

        Form = AccountFormModel.CreateNew(
            currencyCode);
    }

    private void SelectAccount(
        AccountDTO? account)
    {
        if (account is null)
        {
            return;
        }

        SelectedAccount = account;

        Form = AccountFormModel.FromAccount(
            account);
    }

    private bool CanSave()
    {
        if (IsBusy)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(Form.Name))
        {
            return false;
        }

        if (Form.Type == AccountType.NotSpecified)
        {
            return false;
        }

        if (!Form.IsEditMode &&
            string.IsNullOrWhiteSpace(Form.Currency))
        {
            return false;
        }

        return true;
    }

    private async Task SaveAsync()
    {
        if (!CanSave())
        {
            return;
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            await NotificationService.ShowErrorAsync(
                "Não existe um utilizador autenticado.",
                "Sessão");

            return;
        }

        await RunSafeAsync(async () =>
        {
            if (Form.IsEditMode)
            {
                await UpdateAccountAsync();
            }
            else
            {
                await CreateAccountAsync(
                    userId);
            }
        });

        RaiseCommandStates();
    }

    private async Task CreateAccountAsync(
        Guid userId)
    {
        var command = new CreateAccountCommand(
            Form.Name.Trim(),
            NormalizeDescription(Form.Description),
            Form.Type,
            Form.InitialBalance,
            Form.Currency.Trim().ToUpperInvariant(),
            userId,
            FamilyId: null);

        var result =
            await _createAccountHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Criar conta");

            return;
        }

        Accounts.Add(result.Value);

        await NotificationService.ShowInformationAsync(
            "Conta criada com sucesso.",
            "Sucesso");

        await ResetFormAsync(
            CancellationToken.None);
    }

    private async Task UpdateAccountAsync()
    {
        if (!Form.AccountId.HasValue)
        {
            return;
        }

        var command = new UpdateAccountCommand(
            Form.AccountId.Value,
            Form.Name.Trim(),
            NormalizeDescription(Form.Description),
            Form.Type);

        var result =
            await _updateAccountHandler.HandleAsync(
                command);

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

        await ResetFormAsync(
            CancellationToken.None);
    }

    private bool CanDeactivateAccount()
    {
        return !IsBusy &&
               SelectedAccount is not null &&
               SelectedAccount.IsActive;
    }

    private async Task DeactivateSelectedAccountAsync()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        var account = SelectedAccount;

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Deseja desativar a conta '{account.Name}'?",
                "Confirmar");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _deactivateAccountHandler.HandleAsync(
                    new DeactivateAccountCommand(
                        account.Id));

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

            await ResetFormAsync(
                CancellationToken.None);

            await NotificationService.ShowInformationAsync(
                "Conta desativada com sucesso.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    private void CancelEdit()
    {
        _ = CancelEditAsync();
    }

    private async Task CancelEditAsync()
    {
        await ResetFormAsync(
            CancellationToken.None);

        RaiseCommandStates();
    }

    private void ReplaceAccount(
        AccountDTO updatedAccount)
    {
        var existing =
            Accounts.FirstOrDefault(
                account =>
                    account.Id == updatedAccount.Id);

        if (existing is null)
        {
            Accounts.Add(updatedAccount);
            return;
        }

        var index =
            Accounts.IndexOf(existing);

        Accounts[index] =
            updatedAccount;
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
        if (RefreshCommand is AsyncRelayCommand refreshCommand)
        {
            refreshCommand.RaiseCanExecuteChanged();
        }

        if (SaveCommand is AsyncRelayCommand saveCommand)
        {
            saveCommand.RaiseCanExecuteChanged();
        }

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
}