using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Application.UseCases.Authentication.DTOs;
using FamilyManagement.Application.UseCases.Users.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Authentication.Commands.Login;

public sealed class LoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResponseDTO>> HandleAsync(
        LoginCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.EmailOrUserName))
        {
            return Error.Validation(
                "O email ou nome de utilizador é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Error.Validation(
                "A palavra-passe é obrigatória.");
        }

        var identifier = command.EmailOrUserName.Trim();

        var user =
            await _userRepository.GetUserByEmailAsync(identifier)
            ?? await _userRepository.GetUserByUserNameAsync(identifier);

        // Não revelamos se foi o utilizador ou a password que falhou.
        if (user is null || !user.IsActive)
        {
            return Error.Validation(
                "Credenciais inválidas.");
        }

        var passwordValid =
            _passwordHasher.VerifyPassword(
                command.Password,
                user.PasswordHash);

        if (!passwordValid)
        {
            return Error.Validation(
                "Credenciais inválidas.");
        }

        var userDTO = UserMapper.ToDTO(user);

        var token = _tokenService.GenerateToken(userDTO);

        return new LoginResponseDTO(
            userDTO,
            token.Token,
            token.Expiration);
    }
}