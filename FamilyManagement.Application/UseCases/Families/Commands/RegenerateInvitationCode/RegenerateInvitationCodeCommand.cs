namespace FamilyManagement.Application.UseCases.Families.Commands.RegenerateInvitationCode;

public sealed record RegenerateInvitationCodeCommand(
    Guid FamilyId);