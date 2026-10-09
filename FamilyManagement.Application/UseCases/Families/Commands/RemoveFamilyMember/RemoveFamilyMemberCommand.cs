namespace FamilyManagement.Application.UseCases.Families.Commands.RemoveFamilyMember;

public sealed record RemoveFamilyMemberCommand(
    Guid FamilyId,
    Guid UserId);