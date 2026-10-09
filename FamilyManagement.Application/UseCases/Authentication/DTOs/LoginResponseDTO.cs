using FamilyManagement.Application.UseCases.Users.DTOs;

namespace FamilyManagement.Application.UseCases.Authentication.DTOs;

public sealed record LoginResponseDTO(
    UserDTO User,
    string Token,
    DateTime ExpiresAt);