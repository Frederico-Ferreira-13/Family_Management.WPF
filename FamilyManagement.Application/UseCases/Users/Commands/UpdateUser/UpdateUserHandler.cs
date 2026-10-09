using FamilyManagement.Application.UseCases.Users.DTOs;
using FamilyManagement.Application.UseCases.Users.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Commands.UpdateUser;

public sealed class UpdateUserHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result<UserDTO>> HandleAsync(
        UpdateUserCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

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

        var userName =
            command.UserName.Trim();

        var email =
            command.Email.Trim();

        var user =
            await _unitOfWork.Users
                .GetUserByIdWithFamilyAsync(
                    command.UserId);

        if (user is null)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        var existingByName =
            await _unitOfWork.Users
                .GetUserByUserNameAsync(
                    userName);

        if (existingByName is not null &&
            existingByName.Id != user.Id)
        {
            return Error.Conflict(
                "User.DuplicateName",
                "Este nome de utilizador já está em uso.");
        }

        var existingByEmail =
            await _unitOfWork.Users
                .GetUserByEmailAsync(
                    email);

        if (existingByEmail is not null &&
            existingByEmail.Id != user.Id)
        {
            return Error.Conflict(
                "User.DuplicateEmail",
                "Este email já se encontra registado.");
        }

        var result =
            user.UpdateProfile(
                userName,
                email);

        if (result.IsFailure)
        {
            return result.Error;
        }

        _unitOfWork.Users.Update(user);

        await _unitOfWork.CompleteAsync();

        return UserMapper.ToDTO(user);
    }
}