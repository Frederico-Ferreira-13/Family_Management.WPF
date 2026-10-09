using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Users.Commands.CreateUser;

public sealed class CreateUserHandler
{
    private const int MinimumPasswordLength = 8;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<UserDTO>> HandleAsync(
        CreateUserCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserName))
        {
            return Error.Validation(
                "O nome de utilizador é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Error.Validation(
                "O email é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Error.Validation(
                "A palavra-passe é obrigatória.");
        }

        if (command.Password.Length < MinimumPasswordLength)
        {
            return Error.Validation(
                $"A palavra-passe deve ter pelo menos " +
                $"{MinimumPasswordLength} caracteres.");
        }

        var userName = command.UserName.Trim();
        var email = command.Email.Trim();

        if (await _unitOfWork.Users
                .ExistsByUserNameAsync(userName))
        {
            return Error.Conflict(
                "User.DuplicateName",
                "Este nome de utilizador já está em uso.");
        }

        if (await _unitOfWork.Users
                .ExistsByEmailAsync(email))
        {
            return Error.Conflict(
                "User.DuplicateEmail",
                "Este email já se encontra registado.");
        }

        var passwordHash =
            _passwordHasher.HashPassword(
                command.Password);

        var result = User.Create(
            userName,
            email,
            passwordHash);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var user = result.Value;

        await _unitOfWork.Users.AddAsync(user);

        await _unitOfWork.CompleteAsync();

        return UserMapper.ToDTO(user);
    }
}