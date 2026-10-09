namespace FamilyManagement.Application.UseCases.Users.DTOs;

public sealed record UserDTO(
    Guid Id,
    string UserName,
    string Email,
    Guid? FamilyId,
    string? FamilyName,
    bool IsAdmin,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);