using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using Family_Management.WPF.ViewModel.Models;
using FamilyManagement.Application.UseCases.Budgets.Commands.CreateBudget;
using FamilyManagement.Application.UseCases.Budgets.Commands.UpdateBudget;
using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Queries.GetAvailableCategoriesForUser;
using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Queries.GetCurrencies;
using FamilyManagement.Application.UseCases.Currencies.Queries.GetDefaultCurrency;

namespace Family_Management.WPF.ViewModel.Budgets;

public sealed class BudgetFormViewModel : BaseViewModel
{
    private readonly CreateBudgetHandler _createBudgetHandler;
    private readonly UpdateBudgetHandler _updateBudgetHandler;
    private readonly GetAvailableCategoriesForUserHandler _getCategoriesHandler;
    private readonly GetCurrenciesHandler _getCurrenciesHandler;
    private readonly GetDefaultCurrencyHandler _getDefaultCurrencyHandler;
    private readonly CurrentUserService _currentUserService;

    private BudgetFormModel _form;
    private CategoryDTO? _selectedCategory;

    public BudgetFormViewModel(
        CreateBudgetHandler createBudgetHandler,
        UpdateBudgetHandler updateBudgetHandler,
        GetAvailableCategoriesForUserHandler getCategoriesHandler,
        GetCurrenciesHandler getCurrenciesHandler,
        GetDefaultCurrencyHandler getDefaultCurrencyHandler,
        CurrentUserService currentUserService,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _createBudgetHandler = createBudgetHandler
            ?? throw new ArgumentNullException(
                nameof(createBudgetHandler));

        _updateBudgetHandler = updateBudgetHandler
            ?? throw new ArgumentNullException(
                nameof(updateBudgetHandler));

        _getCategoriesHandler = getCategoriesHandler
            ?? throw new ArgumentNullException(
                nameof(getCategoriesHandler));

        _getCurrenciesHandler = getCurrenciesHandler
            ?? throw new ArgumentNullException(
                nameof(getCurrenciesHandler));

        _getDefaultCurrencyHandler = getDefaultCurrencyHandler
            ?? throw new ArgumentNullException(
                nameof(getDefaultCurrencyHandler));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        Title = "Orçamento";

        _form = new BudgetFormModel();
        SubscribeToForm(_form);

        SaveCommand = new AsyncRelayCommand(
            SaveAsync,
            CanSave);

        CancelCommand = new RelayCommand(
            Cancel);
    }

    public ObservableCollection<CategoryDTO> AvailableCategories { get; }
        = new();

    public ObservableCollection<CurrencyDTO> AvailableCurrencies { get; }
        = new();

    public BudgetFormModel Form
    {
        get => _form;

        private set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(_form, value))
            {
                return;
            }

            UnsubscribeFromForm(_form);

            _form = value;

            SubscribeToForm(_form);

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditMode));

            RaiseSaveCanExecuteChanged();
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

            Form.CategoryId = value?.Id;

            RaiseSaveCanExecuteChanged();
        }
    }

    public bool IsEditMode => Form.IsEditMode;

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public event Action<BudgetDTO>? BudgetSaved;

    public event Action? Cancelled;

    public async Task LoadDependenciesAsync(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            await NotificationService.ShowErrorAsync(
                "Não existe um utilizador autenticado.",
                "Sessão");

            return;
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

        AvailableCategories.Clear();

        foreach (var category in categoriesResult.Value)
        {
            AvailableCategories.Add(category);
        }

        var currenciesResult =
            await _getCurrenciesHandler.HandleAsync(
                new GetCurrenciesQuery(
                    OnlyActive: true),
                cancellationToken);

        if (currenciesResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                currenciesResult.Error.Message,
                "Moedas");

            return;
        }

        AvailableCurrencies.Clear();

        foreach (var currency in currenciesResult.Value)
        {
            AvailableCurrencies.Add(currency);
        }
    }

    public async Task PrepareNewAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var defaultCurrencyResult =
            await _getDefaultCurrencyHandler.HandleAsync(
                new GetDefaultCurrencyQuery(),
                cancellationToken);

        var currency =
            defaultCurrencyResult.IsSuccess
                ? defaultCurrencyResult.Value.Code
                : AvailableCurrencies
                    .FirstOrDefault()
                    ?.Code
                  ?? string.Empty;

        Form = BudgetFormModel.CreateNew(
            currency,
            month,
            year);

        SelectedCategory =
            AvailableCategories.FirstOrDefault();
    }

    public void SetBudgetForEdit(
        BudgetDTO budget)
    {
        ArgumentNullException.ThrowIfNull(budget);

        Form = BudgetFormModel.FromBudget(
            budget);

        SelectedCategory =
            AvailableCategories.FirstOrDefault(
                category =>
                    category.Id == budget.CategoryId);
    }

    private bool CanSave()
    {
        return !IsBusy &&
               !string.IsNullOrWhiteSpace(Form.Name) &&
               Form.Amount > 0 &&
               Form.CategoryId.HasValue &&
               Form.CategoryId.Value != Guid.Empty &&
               !string.IsNullOrWhiteSpace(Form.Currency) &&
               Form.Month is >= 1 and <= 12 &&
               Form.Year is >= 2000 and <= 9999;
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
                await UpdateAsync();
            }
            else
            {
                await CreateAsync(userId);
            }
        });

        RaiseSaveCanExecuteChanged();
    }

    private async Task CreateAsync(
        Guid userId)
    {
        if (!Form.CategoryId.HasValue ||
            Form.CategoryId.Value == Guid.Empty)
        {
            return;
        }

        var command = new CreateBudgetCommand(
            Form.Name.Trim(),
            Form.Amount,
            Form.Currency.Trim().ToUpperInvariant(),
            Form.Month,
            Form.Year,
            Form.CategoryId.Value,
            userId);

        var result =
            await _createBudgetHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Criar orçamento");

            return;
        }

        BudgetSaved?.Invoke(result.Value);
    }

    private async Task UpdateAsync()
    {
        if (!Form.BudgetId.HasValue ||
            Form.BudgetId.Value == Guid.Empty)
        {
            return;
        }

        var command = new UpdateBudgetCommand(
            Form.BudgetId.Value,
            Form.Name.Trim(),
            Form.Amount);

        var result =
            await _updateBudgetHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Atualizar orçamento");

            return;
        }

        BudgetSaved?.Invoke(result.Value);
    }

    private void Cancel()
    {
        Cancelled?.Invoke();
    }

    private void SubscribeToForm(
        BudgetFormModel form)
    {
        form.PropertyChanged +=
            OnFormPropertyChanged;
    }

    private void UnsubscribeFromForm(
        BudgetFormModel form)
    {
        form.PropertyChanged -=
            OnFormPropertyChanged;
    }

    private void OnFormPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BudgetFormModel.BudgetId))
        {
            OnPropertyChanged(nameof(IsEditMode));
        }

        RaiseSaveCanExecuteChanged();
    }

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        userId =
            _currentUserService.UserId
            ?? Guid.Empty;

        return userId != Guid.Empty;
    }

    private void RaiseSaveCanExecuteChanged()
    {
        if (SaveCommand is AsyncRelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }
}