using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Commands.RemoveRole;

public sealed class RemoveRoleHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveRoleHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        RemoveRoleCommand command)
    {
        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null)
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");

        var result = user.RemoveRole(command.RoleId);

        if (result.IsFailure)
            return result.Error;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}