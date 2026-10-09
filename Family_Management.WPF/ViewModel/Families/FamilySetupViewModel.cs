using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Families.Commands.CreateFamily;
using FamilyManagement.Application.UseCases.Families.Commands.JoinFamily;

namespace Family_Management.WPF.ViewModel.Families;

public sealed class FamilySetupViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;
    private readonly CreateFamilyHandler _createFamilyHandler;
    private readonly JoinFamilyHandler _joinFamilyHandler;

    private string _newFamilyName = string.Empty;
    private string _invitationCode = string.Empty;

    public FamilySetupViewModel(
        CurrentUserService currentUserService,
        CreateFamilyHandler createFamilyHandler,
        JoinFamilyHandler joinFamilyHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _createFamilyHandler = createFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(createFamilyHandler));

        _joinFamilyHandler = joinFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(joinFamilyHandler));

        Title = "Configuração Inicial de Família";

        CreateFamilyCommand =
            new AsyncRelayCommand(
                ExecuteCreateFamilyAsync,
                CanCreateFamily);

        JoinFamilyCommand =
            new AsyncRelayCommand(
                ExecuteJoinFamilyAsync,
                CanJoinFamily);
    }

    public string NewFamilyName
    {
        get => _newFamilyName;

        set
        {
            value ??= string.Empty;

            if (SetProperty(
                    ref _newFamilyName,
                    value))
            {
                CreateFamilyCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    public string InvitationCode
    {
        get => _invitationCode;

        set
        {
            value ??= string.Empty;

            if (SetProperty(
                    ref _invitationCode,
                    value))
            {
                JoinFamilyCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand CreateFamilyCommand { get; }

    public AsyncRelayCommand JoinFamilyCommand { get; }

    private bool CanCreateFamily()
    {
        return !IsBusy &&
               _currentUserService.IsAuthenticated &&
               !string.IsNullOrWhiteSpace(NewFamilyName) &&
               NewFamilyName.Trim().Length >= 3;
    }

    private bool CanJoinFamily()
    {
        return !IsBusy &&
               _currentUserService.IsAuthenticated &&
               !string.IsNullOrWhiteSpace(InvitationCode);
    }

    private async Task ExecuteCreateFamilyAsync()
    {
        var userId =
            _currentUserService.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            await NotificationService
                .ShowWarningAsync(
                    "Não existe um utilizador autenticado.",
                    "Família");

            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _createFamilyHandler
                    .HandleAsync(
                        new CreateFamilyCommand(
                            NewFamilyName.Trim(),
                            userId.Value));

            if (result.IsFailure)
            {
                await NotificationService
                    .ShowErrorAsync(
                        result.Error.Message,
                        "Não foi possível criar a família");

                return;
            }

            NewFamilyName =
                string.Empty;

            await NotificationService
                .ShowInformationAsync(
                    "Família criada com sucesso.",
                    "Família");

            await NavigationService
                .NavigateToScreen(
                    AppScreen.Dashboard);
        });
    }

    private async Task ExecuteJoinFamilyAsync()
    {
        var userId =
            _currentUserService.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            await NotificationService
                .ShowWarningAsync(
                    "Não existe um utilizador autenticado.",
                    "Família");

            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _joinFamilyHandler
                    .HandleAsync(
                        new JoinFamilyCommand(
                            InvitationCode
                                .Trim()
                                .ToUpperInvariant(),
                            userId.Value));

            if (result.IsFailure)
            {
                await NotificationService
                    .ShowErrorAsync(
                        result.Error.Message,
                        "Não foi possível aderir à família");

                return;
            }

            InvitationCode =
                string.Empty;

            await NotificationService
                .ShowInformationAsync(
                    "Adesão à família concluída com sucesso.",
                    "Família");

            await NavigationService
                .NavigateToScreen(
                    AppScreen.Dashboard);
        });
    }
}