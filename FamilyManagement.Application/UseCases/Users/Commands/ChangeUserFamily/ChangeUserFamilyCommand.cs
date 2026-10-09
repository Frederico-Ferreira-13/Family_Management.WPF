namespace FamilyManagement.Application.UseCases.Users.Commands.ChangeUserFamily;

public sealed record ChangeUserFamilyCommand(
    Guid UserId,
    Guid FamilyId);