using System.Collections.ObjectModel;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;
using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Queries.GetCurrencies;
using FamilyManagement.Application.UseCases.Investments.Commands.CreateInvestment;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Investments;

public sealed class InvestmentFormViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;
    private readonly CreateInvestmentHandler _createInvestmentHandler;
    private readonly GetAccountsByUserHandler _getAccountsByUserHandler;
    private readonly GetCurrenciesHandler _getCurrenciesHandler;

    private readonly List<AccountDTO> _allAccounts = new();

    private string _name = string.Empty;
    private decimal _initialAmount;
    private InvestmentType _type = InvestmentType.Other;
    private DateTime _purchaseDate = DateTime.Today;
    private CurrencyDTO? _selectedCurrency;
    private AccountDTO? _selectedAccount;

    public InvestmentFormViewModel(
        CurrentUserService currentUserService,
        CreateInvestmentHandler createInvestmentHandler,
        GetAccountsByUserHandler getAccountsByUserHandler,
        GetCurrenciesHandler getCurrenciesHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(navigationService, notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _createInvestmentHandler = createInvestmentHandler
            ?? throw new ArgumentNullException(nameof(createInvestmentHandler));

        _getAccountsByUserHandler = getAccountsByUserHandler
            ?? throw new ArgumentNullException(nameof(getAccountsByUserHandler));

        _getCurrenciesHandler = getCurrenciesHandler
            ?? throw new ArgumentNullException(nameof(getCurrenciesHandler));

        Title = "Novo Investimento";

        SaveCommand = new AsyncRelayCommand(
            ExecuteSaveAsync,
            CanExecuteSave);

        CancelCommand = new RelayCommand(
            _ => NavigationService.GoBack());
    }

    public ObservableCollection<CurrencyDTO> Currencies { get; } = new();

    public ObservableCollection<AccountDTO> Accounts { get; } = new();

    public Array InvestmentTypes =>
        Enum.GetValues<InvestmentType>()
            .Where(type => type != InvestmentType.NotSpecified)
            .ToArray();

    public string Name
    {
        get => _name;
        set
        {
            value ??= string.Empty;

            if (SetProperty(ref _name, value))
                SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public decimal InitialAmount
    {
        get => _initialAmount;
        set
        {
            if (SetProperty(ref _initialAmount, value))
                SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public InvestmentType Type
    {
        get => _type;
        set
        {
            if (SetProperty(ref _type, value))
                SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public DateTime PurchaseDate
    {
        get => _purchaseDate;
        set
        {
            if (SetProperty(ref _purchaseDate, value))
                SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public CurrencyDTO? SelectedCurrency
    {
        get => _selectedCurrency;
        set
        {
            if (!SetProperty(ref _selectedCurrency, value))
                return;

            FilterAccountsByCurrency();

            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public AccountDTO? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (SetProperty(ref _selectedAccount, value))
                SaveCommand.RaiseCanExecuteChanged();
        }
    }

    public AsyncRelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        await LoadDataAsync(cancellationToken);

        ResetForm();

        IsInitialized = true;
    }

    private async Task LoadDataAsync(
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var currenciesResult =
                await _getCurrenciesHandler.HandleAsync(
                    new GetCurrenciesQuery(OnlyActive: true),
                    cancellationToken);

            if (currenciesResult.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    currenciesResult.Error.Message,
                    "Moedas");

                return;
            }

            Currencies.Clear();

            foreach (var currency in currenciesResult.Value
                         .OrderBy(currency => currency.Name))
            {
                Currencies.Add(currency);
            }

            var accountsResult =
                await _getAccountsByUserHandler.HandleAsync(
                    new GetAccountsByUserQuery(userId.Value),
                    cancellationToken);

            if (accountsResult.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    accountsResult.Error.Message,
                    "Contas");

                return;
            }

            _allAccounts.Clear();

            _allAccounts.AddRange(
                accountsResult.Value
                    .Where(account => account.IsActive)
                    .OrderBy(account => account.Name));
        });
    }

    private void FilterAccountsByCurrency()
    {
        var previousAccountId =
            SelectedAccount?.Id;

        Accounts.Clear();

        if (SelectedCurrency is null)
        {
            SelectedAccount = null;
            return;
        }

        foreach (var account in _allAccounts.Where(
                     account =>
                         string.Equals(
                             account.Currency,
                             SelectedCurrency.Code,
                             StringComparison.OrdinalIgnoreCase)))
        {
            Accounts.Add(account);
        }

        SelectedAccount =
            previousAccountId.HasValue
                ? Accounts.FirstOrDefault(
                    account =>
                        account.Id == previousAccountId.Value)
                : null;
    }

    private bool CanExecuteSave()
    {
        return !IsBusy &&
               _currentUserService.IsAuthenticated &&
               !string.IsNullOrWhiteSpace(Name) &&
               Name.Trim().Length >= 3 &&
               InitialAmount > 0 &&
               Type != InvestmentType.NotSpecified &&
               PurchaseDate.Date >= new DateTime(1900, 1, 1) &&
               PurchaseDate.Date <= DateTime.Today &&
               SelectedCurrency is not null;
    }

    private async Task ExecuteSaveAsync()
    {
        var userId =
            _currentUserService.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            await NotificationService.ShowWarningAsync(
                "Não existe um utilizador autenticado.",
                "Investimento");

            return;
        }

        if (SelectedCurrency is null)
            return;

        await RunSafeAsync(async () =>
        {
            var result =
                await _createInvestmentHandler.HandleAsync(
                    new CreateInvestmentCommand(
                        Name.Trim(),
                        Type,
                        InitialAmount,
                        SelectedCurrency.Code,
                        PurchaseDate,
                        userId.Value,
                        SelectedAccount?.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível criar o investimento");

                return;
            }

            var investmentName =
                result.Value.Name;

            ResetForm();

            await NotificationService.ShowInformationAsync(
                $"O investimento '{investmentName}' foi criado com sucesso.",
                "Investimento");

            NavigationService.GoBack();
        });
    }

    private void ResetForm()
    {
        Name = string.Empty;
        InitialAmount = 0;
        Type = InvestmentType.Other;
        PurchaseDate = DateTime.Today;

        SelectedCurrency =
            Currencies.FirstOrDefault(
                currency => currency.IsDefault)
            ?? Currencies.FirstOrDefault();

        SelectedAccount = null;

        SaveCommand.RaiseCanExecuteChanged();
    }
}