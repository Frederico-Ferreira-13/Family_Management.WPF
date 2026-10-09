namespace FamilyManagement.Application.UseCases.Families.DTOs;

public sealed record FamilyMemberDTO(
    Guid Id,
    string UserName,
    string Email,
    bool IsAdmin,
    bool IsActive,
    DateTime CreatedAt);