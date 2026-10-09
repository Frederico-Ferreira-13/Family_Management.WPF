using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Users.Commands.ChangeUserFamily;

public sealed class ChangeUserFamilyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public ChangeUserFamilyHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result> HandleAsync(
        ChangeUserFamilyCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        if (command.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        var user =
            await _unitOfWork.Users.GetByIdAsync(
                command.UserId);

        if (user is null)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        if (!user.IsActive)
        {
            return Error.Conflict(
                "User.Inactive",
                "O utilizador está inativo.");
        }

        var family =
            await _unitOfWork.Families.GetByIdAsync(
                command.FamilyId);

        if (family is null || !family.IsActive)
        {
            return Error.NotFound(
                "Family.NotFound",
                "Família não encontrada.");
        }

        if (user.FamilyId == command.FamilyId)
        {
            return Result.Success();
        }

        using var dbTransaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            var removeResult =
                user.RemoveFromFamily();

            if (removeResult.IsFailure)
            {
                await dbTransaction.RollbackAsync();
                return removeResult.Error;
            }

            var assignResult =
                user.AssignToFamily(
                    command.FamilyId);

            if (assignResult.IsFailure)
            {
                await dbTransaction.RollbackAsync();
                return assignResult.Error;
            }

            _unitOfWork.Users.Update(user);

            await _unitOfWork.CompleteAsync();

            await dbTransaction.CommitAsync();

            return Result.Success();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}