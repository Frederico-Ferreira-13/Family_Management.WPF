using System.Collections.ObjectModel;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Queries.GetAccountsByUser;
using FamilyManagement.Application.UseCases.Investments.Commands.DeactivateInvestment;
using FamilyManagement.Application.UseCases.Investments.Commands.UpdateInvestment;
using FamilyManagement.Application.UseCases.Investments.Commands.UpdateInvestmentCurrentValue;
using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Queries.GetActiveInvestmentsByUser;
using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Investments;

public sealed class InvestmentViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;
    private readonly GetActiveInvestmentsByUserHandler _getInvestmentsHandler;
    private readonly GetAccountsByUserHandler _getAccountsByUserHandler;
    private readonly UpdateInvestmentHandler _updateInvestmentHandler;
    private readonly DeactivateInvestmentHandler _deactivateInvestmentHandler;
    private readonly UpdateInvestmentCurrentValueHandler _updateCurrentValueHandler;

    // Mantemos a lista completa das contas separada da coleção
    // apresentada no formulário de edição.
    private readonly List<AccountDTO> _allEditAccounts = new();

    private InvestmentDTO? _selectedInvestment;
    private AccountDTO? _editSelectedAccount;

    private string _editInvestmentName = string.Empty;
    private InvestmentType _editInvestmentType =
        InvestmentType.NotSpecified;

    private decimal _updateValueAmount;

    public InvestmentViewModel(
        CurrentUserService currentUserService,
        GetActiveInvestmentsByUserHandler getInvestmentsHandler,
        GetAccountsByUserHandler getAccountsByUserHandler,
        UpdateInvestmentHandler updateInvestmentHandler,
        DeactivateInvestmentHandler deactivateInvestmentHandler,
        UpdateInvestmentCurrentValueHandler updateCurrentValueHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(navigationService, notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _getInvestmentsHandler = getInvestmentsHandler
            ?? throw new ArgumentNullException(nameof(getInvestmentsHandler));

        _getAccountsByUserHandler = getAccountsByUserHandler
            ?? throw new ArgumentNullException(nameof(getAccountsByUserHandler));

        _updateInvestmentHandler = updateInvestmentHandler
            ?? throw new ArgumentNullException(nameof(updateInvestmentHandler));

        _deactivateInvestmentHandler = deactivateInvestmentHandler
            ?? throw new ArgumentNullException(nameof(deactivateInvestmentHandler));

        _updateCurrentValueHandler = updateCurrentValueHandler
            ?? throw new ArgumentNullException(nameof(updateCurrentValueHandler));

        Title = "Gestão de Investimentos Financeiros";

        LoadDataCommand =
            new AsyncRelayCommand(
                LoadDataAsync);

        UpdateInvestmentCommand =
            new AsyncRelayCommand(
                UpdateInvestmentAsync,
                CanUpdateInvestment);

        DeleteInvestmentCommand =
            new AsyncRelayCommand(
                DeactivateInvestmentAsync,
                CanDeactivateInvestment);

        UpdateValueCommand =
            new AsyncRelayCommand(
                UpdateCurrentValueAsync,
                CanUpdateCurrentValue);

        NewInvestmentCommand =
            new AsyncRelayCommand(
                NavigateToNewInvestmentAsync);
    }

    public ObservableCollection<InvestmentDTO> Investments { get; } =
        new();

    public ObservableCollection<AccountDTO> EditAccounts { get; } =
        new();

    public Array InvestmentTypes =>
        Enum.GetValues<InvestmentType>()
            .Where(type => type != InvestmentType.NotSpecified)
            .ToArray();

    public InvestmentDTO? SelectedInvestment
    {
        get => _selectedInvestment;

        set
        {
            if (!SetProperty(
                    ref _selectedInvestment,
                    value))
            {
                return;
            }

            LoadSelectedInvestment();

            RefreshCommands();
        }
    }

    public string EditInvestmentName
    {
        get => _editInvestmentName;

        set
        {
            value ??= string.Empty;

            if (SetProperty(
                    ref _editInvestmentName,
                    value))
            {
                UpdateInvestmentCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    public InvestmentType EditInvestmentType
    {
        get => _editInvestmentType;

        set
        {
            if (SetProperty(
                    ref _editInvestmentType,
                    value))
            {
                UpdateInvestmentCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    public AccountDTO? EditSelectedAccount
    {
        get => _editSelectedAccount;

        set
        {
            if (SetProperty(
                    ref _editSelectedAccount,
                    value))
            {
                UpdateInvestmentCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Representa o NOVO valor atual absoluto do investimento.
    /// Não representa uma diferença a adicionar/subtrair.
    /// </summary>
    public decimal UpdateValueAmount
    {
        get => _updateValueAmount;

        set
        {
            if (SetProperty(
                    ref _updateValueAmount,
                    value))
            {
                UpdateValueCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    public decimal EditInvestmentInitialValue =>
        SelectedInvestment?.InitialValue ?? 0;

    public decimal EditInvestmentCurrentValue =>
        SelectedInvestment?.CurrentValue ?? 0;

    public string EditInvestmentCurrency =>
        SelectedInvestment?.Currency ?? string.Empty;

    public DateTime? EditInvestmentPurchaseDate =>
        SelectedInvestment?.PurchaseDate;

    public AsyncRelayCommand LoadDataCommand { get; }

    public AsyncRelayCommand UpdateInvestmentCommand { get; }

    public AsyncRelayCommand DeleteInvestmentCommand { get; }

    public AsyncRelayCommand UpdateValueCommand { get; }

    public AsyncRelayCommand NewInvestmentCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        await LoadInitialDataAsync(
            cancellationToken);

        IsInitialized = true;
    }

    private async Task LoadDataAsync()
    {
        await LoadInitialDataAsync(
            CancellationToken.None);
    }

    private async Task LoadInitialDataAsync(
        CancellationToken cancellationToken)
    {
        var userId =
            _currentUserService.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            Investments.Clear();

            _allEditAccounts.Clear();
            EditAccounts.Clear();

            SelectedInvestment = null;

            return;
        }

        await RunSafeAsync(async () =>
        {
            var selectedInvestmentId =
                SelectedInvestment?.Id;

            var investmentsResult =
                await _getInvestmentsHandler.HandleAsync(
                    new GetActiveInvestmentsByUserQuery(
                        userId.Value),
                    cancellationToken);

            if (investmentsResult.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    investmentsResult.Error.Message,
                    "Investimentos");

                return;
            }

            var accountsResult =
                await _getAccountsByUserHandler.HandleAsync(
                    new GetAccountsByUserQuery(
                        userId.Value),
                    cancellationToken);

            if (accountsResult.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    accountsResult.Error.Message,
                    "Contas");

                return;
            }

            Investments.Clear();

            foreach (var investment in investmentsResult.Value
                         .Where(investment => investment.IsActive)
                         .OrderBy(investment => investment.Name))
            {
                Investments.Add(investment);
            }

            /*
             * IMPORTANTE:
             *
             * _allEditAccounts mantém SEMPRE todas as contas
             * ativas do utilizador.
             *
             * EditAccounts é apenas a coleção filtrada de acordo
             * com a moeda do investimento selecionado.
             */
            _allEditAccounts.Clear();

            _allEditAccounts.AddRange(
                accountsResult.Value
                    .Where(account => account.IsActive)
                    .OrderBy(account => account.Name));

            /*
             * Se estivermos apenas a fazer refresh, tentamos
             * preservar o investimento selecionado.
             */
            if (selectedInvestmentId.HasValue)
            {
                SelectedInvestment =
                    Investments.FirstOrDefault(
                        investment =>
                            investment.Id ==
                            selectedInvestmentId.Value);
            }
            else
            {
                SelectedInvestment = null;

                EditAccounts.Clear();
            }
        });
    }

    private void LoadSelectedInvestment()
    {
        if (SelectedInvestment is null)
        {
            EditInvestmentName =
                string.Empty;

            EditInvestmentType =
                InvestmentType.NotSpecified;

            EditSelectedAccount =
                null;

            UpdateValueAmount =
                0;

            EditAccounts.Clear();

            NotifyInvestmentDetailsChanged();

            return;
        }

        EditInvestmentName =
            SelectedInvestment.Name;

        EditInvestmentType =
            SelectedInvestment.Type;

        /*
         * O campo representa o novo valor absoluto.
         * Inicializamos com o valor atual existente.
         */
        UpdateValueAmount =
            SelectedInvestment.CurrentValue;

        FilterEditAccounts();

        EditSelectedAccount =
            SelectedInvestment.AccountId.HasValue
                ? EditAccounts.FirstOrDefault(
                    account =>
                        account.Id ==
                        SelectedInvestment.AccountId.Value)
                : null;

        NotifyInvestmentDetailsChanged();
    }

    private void FilterEditAccounts()
    {
        EditAccounts.Clear();

        if (SelectedInvestment is null)
            return;

        /*
         * Não filtramos EditAccounts sobre ela própria.
         *
         * A fonte é sempre _allEditAccounts, evitando perder
         * contas quando se alterna entre investimentos de
         * moedas diferentes.
         */
        foreach (var account in _allEditAccounts.Where(
                     account =>
                         string.Equals(
                             account.Currency,
                             SelectedInvestment.Currency,
                             StringComparison.OrdinalIgnoreCase)))
        {
            EditAccounts.Add(account);
        }
    }

    private bool CanUpdateInvestment()
    {
        if (IsBusy ||
            SelectedInvestment is null ||
            !SelectedInvestment.IsActive ||
            string.IsNullOrWhiteSpace(EditInvestmentName) ||
            EditInvestmentName.Trim().Length < 3 ||
            EditInvestmentType == InvestmentType.NotSpecified)
        {
            return false;
        }

        var nameChanged =
            !string.Equals(
                EditInvestmentName.Trim(),
                SelectedInvestment.Name,
                StringComparison.Ordinal);

        var typeChanged =
            EditInvestmentType !=
            SelectedInvestment.Type;

        var accountChanged =
            EditSelectedAccount?.Id !=
            SelectedInvestment.AccountId;

        return nameChanged ||
               typeChanged ||
               accountChanged;
    }

    private async Task UpdateInvestmentAsync()
    {
        if (SelectedInvestment is null)
            return;

        var investmentId =
            SelectedInvestment.Id;

        var name =
            EditInvestmentName.Trim();

        var type =
            EditInvestmentType;

        var accountId =
            EditSelectedAccount?.Id;

        await RunSafeAsync(async () =>
        {
            var result =
                await _updateInvestmentHandler.HandleAsync(
                    new UpdateInvestmentCommand(
                        investmentId,
                        name,
                        type,
                        accountId));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível atualizar o investimento");

                return;
            }

            var updatedInvestment =
                result.Value;

            ReplaceInvestment(
                updatedInvestment);

            SelectedInvestment =
                updatedInvestment;

            await NotificationService.ShowInformationAsync(
                "Investimento atualizado com sucesso.",
                "Investimento");
        });

        RefreshCommands();
    }

    private bool CanDeactivateInvestment()
    {
        return !IsBusy &&
               SelectedInvestment is not null &&
               SelectedInvestment.IsActive;
    }

    private async Task DeactivateInvestmentAsync()
    {
        if (SelectedInvestment is null)
            return;

        var investment =
            SelectedInvestment;

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Deseja desativar '{investment.Name}'?",
                "Confirmar desativação");

        if (!confirmed)
            return;

        await RunSafeAsync(async () =>
        {
            var result =
                await _deactivateInvestmentHandler.HandleAsync(
                    new DeactivateInvestmentCommand(
                        investment.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível desativar o investimento");

                return;
            }

            Investments.Remove(
                investment);

            SelectedInvestment =
                null;

            await NotificationService.ShowInformationAsync(
                "Investimento desativado com sucesso.",
                "Investimento");
        });

        RefreshCommands();
    }

    private bool CanUpdateCurrentValue()
    {
        return !IsBusy &&
               SelectedInvestment is not null &&
               SelectedInvestment.IsActive &&
               UpdateValueAmount >= 0 &&
               UpdateValueAmount !=
               SelectedInvestment.CurrentValue;
    }

    private async Task UpdateCurrentValueAsync()
    {
        if (SelectedInvestment is null)
            return;

        var investmentId =
            SelectedInvestment.Id;

        /*
         * Este valor é absoluto.
         *
         * Exemplo:
         * Valor atual = 1 000
         * Utilizador introduz = 1 150
         * Novo CurrentValue = 1 150
         *
         * NÃO significa +1 150.
         */
        var newCurrentValue =
            UpdateValueAmount;

        await RunSafeAsync(async () =>
        {
            var result =
                await _updateCurrentValueHandler.HandleAsync(
                    new UpdateInvestmentCurrentValueCommand(
                        investmentId,
                        newCurrentValue));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível atualizar o valor");

                return;
            }

            var updatedInvestment =
                result.Value;

            ReplaceInvestment(
                updatedInvestment);

            SelectedInvestment =
                updatedInvestment;

            UpdateValueAmount =
                updatedInvestment.CurrentValue;

            await NotificationService.ShowInformationAsync(
                "Valor atual do investimento atualizado.",
                "Investimento");
        });

        RefreshCommands();
    }

    private async Task NavigateToNewInvestmentAsync()
    {
        await RunSafeAsync(async () =>
        {
            await NavigationService
                .NavigateTo<InvestmentFormViewModel>();
        });
    }

    private void ReplaceInvestment(
        InvestmentDTO updatedInvestment)
    {
        var existing =
            Investments.FirstOrDefault(
                investment =>
                    investment.Id ==
                    updatedInvestment.Id);

        if (existing is null)
        {
            Investments.Add(
                updatedInvestment);

            SortInvestments();

            return;
        }

        var index =
            Investments.IndexOf(existing);

        Investments[index] =
            updatedInvestment;

        SortInvestments();
    }

    private void SortInvestments()
    {
        var orderedInvestments =
            Investments
                .OrderBy(investment => investment.Name)
                .ToList();

        Investments.Clear();

        foreach (var investment in orderedInvestments)
        {
            Investments.Add(investment);
        }
    }

    private void NotifyInvestmentDetailsChanged()
    {
        OnPropertyChanged(
            nameof(EditInvestmentInitialValue));

        OnPropertyChanged(
            nameof(EditInvestmentCurrentValue));

        OnPropertyChanged(
            nameof(EditInvestmentCurrency));

        OnPropertyChanged(
            nameof(EditInvestmentPurchaseDate));
    }

    private void RefreshCommands()
    {
        UpdateInvestmentCommand
            .RaiseCanExecuteChanged();

        DeleteInvestmentCommand
            .RaiseCanExecuteChanged();

        UpdateValueCommand
            .RaiseCanExecuteChanged();
    }
}