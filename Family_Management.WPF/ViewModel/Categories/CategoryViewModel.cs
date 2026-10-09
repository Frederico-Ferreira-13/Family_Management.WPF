using System.Collections.ObjectModel;
using System.Windows.Input;
using Family_Management.WPF.Commands;
using Family_Management.WPF.Services.Authentication;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;
using Family_Management.WPF.ViewModel.Common;
using Family_Management.WPF.ViewModel.Models;
using FamilyManagement.Application.UseCases.Categories.Commands.CreateCategory;
using FamilyManagement.Application.UseCases.Categories.Commands.DeactivateCategory;
using FamilyManagement.Application.UseCases.Categories.Commands.UpdateCategory;
using FamilyManagement.Application.UseCases.Categories.DTOs;
using FamilyManagement.Application.UseCases.Categories.Queries.GetAvailableCategoriesForUser;
using FamilyManagement.Application.UseCases.Categories.Queries.GetParentCategoryLookup;
using FamilyManagement.Application.UseCases.Users.Queries.GetUserById;
using FamilyManagement.Domain.Enums;
using System.ComponentModel;

namespace Family_Management.WPF.ViewModel.Categories;

public sealed class CategoryViewModel : BaseViewModel
{
    private readonly CreateCategoryHandler _createCategoryHandler;
    private readonly UpdateCategoryHandler _updateCategoryHandler;
    private readonly DeactivateCategoryHandler _deactivateCategoryHandler;
    private readonly GetAvailableCategoriesForUserHandler _getCategoriesHandler;
    private readonly GetParentCategoryLookupHandler _getParentCategoriesHandler;
    private readonly GetUserByIdHandler _getUserByIdHandler;
    private readonly CurrentUserService _currentUserService;

    private Guid? _currentFamilyId;

    private CategoryDTO? _selectedCategory;
    private CategoryLookupDTO _selectedParentCategory =
        CategoryLookupDTO.None;

    private CategoryFormModel _form;

    public CategoryViewModel(
        CreateCategoryHandler createCategoryHandler,
        UpdateCategoryHandler updateCategoryHandler,
        DeactivateCategoryHandler deactivateCategoryHandler,
        GetAvailableCategoriesForUserHandler getCategoriesHandler,
        GetParentCategoryLookupHandler getParentCategoriesHandler,
        GetUserByIdHandler getUserByIdHandler,
        CurrentUserService currentUserService,
        INavigationService navigationService,
        INotificationService notificationService)
        : base(navigationService, notificationService)
    {
        _createCategoryHandler = createCategoryHandler
            ?? throw new ArgumentNullException(nameof(createCategoryHandler));

        _updateCategoryHandler = updateCategoryHandler
            ?? throw new ArgumentNullException(nameof(updateCategoryHandler));

        _deactivateCategoryHandler = deactivateCategoryHandler
            ?? throw new ArgumentNullException(nameof(deactivateCategoryHandler));

        _getCategoriesHandler = getCategoriesHandler
            ?? throw new ArgumentNullException(nameof(getCategoriesHandler));

        _getParentCategoriesHandler = getParentCategoriesHandler
            ?? throw new ArgumentNullException(nameof(getParentCategoriesHandler));

        _getUserByIdHandler = getUserByIdHandler
            ?? throw new ArgumentNullException(nameof(getUserByIdHandler));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        Title = "Categorias";

        _form = CategoryFormModel.CreateNew();
            SubscribeToForm(_form);

        SaveCategoryCommand =
            new AsyncRelayCommand(
                SaveCategoryAsync,
                CanSave);

        ClearFormCommand =
            new AsyncRelayCommand(
                ResetFormAsync,
                () => !IsBusy);

        DeactivateCategoryCommand =
            new AsyncRelayCommand(
                DeactivateSelectedCategoryAsync,
                CanDeactivate);

        RefreshCommand =
            new AsyncRelayCommand(
                RefreshAsync,
                () => !IsBusy);
    }

    public ObservableCollection<CategoryDTO> Categories { get; }
        = new();

    public ObservableCollection<CategoryLookupDTO> ParentCategories { get; }
        = new();

    public IReadOnlyList<CategoryType> AvailableCategoryTypes { get; }
        = Enum.GetValues<CategoryType>()
            .Where(type => type != CategoryType.NotSpecified)
            .ToArray();

    public IReadOnlyList<CategoryScope> AvailableScopes { get; }
        = Enum.GetValues<CategoryScope>();

    public CategoryFormModel Form
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
            OnPropertyChanged(nameof(CanSelectFamilyScope));

            RaiseCommandStates();
        }
    }

    public CategoryDTO? SelectedCategory
    {
        get => _selectedCategory;

        set
        {
            if (!SetProperty(ref _selectedCategory, value))
            {
                return;
            }

            _ = HandleSelectedCategoryChangedAsync(value);

            RaiseCommandStates();
        }
    }

    public CategoryLookupDTO SelectedParentCategory
    {
        get => _selectedParentCategory;

        set
        {
            if (!SetProperty(ref _selectedParentCategory, value))
            {
                return;
            }

            Form.ParentCategoryId =
                value.Id == Guid.Empty
                    ? null
                    : value.Id;

            RaiseCommandStates();
        }
    }

    public bool IsEditMode => Form.IsEditMode;

    public bool CanSelectFamilyScope =>
        _currentFamilyId.HasValue &&
        !Form.IsEditMode;

    public ICommand SaveCategoryCommand { get; }

    public ICommand ClearFormCommand { get; }

    public ICommand DeactivateCategoryCommand { get; }

    public ICommand RefreshCommand { get; }

    public override async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
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

        var userResult =
            await _getUserByIdHandler.HandleAsync(
                new GetUserByIdQuery(userId));

        if (userResult.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                userResult.Error.Message,
                "Utilizador");

            return;
        }

        _currentFamilyId =
            userResult.Value.FamilyId;

        await LoadCategoriesAsync(
            cancellationToken);

        await ResetFormAsync();

        IsInitialized = true;
    }

    private async Task RefreshAsync()
    {
        await RunSafeAsync(async () =>
        {
            await LoadCategoriesAsync(
                CancellationToken.None);

            await ResetFormAsync();
        });

        RaiseCommandStates();
    }

    private async Task LoadCategoriesAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return;
        }

        var result =
            await _getCategoriesHandler.HandleAsync(
                new GetAvailableCategoriesForUserQuery(
                    userId),
                cancellationToken);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Categorias");

            return;
        }

        Categories.Clear();

        foreach (var category in result.Value
                     .OrderBy(category => category.Type)
                     .ThenBy(category => category.Name))
        {
            Categories.Add(category);
        }
    }

    private async Task HandleSelectedCategoryChangedAsync(
        CategoryDTO? category)
    {
        if (category is null)
        {
            return;
        }

        if (category.IsGlobal)
        {
            await NotificationService.ShowWarningAsync(
                "As categorias globais não podem ser editadas nesta área.",
                "Categoria global");

            _selectedCategory = null;
            OnPropertyChanged(nameof(SelectedCategory));

            await ResetFormAsync();
            return;
        }

        Form =
            CategoryFormModel.FromCategory(
                category);

        await LoadParentCategoriesAsync(
            CancellationToken.None);

        SelectedParentCategory =
            ParentCategories.FirstOrDefault(
                parent =>
                    parent.Id ==
                    category.ParentCategoryId)
            ?? CategoryLookupDTO.None;

        RaiseCommandStates();
    }

    private async Task ResetFormAsync()
    {
        _selectedCategory = null;
        OnPropertyChanged(nameof(SelectedCategory));

        Form =
            CategoryFormModel.CreateNew();

        if (!_currentFamilyId.HasValue &&
            Form.Scope == CategoryScope.Family)
        {
            Form.Scope =
                CategoryScope.Personal;
        }

        await LoadParentCategoriesAsync(
            CancellationToken.None);

        SelectedParentCategory =
            CategoryLookupDTO.None;

        RaiseCommandStates();
    }

    public async Task ChangeTypeAsync(
        CategoryType type)
    {
        if (Form.IsEditMode &&
            SelectedCategory?.Type == type)
        {
            return;
        }

        Form.Type = type;
        Form.ParentCategoryId = null;

        await LoadParentCategoriesAsync(
            CancellationToken.None);

        SelectedParentCategory =
            CategoryLookupDTO.None;

        RaiseCommandStates();
    }

    public async Task ChangeScopeAsync(
        CategoryScope scope)
    {
        if (Form.IsEditMode)
        {
            return;
        }

        if (scope == CategoryScope.Family &&
            !_currentFamilyId.HasValue)
        {
            await NotificationService.ShowWarningAsync(
                "O utilizador não pertence a nenhuma família.",
                "Categoria familiar");

            Form.Scope =
                CategoryScope.Personal;

            OnPropertyChanged(nameof(Form));

            return;
        }

        Form.Scope = scope;
        Form.ParentCategoryId = null;

        await LoadParentCategoriesAsync(
            CancellationToken.None);

        SelectedParentCategory =
            CategoryLookupDTO.None;

        RaiseCommandStates();
    }

    private async Task LoadParentCategoriesAsync(
        CancellationToken cancellationToken)
    {
        ParentCategories.Clear();
        ParentCategories.Add(
            CategoryLookupDTO.None);

        if (!TryGetCurrentUserId(out var userId))
        {
            return;
        }

        var scope = GetScopeIdentifiers(userId);

        var result =
            await _getParentCategoriesHandler.HandleAsync(
                new GetParentCategoryLookupQuery(
                    scope.UserId,
                    scope.FamilyId,
                    Form.Type,
                    Form.CategoryId),
                cancellationToken);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Categorias principais");

            return;
        }

        foreach (var parent in result.Value)
        {
            ParentCategories.Add(parent);
        }
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

        if (Form.Type == CategoryType.NotSpecified)
        {
            return false;
        }

        if (Form.Scope == CategoryScope.Family &&
            !_currentFamilyId.HasValue)
        {
            return false;
        }

        if (Form.CategoryId.HasValue &&
            Form.ParentCategoryId ==
            Form.CategoryId)
        {
            return false;
        }

        return true;
    }

    private async Task SaveCategoryAsync()
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
                await UpdateCategoryAsync();
            }
            else
            {
                await CreateCategoryAsync(
                    userId);
            }
        });

        RaiseCommandStates();
    }

    private async Task CreateCategoryAsync(
        Guid userId)
    {
        var scope =
            GetScopeIdentifiers(userId);

        var command =
            new CreateCategoryCommand(
                Name: Form.Name.Trim(),
                Description: NormalizeDescription(
                    Form.Description),
                Type: Form.Type,
                UserId: scope.UserId,
                FamilyId: scope.FamilyId,
                ParentCategoryId:
                    Form.ParentCategoryId);

        var result =
            await _createCategoryHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Criar categoria");

            return;
        }

        await LoadCategoriesAsync(
            CancellationToken.None);

        await ResetFormAsync();

        await NotificationService.ShowInformationAsync(
            "Categoria criada com sucesso.",
            "Sucesso");
    }

    private async Task UpdateCategoryAsync()
    {
        if (!Form.CategoryId.HasValue)
        {
            return;
        }

        var command =
            new UpdateCategoryCommand(
                CategoryId:
                    Form.CategoryId.Value,
                Name:
                    Form.Name.Trim(),
                Description:
                    NormalizeDescription(
                        Form.Description),
                Type:
                    Form.Type,
                ParentCategoryId:
                    Form.ParentCategoryId);

        var result =
            await _updateCategoryHandler.HandleAsync(
                command);

        if (result.IsFailure)
        {
            await NotificationService.ShowErrorAsync(
                result.Error.Message,
                "Atualizar categoria");

            return;
        }

        await LoadCategoriesAsync(
            CancellationToken.None);

        await ResetFormAsync();

        await NotificationService.ShowInformationAsync(
            "Categoria atualizada com sucesso.",
            "Sucesso");
    }

    private bool CanDeactivate()
    {
        return !IsBusy &&
               SelectedCategory is
               {
                   IsActive: true,
                   IsGlobal: false
               };
    }

    private async Task DeactivateSelectedCategoryAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var category =
            SelectedCategory;

        var confirmed =
            await NotificationService.ShowConfirmationAsync(
                $"Deseja desativar a categoria '{category.Name}'?",
                "Confirmar");

        if (!confirmed)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var result =
                await _deactivateCategoryHandler.HandleAsync(
                    new DeactivateCategoryCommand(
                        category.Id));

            if (result.IsFailure)
            {
                await NotificationService.ShowErrorAsync(
                    result.Error.Message,
                    "Desativar categoria");

                return;
            }

            await LoadCategoriesAsync(
                CancellationToken.None);

            await ResetFormAsync();

            await NotificationService.ShowInformationAsync(
                "Categoria desativada com sucesso.",
                "Sucesso");
        });

        RaiseCommandStates();
    }

    private (
        Guid? UserId,
        Guid? FamilyId)
        GetScopeIdentifiers(
            Guid currentUserId)
    {
        return Form.Scope switch
        {
            CategoryScope.Personal =>
                (currentUserId, null),

            CategoryScope.Family
                when _currentFamilyId.HasValue =>
                (null, _currentFamilyId.Value),

            _ =>
                (currentUserId, null)
        };
    }

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        userId =
            _currentUserService.UserId
            ?? Guid.Empty;

        return userId != Guid.Empty;
    }

    private void SubscribeToForm(
    CategoryFormModel form)
{
    form.PropertyChanged += OnFormPropertyChanged;
}

    private void UnsubscribeFromForm(
        CategoryFormModel form)
    {
        form.PropertyChanged -= OnFormPropertyChanged;
    }

    private void OnFormPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CategoryFormModel.CategoryId))
        {
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(CanSelectFamilyScope));
        }

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        if (SaveCategoryCommand
            is AsyncRelayCommand save)
        {
            save.RaiseCanExecuteChanged();
        }

        if (ClearFormCommand
            is AsyncRelayCommand clear)
        {
            clear.RaiseCanExecuteChanged();
        }

        if (DeactivateCategoryCommand
            is AsyncRelayCommand deactivate)
        {
            deactivate.RaiseCanExecuteChanged();
        }

        if (RefreshCommand
            is AsyncRelayCommand refresh)
        {
            refresh.RaiseCanExecuteChanged();
        }
    }

    private static string? NormalizeDescription(
        string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    public CategoryType SelectedType
    {
        get => Form.Type;

        set
        {
            if (Form.Type == value)
            {
                return;
            }

            _ = ChangeTypeAsync(value);

            OnPropertyChanged();
        }
    }

    public CategoryScope SelectedScope
    {
        get => Form.Scope;

        set
        {
            if (Form.Scope == value)
            {
                return;
            }

            _ = ChangeScopeAsync(value);

            OnPropertyChanged();
        }
    }
}