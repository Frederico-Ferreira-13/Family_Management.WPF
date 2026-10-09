namespace FamilyManagement.Application.UseCases.Users.Commands.RemoveRole;

public sealed record RemoveRoleCommand(
    Guid UserId,
    Guid RoleId);