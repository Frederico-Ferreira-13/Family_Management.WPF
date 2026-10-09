using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Users.Queries.GetUserById;

namespace Family_Management.WPF.ViewModel.Shell;

public sealed class HomeViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;
    private readonly GetUserByIdHandler _getUserByIdHandler;

    private string _welcomeMessage =
        "Bem-vindo(a) à Gestão Familiar!";

    public HomeViewModel(
        CurrentUserService currentUserService,
        GetUserByIdHandler getUserByIdHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _getUserByIdHandler = getUserByIdHandler
            ?? throw new ArgumentNullException(
                nameof(getUserByIdHandler));

        Title = "Início";

        NavigateToDashboardCommand =
            new AsyncRelayCommand(
                NavigateToDashboardAsync);

        NavigateToAccountsCommand =
            new AsyncRelayCommand(
                NavigateToAccountsAsync);
    }

    public string WelcomeMessage
    {
        get => _welcomeMessage;

        private set =>
            SetProperty(
                ref _welcomeMessage,
                value);
    }

    public AsyncRelayCommand NavigateToDashboardCommand { get; }

    public AsyncRelayCommand NavigateToAccountsCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        await LoadUserDataAsync(
            cancellationToken);

        IsInitialized = true;
    }

    private async Task LoadUserDataAsync(
        CancellationToken cancellationToken)
    {
        var userId =
            _currentUserService.UserId;

        if (!userId.HasValue)
        {
            WelcomeMessage =
                "Bem-vindo(a) à Gestão Familiar!";

            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _getUserByIdHandler.HandleAsync(
                    new GetUserByIdQuery(userId.Value),
                    cancellationToken);

            if (result.IsFailure)
            {
                WelcomeMessage =
                    "Bem-vindo(a) à Gestão Familiar!";

                return;
            }

            WelcomeMessage =
                $"Bem-vindo(a) de volta, {result.Value.UserName}!";
        });
    }

    private async Task NavigateToDashboardAsync()
    {
        await RunSafeAsync(async () =>
        {
            await NavigationService.NavigateToScreen(
                AppScreen.Dashboard);
        });
    }

    private async Task NavigateToAccountsAsync()
    {
        await RunSafeAsync(async () =>
        {
            await NavigationService.NavigateToScreen(
                AppScreen.Accounts);
        });
    }
}