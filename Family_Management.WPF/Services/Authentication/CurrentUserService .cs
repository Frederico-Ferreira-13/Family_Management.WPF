using FamilyManagement.Application.Common.Interfaces;

namespace Family_Management.WPF.Services.Authentication;

public sealed class CurrentUserService : ICurrentUserService
{
    public event Action? SessionChanged;

    public Guid? UserId { get; private set; }

    public bool IsAuthenticated =>
        UserId.HasValue &&
        UserId.Value != Guid.Empty;

    public void SetCurrentUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador do utilizador é obrigatório.",
                nameof(userId));
        }

        UserId = userId;

        SessionChanged?.Invoke();
    }

    public void Clear()
    {
        if (!UserId.HasValue)
            return;

        UserId = null;

        SessionChanged?.Invoke();
    }
}