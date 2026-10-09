using System.Collections.ObjectModel;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using FamilyManagement.Application.UseCases.Families.Commands.AddFamilyMember;
using FamilyManagement.Application.UseCases.Families.Commands.CreateFamily;
using FamilyManagement.Application.UseCases.Families.Commands.DeactivateFamily;
using FamilyManagement.Application.UseCases.Families.Commands.RegenerateInvitationCode;
using FamilyManagement.Application.UseCases.Families.Commands.RemoveFamilyMember;
using FamilyManagement.Application.UseCases.Families.Commands.RenameFamily;
using FamilyManagement.Application.UseCases.Families.DTOs;
using FamilyManagement.Application.UseCases.Families.Queries.GetActiveFamilies;
using FamilyManagement.Application.UseCases.Families.Queries.GetFamilyMembers;
using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Queries.GetUsersWithoutFamily;
using FamilyManagement.Application.UseCases.Families.Queries.GetFamilyById;

namespace Family_Management.WPF.ViewModel.Families;

public sealed class FamilyViewModel : BaseViewModel
{
    private readonly CurrentUserService _currentUserService;

    private readonly GetActiveFamiliesHandler
        _getActiveFamiliesHandler;

    private readonly GetFamilyMembersHandler
        _getFamilyMembersHandler;

    private readonly GetUsersWithoutFamilyHandler
        _getUsersWithoutFamilyHandler;

    private readonly CreateFamilyHandler
        _createFamilyHandler;

    private readonly RenameFamilyHandler
        _renameFamilyHandler;

    private readonly AddFamilyMemberHandler
        _addFamilyMemberHandler;

    private readonly RemoveFamilyMemberHandler
        _removeFamilyMemberHandler;

    private readonly DeactivateFamilyHandler
        _deactivateFamilyHandler;

    private readonly RegenerateInvitationCodeHandler
        _regenerateInvitationCodeHandler;

    private readonly GetFamilyByIdHandler
    _getFamilyByIdHandler;

    private string _formTitle = "Criar Nova Família";
    private string _newFamilyName = string.Empty;
    private string _editFamilyName = string.Empty;

    private FamilyDTO? _selectedFamily;
    private UserDTO? _selectedUserToAdd;
    private FamilyMemberDTO? _selectedUserToRemove;

    public FamilyViewModel(
        CurrentUserService currentUserService,
        GetActiveFamiliesHandler getActiveFamiliesHandler,
        GetFamilyMembersHandler getFamilyMembersHandler,
        GetUsersWithoutFamilyHandler getUsersWithoutFamilyHandler,
        CreateFamilyHandler createFamilyHandler,
        RenameFamilyHandler renameFamilyHandler,
        AddFamilyMemberHandler addFamilyMemberHandler,
        RemoveFamilyMemberHandler removeFamilyMemberHandler,
        DeactivateFamilyHandler deactivateFamilyHandler,
        RegenerateInvitationCodeHandler regenerateInvitationCodeHandler,
        GetFamilyByIdHandler getFamilyByIdHandler,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(
            navigationService,
            notificationService)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _getActiveFamiliesHandler = getActiveFamiliesHandler
            ?? throw new ArgumentNullException(
                nameof(getActiveFamiliesHandler));

        _getFamilyMembersHandler = getFamilyMembersHandler
            ?? throw new ArgumentNullException(
                nameof(getFamilyMembersHandler));

        _getUsersWithoutFamilyHandler =
            getUsersWithoutFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(getUsersWithoutFamilyHandler));

        _createFamilyHandler = createFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(createFamilyHandler));

        _renameFamilyHandler = renameFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(renameFamilyHandler));

        _addFamilyMemberHandler = addFamilyMemberHandler
            ?? throw new ArgumentNullException(
                nameof(addFamilyMemberHandler));

        _removeFamilyMemberHandler = removeFamilyMemberHandler
            ?? throw new ArgumentNullException(
                nameof(removeFamilyMemberHandler));

        _deactivateFamilyHandler = deactivateFamilyHandler
            ?? throw new ArgumentNullException(
                nameof(deactivateFamilyHandler));

        _regenerateInvitationCodeHandler =
            regenerateInvitationCodeHandler
            ?? throw new ArgumentNullException(
                nameof(regenerateInvitationCodeHandler));

        _getFamilyByIdHandler = getFamilyByIdHandler
            ?? throw new ArgumentNullException(
                nameof(getFamilyByIdHandler));

        Title = "Gestão de Famílias";

        AddFamilyCommand =
            new AsyncRelayCommand(
                AddFamilyAsync,
                CanAddFamily);

        UpdateFamilyCommand =
            new AsyncRelayCommand(
                UpdateFamilyAsync,
                CanUpdateFamily);

        AddUserToFamilyCommand =
            new AsyncRelayCommand(
                AddUserToFamilyAsync,
                CanAddUserToFamily);

        RemoveUserFromFamilyCommand =
            new AsyncRelayCommand(
                RemoveUserFromFamilyAsync,
                CanRemoveUserFromFamily);

        NewFamilyCommand =
            new RelayCommand(
                _ => ExecuteNewFamily());

        DeactivateFamilyCommand =
            new AsyncRelayCommand(
                DeactivateFamilyAsync,
                CanDeactivateFamily);

        RegenerateInvitationCodeCommand =
            new AsyncRelayCommand(
                RegenerateInvitationCodeAsync,
                CanRegenerateInvitationCode);
    }

    public ObservableCollection<FamilyDTO>
        Families { get; } = new();

    public ObservableCollection<UserDTO>
        AllAvailableUsers { get; } = new();

    public ObservableCollection<FamilyMemberDTO>
        SelectedFamilyMembers { get; } = new();

    public string FormTitle
    {
        get => _formTitle;

        private set =>
            SetProperty(
                ref _formTitle,
                value);
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
                AddFamilyCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    public string EditFamilyName
    {
        get => _editFamilyName;

        set
        {
            value ??= string.Empty;

            if (SetProperty(
                    ref _editFamilyName,
                    value))
            {
                UpdateFamilyCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    public FamilyDTO? SelectedFamily
    {
        get => _selectedFamily;

        set
        {
            if (!SetProperty(
                    ref _selectedFamily,
                    value))
            {
                return;
            }

            EditFamilyName =
                value?.Name ?? string.Empty;

            UpdateFormState();

            OnPropertyChanged(
                nameof(IsSelectedFamilyActive));

            OnPropertyChanged(
                nameof(HasSelectedFamily));

            RefreshFamilyCommandsCanExecute();

            _ = LoadSelectedFamilyMembersSafeAsync();
        }
    }

    public UserDTO? SelectedUserToAdd
    {
        get => _selectedUserToAdd;

        set
        {
            if (SetProperty(
                    ref _selectedUserToAdd,
                    value))
            {
                AddUserToFamilyCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    public FamilyMemberDTO? SelectedUserToRemove
    {
        get => _selectedUserToRemove;

        set
        {
            if (SetProperty(
                    ref _selectedUserToRemove,
                    value))
            {
                RemoveUserFromFamilyCommand
                    .RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsSelectedFamilyActive =>
        SelectedFamily?.IsActive ?? false;

    public bool HasSelectedFamily =>
        SelectedFamily is not null;

    public AsyncRelayCommand AddFamilyCommand { get; }

    public AsyncRelayCommand UpdateFamilyCommand { get; }

    public AsyncRelayCommand AddUserToFamilyCommand { get; }

    public AsyncRelayCommand RemoveUserFromFamilyCommand { get; }

    public RelayCommand NewFamilyCommand { get; }

    public AsyncRelayCommand DeactivateFamilyCommand { get; }

    public AsyncRelayCommand RegenerateInvitationCodeCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        await LoadDataAsync(
            cancellationToken);

        IsInitialized = true;
    }

    private async Task LoadDataAsync(
        CancellationToken cancellationToken)
    {
        await RunSafeAsync(async () =>
        {
            await LoadFamiliesAsync(
                cancellationToken);

            await LoadAvailableUsersAsync(
                cancellationToken);
        });
    }

    private async Task LoadFamiliesAsync(
        CancellationToken cancellationToken)
    {
        var result =
            await _getActiveFamiliesHandler.HandleAsync(
                new GetActiveFamiliesQuery(),
                cancellationToken);

        Families.Clear();

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Famílias");

            return;
        }

        foreach (var family in result.Value)
        {
            Families.Add(family);
        }
    }

    private async Task LoadAvailableUsersAsync(
        CancellationToken cancellationToken)
    {
        var result =
            await _getUsersWithoutFamilyHandler.HandleAsync(
                new GetUsersWithoutFamilyQuery(),
                cancellationToken);

        AllAvailableUsers.Clear();

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Utilizadores");

            return;
        }

        foreach (var user in result.Value)
        {
            AllAvailableUsers.Add(user);
        }
    }

    private async Task LoadSelectedFamilyMembersSafeAsync()
    {
        try
        {
            await LoadSelectedFamilyMembersAsync(
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync(
                ex.Message,
                "Não foi possível carregar os membros");
        }
    }

    private async Task LoadSelectedFamilyMembersAsync(
        CancellationToken cancellationToken)
    {
        SelectedFamilyMembers.Clear();
        SelectedUserToRemove = null;

        if (SelectedFamily is null)
        {
            return;
        }

        var familyId = SelectedFamily.Id;

        var result =
            await _getFamilyMembersHandler.HandleAsync(
                new GetFamilyMembersQuery(familyId),
                cancellationToken);

        // A seleção pode ter mudado enquanto aguardávamos.
        if (SelectedFamily?.Id != familyId)
        {
            return;
        }

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Membros da família");

            return;
        }

        foreach (var member in result.Value)
        {
            SelectedFamilyMembers.Add(member);
        }
    }

    private bool CanAddFamily()
    {
        return !IsBusy &&
               _currentUserService.IsAuthenticated &&
               SelectedFamily is null &&
               !string.IsNullOrWhiteSpace(NewFamilyName) &&
               NewFamilyName.Trim().Length >= 3;
    }

    private async Task AddFamilyAsync()
    {
        var userId =
            _currentUserService.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            await NotificationService.ShowWarningAsync(
                "Não existe um utilizador autenticado.",
                "Família");

            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _createFamilyHandler.HandleAsync(
                    new CreateFamilyCommand(
                        NewFamilyName.Trim(),
                        userId.Value));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível criar a família");

                return;
            }

            NewFamilyName = string.Empty;

            await LoadFamiliesAsync(
                CancellationToken.None);

            SelectedFamily =
                Families.FirstOrDefault(
                    family =>
                        family.Id == result.Value.Id);

            await LoadAvailableUsersAsync(
                CancellationToken.None);

            await NotificationService.ShowInformationAsync(
                "Família criada com sucesso.",
                "Família");
        });

        RefreshFamilyCommandsCanExecute();
    }

    private bool CanUpdateFamily()
    {
        if (IsBusy ||
            SelectedFamily is null ||
            string.IsNullOrWhiteSpace(EditFamilyName))
        {
            return false;
        }

        var name =
            EditFamilyName.Trim();

        return name.Length >= 3 &&
               !SelectedFamily.Name.Equals(
                   name,
                   StringComparison.OrdinalIgnoreCase);
    }

    private async Task UpdateFamilyAsync()
    {
        if (SelectedFamily is null)
        {
            return;
        }

        var familyId =
            SelectedFamily.Id;

        await RunSafeAsync(async () =>
        {
            var result =
                await _renameFamilyHandler.HandleAsync(
                    new RenameFamilyCommand(
                        familyId,
                        EditFamilyName.Trim()));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível atualizar a família");

                return;
            }

            ReplaceFamily(result.Value);

            SelectedFamily = result.Value;

            await NotificationService.ShowInformationAsync(
                "Nome da família atualizado.",
                "Família");
        });

        RefreshFamilyCommandsCanExecute();
    }

    private bool CanAddUserToFamily()
    {
        return !IsBusy &&
               SelectedFamily is not null &&
               SelectedFamily.IsActive &&
               SelectedUserToAdd is not null;
    }

    private async Task AddUserToFamilyAsync()
    {
        if (SelectedFamily is null ||
            SelectedUserToAdd is null)
        {
            return;
        }

        var familyId =
            SelectedFamily.Id;

        var userId =
            SelectedUserToAdd.Id;

        await RunSafeAsync(async () =>
        {
            var result =
                await _addFamilyMemberHandler.HandleAsync(
                    new AddFamilyMemberCommand(
                        familyId,
                        userId));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível adicionar o membro");

                return;
            }

            SelectedUserToAdd = null;

            await LoadSelectedFamilyMembersAsync(
                CancellationToken.None);

            await LoadAvailableUsersAsync(
                CancellationToken.None);

            await RefreshSelectedFamilyAsync(
                familyId);

            await NotificationService.ShowInformationAsync(
                "Utilizador adicionado à família.",
                "Família");
        });

        RefreshFamilyCommandsCanExecute();
    }

    private bool CanRemoveUserFromFamily()
    {
        return !IsBusy &&
               SelectedFamily is not null &&
               SelectedUserToRemove is not null &&
               SelectedUserToRemove.Id !=
                   _currentUserService.UserId &&
               SelectedUserToRemove.Id !=
                   SelectedFamily.CreatorUserId;
    }

    private async Task RemoveUserFromFamilyAsync()
    {
        if (SelectedFamily is null ||
            SelectedUserToRemove is null)
        {
            return;
        }

        var familyId =
            SelectedFamily.Id;

        var member =
            SelectedUserToRemove;

        if (member.Id ==
            _currentUserService.UserId)
        {
            await NotificationService.ShowWarningAsync(
                "Não pode remover o seu próprio acesso à família.",
                "Família");

            return;
        }

        if (member.Id ==
            SelectedFamily.CreatorUserId)
        {
            await NotificationService.ShowWarningAsync(
                "O criador da família não pode ser removido.",
                "Família");

            return;
        }

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Pretende remover '{member.UserName}' da família?",
                "Remover membro");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _removeFamilyMemberHandler.HandleAsync(
                    new RemoveFamilyMemberCommand(
                        familyId,
                        member.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível remover o membro");

                return;
            }

            SelectedUserToRemove = null;

            await LoadSelectedFamilyMembersAsync(
                CancellationToken.None);

            await LoadAvailableUsersAsync(
                CancellationToken.None);

            await RefreshSelectedFamilyAsync(
                familyId);

            await NotificationService.ShowInformationAsync(
                "Membro removido da família.",
                "Família");
        });

        RefreshFamilyCommandsCanExecute();
    }

    private bool CanDeactivateFamily()
    {
        return !IsBusy &&
               SelectedFamily is not null &&
               SelectedFamily.IsActive;
    }

    private async Task DeactivateFamilyAsync()
    {
        if (SelectedFamily is null)
        {
            return;
        }

        var family =
            SelectedFamily;

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Pretende desativar a família '{family.Name}'? " +
                "Os membros serão removidos da família.",
                "Desativar família");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _deactivateFamilyHandler.HandleAsync(
                    new DeactivateFamilyCommand(
                        family.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível desativar a família");

                return;
            }

            SelectedFamily = null;

            await LoadFamiliesAsync(
                CancellationToken.None);

            await LoadAvailableUsersAsync(
                CancellationToken.None);

            await NotificationService.ShowInformationAsync(
                "Família desativada com sucesso.",
                "Família");
        });

        RefreshFamilyCommandsCanExecute();
    }

    private bool CanRegenerateInvitationCode()
    {
        return !IsBusy &&
               SelectedFamily is not null &&
               SelectedFamily.IsActive;
    }

    private async Task RegenerateInvitationCodeAsync()
    {
        if (SelectedFamily is null)
        {
            return;
        }

        var familyId =
            SelectedFamily.Id;

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                "Pretende gerar um novo código de convite? " +
                "O código atual deixará de ser válido.",
                "Novo código de convite");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _regenerateInvitationCodeHandler
                    .HandleAsync(
                        new RegenerateInvitationCodeCommand(
                            familyId));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Não foi possível gerar um novo código");

                return;
            }

            await RefreshSelectedFamilyAsync(
                familyId);

            await NotificationService.ShowInformationAsync(
                $"Novo código de convite: {result.Value}",
                "Código de convite");
        });
    }

    private async Task RefreshSelectedFamilyAsync(
    Guid familyId)
    {
        var result =
            await _getFamilyByIdHandler.HandleAsync(
                new GetFamilyByIdQuery(familyId),
                CancellationToken.None);

        if (result.IsFailure)
        {
            return;
        }

        ReplaceFamily(result.Value);

        SelectedFamily = result.Value;
    }

    private void ExecuteNewFamily()
    {
        SelectedFamily = null;

        NewFamilyName = string.Empty;
        EditFamilyName = string.Empty;

        SelectedUserToAdd = null;
        SelectedUserToRemove = null;

        SelectedFamilyMembers.Clear();

        UpdateFormState();

        RefreshFamilyCommandsCanExecute();
    }

    private void ReplaceFamily(
        FamilyDTO updatedFamily)
    {
        var existing =
            Families.FirstOrDefault(
                family =>
                    family.Id == updatedFamily.Id);

        if (existing is null)
        {
            return;
        }

        var index =
            Families.IndexOf(existing);

        Families[index] =
            updatedFamily;
    }

    private void UpdateFormState()
    {
        FormTitle =
            SelectedFamily is null
                ? "Criar Nova Família"
                : $"Editar Família: {SelectedFamily.Name}";
    }

    private void RefreshFamilyCommandsCanExecute()
    {
        AddFamilyCommand.RaiseCanExecuteChanged();
        UpdateFamilyCommand.RaiseCanExecuteChanged();
        AddUserToFamilyCommand.RaiseCanExecuteChanged();
        RemoveUserFromFamilyCommand.RaiseCanExecuteChanged();
        DeactivateFamilyCommand.RaiseCanExecuteChanged();
        RegenerateInvitationCodeCommand
            .RaiseCanExecuteChanged();
    }
}