using System.Collections.ObjectModel;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Goals.Commands.AddGoalProgress;
using FamilyManagement.Application.UseCases.Goals.Commands.DeactivateGoal;
using FamilyManagement.Application.UseCases.Goals.Commands.WithdrawGoalProgress;
using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Queries.GetGoalsByUser;

namespace Family_Management.WPF.ViewModel.Goals;

public sealed class GoalViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;
    private readonly GetGoalsByUserHandler _getGoalsByUserHandler;
    private readonly DeactivateGoalHandler _deactivateGoalHandler;
    private readonly AddGoalProgressHandler _addGoalProgressHandler;
    private readonly WithdrawGoalProgressHandler _withdrawGoalProgressHandler;
    private readonly GoalFormViewModel _goalFormViewModel;

    private GoalDTO? _selectedGoal;
    private decimal _progressAmount;

    public GoalViewModel(
        CurrentUserService currentUserService,
        GetGoalsByUserHandler getGoalsByUserHandler,
        DeactivateGoalHandler deactivateGoalHandler,
        AddGoalProgressHandler addGoalProgressHandler,
        WithdrawGoalProgressHandler withdrawGoalProgressHandler,
        GoalFormViewModel goalFormViewModel,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(navigationService, notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _getGoalsByUserHandler = getGoalsByUserHandler
            ?? throw new ArgumentNullException(nameof(getGoalsByUserHandler));

        _deactivateGoalHandler = deactivateGoalHandler
            ?? throw new ArgumentNullException(nameof(deactivateGoalHandler));

        _addGoalProgressHandler = addGoalProgressHandler
            ?? throw new ArgumentNullException(nameof(addGoalProgressHandler));

        _withdrawGoalProgressHandler = withdrawGoalProgressHandler
            ?? throw new ArgumentNullException(nameof(withdrawGoalProgressHandler));

        _goalFormViewModel = goalFormViewModel
            ?? throw new ArgumentNullException(nameof(goalFormViewModel));

        Title = "Gestão de Metas Financeiras";

        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);

        DeleteGoalCommand = new AsyncRelayCommand(
            DeactivateGoalAsync,
            CanDeactivateGoal);

        AddProgressCommand = new AsyncRelayCommand(
            AddProgressAsync,
            CanAddProgress);

        SubtractProgressCommand = new AsyncRelayCommand(
            WithdrawProgressAsync,
            CanWithdrawProgress);

        NewGoalCommand = new RelayCommand(
            _ => NewGoal());

        _goalFormViewModel.GoalSaved += OnGoalSaved;
    }

    public ObservableCollection<GoalDTO> Goals { get; } = new();

    public GoalFormViewModel GoalForm => _goalFormViewModel;

    public GoalDTO? SelectedGoal
    {
        get => _selectedGoal;

        set
        {
            if (!SetProperty(ref _selectedGoal, value))
            {
                return;
            }

            ProgressAmount = 0;

            if (value is null)
            {
                GoalForm.ResetForm();
            }
            else
            {
                GoalForm.SetGoalToEdit(value);
            }

            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(CanEditOrAddProgress));
            OnPropertyChanged(nameof(CanWithdrawFromGoal));

            RefreshCommands();
        }
    }

    public decimal ProgressAmount
    {
        get => _progressAmount;

        set
        {
            if (SetProperty(ref _progressAmount, value))
            {
                RefreshCommands();
            }
        }
    }

    public bool CanEditOrAddProgress =>
        SelectedGoal is not null &&
        SelectedGoal.IsActive;

    public bool CanWithdrawFromGoal =>
        SelectedGoal is not null &&
        SelectedGoal.IsActive &&
        SelectedGoal.CurrentAmount > 0;

    public string FormTitle =>
        SelectedGoal is null
            ? "Nova Meta de Poupança"
            : $"Editar: {SelectedGoal.Name}";

    public AsyncRelayCommand LoadDataCommand { get; }

    public AsyncRelayCommand DeleteGoalCommand { get; }

    public AsyncRelayCommand AddProgressCommand { get; }

    public AsyncRelayCommand SubtractProgressCommand { get; }

    public RelayCommand NewGoalCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        await GoalForm.InitializeAsync(cancellationToken);

        await LoadGoalsAsync(cancellationToken);

        IsInitialized = true;
    }

    private async Task LoadDataAsync()
    {
        await LoadGoalsAsync(CancellationToken.None);
    }

    private async Task LoadGoalsAsync(
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            Goals.Clear();
            SelectedGoal = null;
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result = await _getGoalsByUserHandler.HandleAsync(
                new GetGoalsByUserQuery(userId.Value),
                cancellationToken);

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Metas");

                return;
            }

            Goals.Clear();

            foreach (var goal in result.Value
                         .Where(goal => goal.IsActive)
                         .OrderBy(goal => goal.IsAchieved)
                         .ThenBy(goal => goal.TargetDate)
                         .ThenBy(goal => goal.Name))
            {
                Goals.Add(goal);
            }

            if (SelectedGoal is not null)
            {
                var selectedId = SelectedGoal.Id;

                SelectedGoal = Goals.FirstOrDefault(
                    goal => goal.Id == selectedId);
            }
        });
    }

    private void OnGoalSaved(GoalDTO savedGoal)
    {
        var existing = Goals.FirstOrDefault(
            goal => goal.Id == savedGoal.Id);

        if (existing is null)
        {
            Goals.Add(savedGoal);
        }
        else
        {
            var index = Goals.IndexOf(existing);
            Goals[index] = savedGoal;
        }

        SortGoals();

        SelectedGoal = null;

        RefreshCommands();
    }

    private void NewGoal()
    {
        SelectedGoal = null;

        ProgressAmount = 0;

        GoalForm.ResetForm();

        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(CanEditOrAddProgress));
        OnPropertyChanged(nameof(CanWithdrawFromGoal));

        RefreshCommands();
    }

    private bool CanDeactivateGoal()
    {
        return !IsBusy &&
               SelectedGoal is not null &&
               SelectedGoal.IsActive;
    }

    private async Task DeactivateGoalAsync()
    {
        if (SelectedGoal is null)
        {
            return;
        }

        var goal = SelectedGoal;

        var confirmed = await NotificationService.ShowConfirmationAsync(
            $"Deseja desativar a meta '{goal.Name}'?",
            "Confirmar");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result = await _deactivateGoalHandler.HandleAsync(
                new DeactivateGoalCommand(goal.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível desativar a meta");

                return;
            }

            Goals.Remove(goal);

            SelectedGoal = null;

            await NotificationService.ShowInformationAsync(
                "Meta desativada com sucesso.",
                "Meta");
        });

        RefreshCommands();
    }

    private bool CanAddProgress()
    {
        return !IsBusy &&
               SelectedGoal is not null &&
               SelectedGoal.IsActive &&
               ProgressAmount > 0;
    }

    private async Task AddProgressAsync()
    {
        if (SelectedGoal is null ||
            ProgressAmount <= 0)
        {
            return;
        }

        var goalId = SelectedGoal.Id;
        var amount = ProgressAmount;

        await RunSafeAsync(async () =>
        {
            var result = await _addGoalProgressHandler.HandleAsync(
                new AddGoalProgressCommand(
                    goalId,
                    amount));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível atualizar o progresso");

                return;
            }

            ReplaceGoal(result.Value);

            SelectedGoal = result.Value;

            ProgressAmount = 0;

            await NotificationService.ShowInformationAsync(
                "Progresso adicionado com sucesso.",
                "Meta");
        });

        RefreshCommands();
    }

    private bool CanWithdrawProgress()
    {
        return !IsBusy &&
               SelectedGoal is not null &&
               SelectedGoal.IsActive &&
               ProgressAmount > 0 &&
               ProgressAmount <= SelectedGoal.CurrentAmount;
    }

    private async Task WithdrawProgressAsync()
    {
        if (SelectedGoal is null ||
            ProgressAmount <= 0)
        {
            return;
        }

        var goalId = SelectedGoal.Id;
        var amount = ProgressAmount;

        await RunSafeAsync(async () =>
        {
            var result = await _withdrawGoalProgressHandler.HandleAsync(
                new WithdrawGoalProgressCommand(
                    goalId,
                    amount));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível retirar o progresso");

                return;
            }

            ReplaceGoal(result.Value);

            SelectedGoal = result.Value;

            ProgressAmount = 0;

            await NotificationService.ShowInformationAsync(
                "Progresso retirado com sucesso.",
                "Meta");
        });

        RefreshCommands();
    }

    private void ReplaceGoal(GoalDTO updatedGoal)
    {
        var existing = Goals.FirstOrDefault(
            goal => goal.Id == updatedGoal.Id);

        if (existing is null)
        {
            Goals.Add(updatedGoal);
        }
        else
        {
            var index = Goals.IndexOf(existing);
            Goals[index] = updatedGoal;
        }

        SortGoals();
    }

    private void SortGoals()
    {
        var orderedGoals = Goals
            .OrderBy(goal => goal.IsAchieved)
            .ThenBy(goal => goal.TargetDate)
            .ThenBy(goal => goal.Name)
            .ToList();

        Goals.Clear();

        foreach (var goal in orderedGoals)
        {
            Goals.Add(goal);
        }
    }

    private void RefreshCommands()
    {
        DeleteGoalCommand.RaiseCanExecuteChanged();
        AddProgressCommand.RaiseCanExecuteChanged();
        SubtractProgressCommand.RaiseCanExecuteChanged();
    }
}