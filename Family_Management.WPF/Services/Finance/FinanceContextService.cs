using Family_Management.WPF.Services.Authentication;
using FamilyManagement.Application.UseCases.Users.Queries.GetUserById;

namespace Family_Management.WPF.Services.Finance;

public sealed class FinanceContextService : IDisposable
{
    private readonly CurrentUserService _currentUserService;
    private readonly GetUserByIdHandler _getUserByIdHandler;

    public FinanceContextService(
        CurrentUserService currentUserService,
        GetUserByIdHandler getUserByIdHandler)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _getUserByIdHandler = getUserByIdHandler
            ?? throw new ArgumentNullException(
                nameof(getUserByIdHandler));

        _currentUserService.SessionChanged += OnSessionChanged;
    }

    public event Action? ContextChanged;

    public FinanceScope Scope { get; private set; }
        = FinanceScope.Personal;

    public Guid? UserId { get; private set; }

    public Guid? FamilyId { get; private set; }

    public bool CanUseFamilyScope =>
        UserId.HasValue &&
        FamilyId.HasValue;

    public Guid? ActiveFamilyId =>
        Scope == FinanceScope.Family
            ? FamilyId
            : null;

    public async Task<bool> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        var sessionUserId = _currentUserService.UserId;

        if (!_currentUserService.IsAuthenticated ||
            !sessionUserId.HasValue ||
            sessionUserId.Value == Guid.Empty)
        {
            Reset();
            return false;
        }

        var result =
            await _getUserByIdHandler.HandleAsync(
                new GetUserByIdQuery(
                    sessionUserId.Value),
                cancellationToken);

        if (result.IsFailure ||
            !result.Value.IsActive)
        {
            Reset();
            return false;
        }

        UserId = result.Value.Id;
        FamilyId = result.Value.FamilyId;

        // Se o utilizador não tiver família,
        // o contexto regressa ao modo pessoal.
        if (!FamilyId.HasValue)
        {
            Scope = FinanceScope.Personal;
        }

        ContextChanged?.Invoke();

        return true;
    }

    public bool TrySelectScope(FinanceScope scope)
    {
        if (!UserId.HasValue)
        {
            return false;
        }

        if (scope != FinanceScope.Personal &&
            scope != FinanceScope.Family)
        {
            return false;
        }

        if (scope == FinanceScope.Family &&
            !CanUseFamilyScope)
        {
            return false;
        }

        if (Scope == scope)
        {
            return true;
        }

        Scope = scope;

        ContextChanged?.Invoke();

        return true;
    }

    private void OnSessionChanged()
    {
        Reset();
    }

    private void Reset()
    {
        Scope = FinanceScope.Personal;

        UserId = null;
        FamilyId = null;

        ContextChanged?.Invoke();
    }

    public void Dispose()
    {
        _currentUserService.SessionChanged -=
            OnSessionChanged;
    }
}