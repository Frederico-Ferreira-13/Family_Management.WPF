namespace FamilyManagement.Application.UseCases.Users.DTOs;

public sealed record UserLookupDTO(
    Guid Id,
    string UserName,
    string Email);