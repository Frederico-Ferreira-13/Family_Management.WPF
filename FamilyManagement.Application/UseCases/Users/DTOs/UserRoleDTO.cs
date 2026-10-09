namespace FamilyManagement.Application.UseCases.Users.DTOs;

public sealed record UserRoleDTO(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record RoleLookupDTO(
    Guid Id,
    string Name);