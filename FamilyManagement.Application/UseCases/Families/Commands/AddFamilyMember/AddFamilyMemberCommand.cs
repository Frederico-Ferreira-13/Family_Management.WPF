namespace FamilyManagement.Application.UseCases.Families.Commands.AddFamilyMember;

public sealed record AddFamilyMemberCommand(
    Guid FamilyId,
    Guid UserId);