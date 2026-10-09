namespace FamilyManagement.Application.UseCases.Families.Commands.RenameFamily;

public sealed record RenameFamilyCommand(
    Guid FamilyId,
    string Name);