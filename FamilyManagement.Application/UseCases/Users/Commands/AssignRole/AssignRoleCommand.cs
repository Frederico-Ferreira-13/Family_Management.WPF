namespace FamilyManagement.Application.UseCases.Users.Commands.AssignRole;

public sealed record AssignRoleCommand(
    Guid UserId,
    Guid RoleId);