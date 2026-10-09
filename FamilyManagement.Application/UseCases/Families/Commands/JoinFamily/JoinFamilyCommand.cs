namespace FamilyManagement.Application.UseCases.Families.Commands.JoinFamily;

public sealed record JoinFamilyCommand(
    string InvitationCode,
    Guid UserId);