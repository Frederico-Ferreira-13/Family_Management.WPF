namespace FamilyManagement.Application.UseCases.Families.DTOs;

public sealed record FamilyDTO(
    Guid Id,
    string Name,
    string InvitationCode,
    Guid CreatorUserId,
    string? CreatorUserName,
    int MemberCount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);