using FamilyManagement.Application.Common.Models;
using FamilyManagement.Application.UseCases.Users.DTOs;

namespace FamilyManagement.Application.Common.Interfaces;

public interface ITokenService
{
    TokenResponse GenerateToken(UserDTO user);
}