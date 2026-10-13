using FamilyManagement.Application.UseCases.Transactions.Services;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Transactions.Commands.DeactivateTransaction;

public sealed class DeactivateTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionImpactService _impactService;

    public DeactivateTransactionHandler(
        IUnitOfWork unitOfWork,
        ITransactionImpactService impactService)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _impactService = impactService
            ?? throw new ArgumentNullException(nameof(impactService));
    }

    public async Task<Result> HandleAsync(
        DeactivateTransactionCommand command)
    {
        if (command.TransactionId == Guid.Empty)
            return Error.Validation("A transação é obrigatória.");

        if (command.UserId == Guid.Empty)
            return Error.Validation("O utilizador é obrigatório.");

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        var transaction = await _unitOfWork.Transactions
            .GetByIdAsync(command.TransactionId);

        if (transaction is null)
        {
            return Error.NotFound(
                "Transaction.NotFound",
                "Transação não encontrada.");
        }

        if (transaction.FamilyId.HasValue)
        {
            if (user.FamilyId != transaction.FamilyId)
            {
                return Error.BusinessRule(
                    "Transaction.AccessDenied",
                    "Não tem autorização para desativar esta transação.");
            }

            var family = await _unitOfWork.Families
                .GetByIdAsync(transaction.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.BusinessRule(
                    "Transaction.FamilyUnavailable",
                    "A família não existe ou está inativa.");
            }
        }
        else if (transaction.UserId != command.UserId)
        {
            return Error.BusinessRule(
                "Transaction.AccessDenied",
                "Não tem autorização para desativar esta transação.");
        }

        // Evita reverter duas vezes os mesmos impactos.
        if (!transaction.IsActive)
            return Result.Success();

        using var dbTransaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (transaction.IsConfirmed)
            {
                var revertResult =
                    await _impactService.RevertAsync(transaction);

                if (revertResult.IsFailure)
                {
                    await RollbackAndClearAsync(dbTransaction);
                    return revertResult.Error;
                }
            }

            transaction.Deactivate();

            _unitOfWork.Transactions.Update(transaction);

            await _unitOfWork.CompleteAsync();
            await dbTransaction.CommitAsync();

            return Result.Success();
        }
        catch
        {
            await RollbackAndClearAsync(dbTransaction);
            throw;
        }
    }

    private async Task RollbackAndClearAsync(
        IDbTransaction dbTransaction)
    {
        try
        {
            await dbTransaction.RollbackAsync();
        }
        finally
        {
            // Um rollback de BD não restaura as entidades rastreadas.
            // Este UnitOfWork não deve voltar a usar referências antigas.
            _unitOfWork.ClearTrackedChanges();
        }
    }
}