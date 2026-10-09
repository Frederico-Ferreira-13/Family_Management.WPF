using System.Collections.ObjectModel;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Transactions.Commands.ConfirmTransaction;
using FamilyManagement.Application.UseCases.Transactions.Commands.DeactivateTransaction;
using FamilyManagement.Application.UseCases.Transactions.Commands.UnconfirmTransaction;
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Queries.GetTransactionsByUser;
using FamilyManagement.Domain.Interfaces;

namespace Family_Management.WPF.ViewModel.Transactions;

public sealed class TransactionViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;

    private readonly GetTransactionsByUserHandler
        _getTransactionsByUserHandler;

    private readonly ConfirmTransactionHandler
        _confirmTransactionHandler;

    private readonly UnconfirmTransactionHandler
        _unconfirmTransactionHandler;

    private readonly DeactivateTransactionHandler
        _deactivateTransactionHandler;

    private TransactionDTO? _selectedTransaction;

    private string _searchTerm = string.Empty;

    private bool? _confirmedFilter;

    public TransactionViewModel(
        CurrentUserService currentUserService,
        GetTransactionsByUserHandler getTransactionsByUserHandler,
        ConfirmTransactionHandler confirmTransactionHandler,
        UnconfirmTransactionHandler unconfirmTransactionHandler,
        DeactivateTransactionHandler deactivateTransactionHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _getTransactionsByUserHandler =
            getTransactionsByUserHandler
            ?? throw new ArgumentNullException(
                nameof(getTransactionsByUserHandler));

        _confirmTransactionHandler =
            confirmTransactionHandler
            ?? throw new ArgumentNullException(
                nameof(confirmTransactionHandler));

        _unconfirmTransactionHandler =
            unconfirmTransactionHandler
            ?? throw new ArgumentNullException(
                nameof(unconfirmTransactionHandler));

        _deactivateTransactionHandler =
            deactivateTransactionHandler
            ?? throw new ArgumentNullException(
                nameof(deactivateTransactionHandler));

        Title = "Transações";

        RefreshCommand =
            new AsyncRelayCommand(
                ExecuteRefreshAsync,
                () => !IsBusy);

        CreateCommand =
            new AsyncRelayCommand(
                ExecuteCreateAsync,
                () => !IsBusy);

        EditCommand =
            new AsyncRelayCommand(
                ExecuteEditAsync,
                CanEdit);

        ConfirmCommand =
            new AsyncRelayCommand(
                ExecuteConfirmAsync,
                CanConfirm);

        UnconfirmCommand =
            new AsyncRelayCommand(
                ExecuteUnconfirmAsync,
                CanUnconfirm);

        DeactivateCommand =
            new AsyncRelayCommand(
                ExecuteDeactivateAsync,
                CanDeactivate);

        ClearFiltersCommand =
            new AsyncRelayCommand(
                ExecuteClearFiltersAsync,
                () => !IsBusy);
    }

    public ObservableCollection<TransactionDTO>
        Transactions { get; } = new();

    public TransactionDTO? SelectedTransaction
    {
        get => _selectedTransaction;

        set
        {
            if (!SetProperty(
                    ref _selectedTransaction,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public string SearchTerm
    {
        get => _searchTerm;

        set
        {
            if (!SetProperty(
                    ref _searchTerm,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public bool? ConfirmedFilter
    {
        get => _confirmedFilter;

        set => SetProperty(
            ref _confirmedFilter,
            value);
    }

    public ICommand RefreshCommand { get; }

    public ICommand CreateCommand { get; }

    public ICommand EditCommand { get; }

    public ICommand ConfirmCommand { get; }

    public ICommand UnconfirmCommand { get; }

    public ICommand DeactivateCommand { get; }

    public ICommand ClearFiltersCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        await LoadTransactionsAsync(
            cancellationToken);

        IsInitialized = true;
    }

    private async Task ExecuteRefreshAsync()
    {
        await LoadTransactionsAsync();
    }

    private async Task LoadTransactionsAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            Transactions.Clear();
            SelectedTransaction = null;

            return;
        }

        await RunSafeAsync(
            async () =>
            {
                var filter =
                    new TransactionFilter
                    {
                        IsConfirmed =
                            ConfirmedFilter,

                        SearchTerm =
                            string.IsNullOrWhiteSpace(
                                SearchTerm)
                                ? null
                                : SearchTerm.Trim(),

                        IncludeDetails = true
                    };

                var result =
                    await _getTransactionsByUserHandler
                        .HandleAsync(
                            new GetTransactionsByUserQuery(
                                userId.Value,
                                filter));

                cancellationToken.ThrowIfCancellationRequested();

                if (result.IsFailure)
                {
                    await NotificationService.ShowErrorAsync(
                        result.Error.Message,
                        "Transações");

                    return;
                }

                Transactions.Clear();

                foreach (var transaction
                         in result.Value
                             .Where(x => x.IsActive)
                             .OrderByDescending(x => x.Date))
                {
                    Transactions.Add(transaction);
                }

                SelectedTransaction = null;
            },
            cancellationToken: cancellationToken);

        RaiseCommandStates();
    }

    private Task ExecuteCreateAsync()
    {
        return NavigationService
            .NavigateTo<TransactionFormViewModel>();
    }

    private bool CanEdit()
    {
        return !IsBusy &&
               SelectedTransaction is not null;
    }

    private Task ExecuteEditAsync()
    {
        if (SelectedTransaction is null)
            return Task.CompletedTask;

        return NavigationService
            .NavigateTo<TransactionFormViewModel, Guid>(
                SelectedTransaction.Id);
    }

    private bool CanConfirm()
    {
        return !IsBusy &&
               SelectedTransaction is
               {
                   IsActive: true,
                   IsConfirmed: false
               };
    }

    private async Task ExecuteConfirmAsync()
    {
        var transaction = SelectedTransaction;

        if (transaction is null)
            return;

        await RunSafeAsync(
            async () =>
            {
                var result =
                    await _confirmTransactionHandler
                        .HandleAsync(
                            new ConfirmTransactionCommand(
                                transaction.Id));

                if (result.IsFailure)
                {
                    await NotificationService.ShowErrorAsync(
                        result.Error.Message,
                        "Confirmar transação");

                    return;
                }

                await ReloadTransactionsCoreAsync();
            });

        RaiseCommandStates();
    }

    private bool CanUnconfirm()
    {
        return !IsBusy &&
               SelectedTransaction is
               {
                   IsActive: true,
                   IsConfirmed: true
               };
    }

    private async Task ExecuteUnconfirmAsync()
    {
        var transaction = SelectedTransaction;

        if (transaction is null)
            return;

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                "Pretende retirar a confirmação desta transação?",
                "Desconfirmar transação");

        if (!confirmed)
            return;

        await RunSafeAsync(
            async () =>
            {
                var result =
                    await _unconfirmTransactionHandler
                        .HandleAsync(
                            new UnconfirmTransactionCommand(
                                transaction.Id));

                if (result.IsFailure)
                {
                    await NotificationService.ShowErrorAsync(
                        result.Error.Message,
                        "Desconfirmar transação");

                    return;
                }

                await ReloadTransactionsCoreAsync();
            });

        RaiseCommandStates();
    }

    private bool CanDeactivate()
    {
        return !IsBusy &&
               SelectedTransaction is
               {
                   IsActive: true
               };
    }

    private async Task ExecuteDeactivateAsync()
    {
        var transaction = SelectedTransaction;

        if (transaction is null)
            return;

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                "Pretende eliminar esta transação? " +
                "O respetivo impacto financeiro será revertido.",
                "Eliminar transação");

        if (!confirmed)
            return;

        await RunSafeAsync(
            async () =>
            {
                var result =
                    await _deactivateTransactionHandler
                        .HandleAsync(
                            new DeactivateTransactionCommand(
                                transaction.Id));

                if (result.IsFailure)
                {
                    await NotificationService.ShowErrorAsync(
                        result.Error.Message,
                        "Eliminar transação");

                    return;
                }

                await ReloadTransactionsCoreAsync();
            });

        RaiseCommandStates();
    }

    private async Task ExecuteClearFiltersAsync()
    {
        SearchTerm = string.Empty;
        ConfirmedFilter = null;

        await LoadTransactionsAsync();
    }

    private async Task ReloadTransactionsCoreAsync()
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            Transactions.Clear();
            SelectedTransaction = null;

            return;
        }

        var filter =
            new TransactionFilter
            {
                IsConfirmed = ConfirmedFilter,

                SearchTerm =
                    string.IsNullOrWhiteSpace(
                        SearchTerm)
                        ? null
                        : SearchTerm.Trim(),

                IncludeDetails = true
            };

        var result =
            await _getTransactionsByUserHandler
                .HandleAsync(
                    new GetTransactionsByUserQuery(
                        userId.Value,
                        filter));

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Transações");

            return;
        }

        Transactions.Clear();

        foreach (var transaction
                 in result.Value
                     .Where(x => x.IsActive)
                     .OrderByDescending(x => x.Date))
        {
            Transactions.Add(transaction);
        }

        SelectedTransaction = null;
    }

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

    private void RaiseCommandStates()
    {
        if (RefreshCommand
            is AsyncRelayCommand refreshCommand)
        {
            refreshCommand.RaiseCanExecuteChanged();
        }

        if (CreateCommand
            is AsyncRelayCommand createCommand)
        {
            createCommand.RaiseCanExecuteChanged();
        }

        if (EditCommand
            is AsyncRelayCommand editCommand)
        {
            editCommand.RaiseCanExecuteChanged();
        }

        if (ConfirmCommand
            is AsyncRelayCommand confirmCommand)
        {
            confirmCommand.RaiseCanExecuteChanged();
        }

        if (UnconfirmCommand
            is AsyncRelayCommand unconfirmCommand)
        {
            unconfirmCommand.RaiseCanExecuteChanged();
        }

        if (DeactivateCommand
            is AsyncRelayCommand deactivateCommand)
        {
            deactivateCommand.RaiseCanExecuteChanged();
        }

        if (ClearFiltersCommand
            is AsyncRelayCommand clearFiltersCommand)
        {
            clearFiltersCommand.RaiseCanExecuteChanged();
        }
    }
}