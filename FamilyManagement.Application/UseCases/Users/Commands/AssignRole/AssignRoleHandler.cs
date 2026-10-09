using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Commands.AssignRole;

public sealed class AssignRoleHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public AssignRoleHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        AssignRoleCommand command)
    {
        var user = await _unitOfWork.Users
            .GetUserByIdWithFamilyAsync(command.UserId);

        if (user is null)
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");

        var role = await _unitOfWork.UserRoles
            .GetByIdAsync(command.RoleId);

        if (role is null || !role.IsActive)
            return Error.NotFound(
                "Role.NotFound",
                "Papel não encontrado.");

        var result = user.AssignRole(role);

        if (result.IsFailure)
            return result.Error;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}