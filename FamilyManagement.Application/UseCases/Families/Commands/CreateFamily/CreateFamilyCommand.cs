namespace FamilyManagement.Application.UseCases.Families.Commands.CreateFamily;

public sealed record CreateFamilyCommand(
    string Name,
    Guid CreatorUserId);