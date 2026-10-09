namespace FamilyManagement.Application.UseCases.Users.Commands.RemoveUserFromFamily;

public sealed record RemoveUserFromFamilyCommand(
    Guid UserId);