namespace FamilyManagement.Application.UseCases.Users.Commands.AssignUserToFamily;

public sealed record AssignUserToFamilyCommand(
    Guid UserId,
    Guid FamilyId);