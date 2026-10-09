using System.Collections.ObjectModel;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Queries.GetCurrencies;
using FamilyManagement.Application.UseCases.Goals.Commands.CreateGoal;
using FamilyManagement.Application.UseCases.Goals.Commands.UpdateGoal;
using FamilyManagement.Application.UseCases.Goals.DTOs;

namespace Family_Management.WPF.ViewModel.Goals;

public sealed class GoalFormViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;
    private readonly CreateGoalHandler _createGoalHandler;
    private readonly UpdateGoalHandler _updateGoalHandler;
    private readonly GetCurrenciesHandler _getCurrenciesHandler;

    private GoalDTO? _originalGoal;

    private string _goalName = string.Empty;
    private decimal _goalTargetAmount;
    private DateTime? _goalTargetDate;
    private CurrencyDTO? _goalSelectedCurrency;

    public GoalFormViewModel(
        CurrentUserService currentUserService,
        CreateGoalHandler createGoalHandler,
        UpdateGoalHandler updateGoalHandler,
        GetCurrenciesHandler getCurrenciesHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(navigationService, notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _createGoalHandler = createGoalHandler
            ?? throw new ArgumentNullException(nameof(createGoalHandler));

        _updateGoalHandler = updateGoalHandler
            ?? throw new ArgumentNullException(nameof(updateGoalHandler));

        _getCurrenciesHandler = getCurrenciesHandler
            ?? throw new ArgumentNullException(nameof(getCurrenciesHandler));

        Title = "Meta Financeira";

        SaveGoalCommand = new AsyncRelayCommand(
            SaveGoalAsync,
            CanSaveGoal);
    }

    public ObservableCollection<CurrencyDTO> Currencies { get; } = new();

    public string GoalName
    {
        get => _goalName;

        set
        {
            value ??= string.Empty;

            if (SetProperty(ref _goalName, value))
            {
                SaveGoalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public decimal GoalTargetAmount
    {
        get => _goalTargetAmount;
        set
        {
            if (SetProperty(ref _goalTargetAmount, value))
            {
                SaveGoalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DateTime? GoalTargetDate
    {
        get => _goalTargetDate;
        set
        {
            if (SetProperty(ref _goalTargetDate, value))
            {
                SaveGoalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public CurrencyDTO? GoalSelectedCurrency
    {
        get => _goalSelectedCurrency;
        set
        {
            if (SetProperty(ref _goalSelectedCurrency, value))
            {
                SaveGoalCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsEditing => _originalGoal is not null;

    public bool CanChangeCurrency => !IsEditing;

    public string SaveButtonText =>
        IsEditing
            ? "💾 Guardar Alterações"
            : "✅ Criar Meta";

    public AsyncRelayCommand SaveGoalCommand { get; }

    public event Action<GoalDTO>? GoalSaved;

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        await LoadCurrenciesAsync(cancellationToken);

        ResetForm();

        IsInitialized = true;
    }

    public void SetGoalToEdit(GoalDTO goal)
    {
        ArgumentNullException.ThrowIfNull(goal);

        _originalGoal = goal;

        _goalName = goal.Name;
        _goalTargetAmount = goal.TargetAmount;
        _goalTargetDate = goal.TargetDate;

        _goalSelectedCurrency = Currencies.FirstOrDefault(
            currency =>
                currency.Code.Equals(
                    goal.Currency,
                    StringComparison.OrdinalIgnoreCase));

        OnPropertyChanged(nameof(GoalName));
        OnPropertyChanged(nameof(GoalTargetAmount));
        OnPropertyChanged(nameof(GoalTargetDate));
        OnPropertyChanged(nameof(GoalSelectedCurrency));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(CanChangeCurrency));
        OnPropertyChanged(nameof(SaveButtonText));

        SaveGoalCommand.RaiseCanExecuteChanged();
    }

    public void ResetForm()
    {
        _originalGoal = null;

        _goalName = string.Empty;
        _goalTargetAmount = 0;
        _goalTargetDate = null;

        _goalSelectedCurrency =
            Currencies.FirstOrDefault(currency => currency.IsDefault)
            ?? Currencies.FirstOrDefault();

        OnPropertyChanged(nameof(GoalName));
        OnPropertyChanged(nameof(GoalTargetAmount));
        OnPropertyChanged(nameof(GoalTargetDate));
        OnPropertyChanged(nameof(GoalSelectedCurrency));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(CanChangeCurrency));
        OnPropertyChanged(nameof(SaveButtonText));

        SaveGoalCommand.RaiseCanExecuteChanged();
    }

    private async Task LoadCurrenciesAsync(
        CancellationToken cancellationToken)
    {
        await RunSafeAsync(async () =>
        {
            var result = await _getCurrenciesHandler.HandleAsync(
                new GetCurrenciesQuery(OnlyActive: true),
                cancellationToken);

            Currencies.Clear();

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Moedas");

                return;
            }

            foreach (var currency in result.Value.OrderBy(c => c.Name))
            {
                Currencies.Add(currency);
            }
        });
    }

    private bool CanSaveGoal()
    {
        if (IsBusy ||
            !_currentUserService.IsAuthenticated ||
            string.IsNullOrWhiteSpace(GoalName) ||
            GoalTargetAmount <= 0 ||
            GoalSelectedCurrency is null)
        {
            return false;
        }

        if (GoalTargetDate.HasValue &&
            GoalTargetDate.Value.Date < DateTime.Today)
        {
            return false;
        }

        return true;
    }

    private async Task SaveGoalAsync()
    {
        var userId = _currentUserService.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            await NotificationService.ShowWarningAsync(
                "Não existe um utilizador autenticado.",
                "Meta");

            return;
        }

        await RunSafeAsync(async () =>
        {
            if (_originalGoal is null)
            {
                await CreateGoalAsync(userId.Value);
                return;
            }

            await UpdateGoalAsync();
        });
    }

    private async Task CreateGoalAsync(Guid userId)
    {
        if (GoalSelectedCurrency is null)
        {
            return;
        }

        var result = await _createGoalHandler.HandleAsync(
            new CreateGoalCommand(
                GoalName.Trim(),
                GoalTargetAmount,
                GoalSelectedCurrency.Code,
                userId,
                GoalTargetDate));

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Não foi possível criar a meta");

            return;
        }

        var savedGoal = result.Value;

        ResetForm();

        GoalSaved?.Invoke(savedGoal);

        await NotificationService.ShowInformationAsync(
            "Meta criada com sucesso.",
            "Meta");
    }

    private async Task UpdateGoalAsync()
    {
        if (_originalGoal is null)
        {
            return;
        }

        var result = await _updateGoalHandler.HandleAsync(
            new UpdateGoalCommand(
                _originalGoal.Id,
                GoalName.Trim(),
                GoalTargetAmount,
                GoalTargetDate));

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Não foi possível atualizar a meta");

            return;
        }

        var savedGoal = result.Value;

        ResetForm();

        GoalSaved?.Invoke(savedGoal);

        await NotificationService.ShowInformationAsync(
            "Meta atualizada com sucesso.",
            "Meta");
    }
}