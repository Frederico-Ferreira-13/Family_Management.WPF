namespace FamilyManagement.Application.Common.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }
}