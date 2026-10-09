using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Accounts.Commands.DeactivateAccount;

public sealed class DeactivateAccountHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateAccountHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        DeactivateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var account =
            await _unitOfWork.Accounts.GetByIdAsync(command.AccountId);

        if (account is null)
        {
            return Error.NotFound(
                "Account.NotFound",
                "Conta não encontrada.");
        }

        if (account.Balance.Amount != 0)
        {
            return Error.BusinessRule(
                "Account.HasBalance",
                "A conta deve ter saldo zero antes de ser desativada.");
        }

        account.Deactivate();

        _unitOfWork.Accounts.Update(account);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}