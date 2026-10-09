using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Application.UseCases.Transactions.Services;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Transactions.Commands.CreateTransfer;

public sealed class CreateTransferHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionImpactService _impactService;

    public CreateTransferHandler(
        IUnitOfWork unitOfWork,
        ITransactionImpactService impactService)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _impactService = impactService
            ?? throw new ArgumentNullException(nameof(impactService));
    }

    public async Task<Result<TransactionDTO>> HandleAsync(
        CreateTransferCommand command)
    {
        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
        }

        if (command.SourceAccountId == Guid.Empty)
        {
            return Error.Validation(
                "A conta de origem é obrigatória.");
        }

        if (command.TargetAccountId == Guid.Empty)
        {
            return Error.Validation(
                "A conta de destino é obrigatória.");
        }

        if (command.SourceAccountId ==
            command.TargetAccountId)
        {
            return Error.Validation(
                "As contas de origem e destino devem ser diferentes.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        var source = await _unitOfWork.Accounts
            .GetByIdAsync(command.SourceAccountId);

        if (source is null || !source.IsActive)
        {
            return Error.NotFound(
                "Account.SourceNotFound",
                "Conta de origem não encontrada.");
        }

        var target = await _unitOfWork.Accounts
            .GetByIdAsync(command.TargetAccountId);

        if (target is null || !target.IsActive)
        {
            return Error.NotFound(
                "Account.TargetNotFound",
                "Conta de destino não encontrada.");
        }

        if (source.UserId != command.UserId ||
            target.UserId != command.UserId)
        {
            return Error.BusinessRule(
                "Transaction.AccountOwnerMismatch",
                "As contas devem pertencer ao utilizador.");
        }

        var moneyResult = Money.TryCreate(
            command.Amount,
            command.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var money = moneyResult.Value;

        if (!string.Equals(
                source.Balance.Currency,
                money.Currency,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                target.Balance.Currency,
                money.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        var result = Transaction.CreateTransfer(
            command.Date,
            money,
            command.Description,
            command.UserId,
            command.SourceAccountId,
            command.TargetAccountId,
            command.Notes,
            command.IsConfirmed);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var transaction = result.Value;

        using var dbTransaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (transaction.IsConfirmed)
            {
                var impact =
                    await _impactService.ApplyAsync(
                        transaction);

                if (impact.IsFailure)
                {
                    await dbTransaction.RollbackAsync();

                    return impact.Error;
                }
            }

            await _unitOfWork.Transactions
                .AddAsync(transaction);

            await _unitOfWork.CompleteAsync();

            await dbTransaction.CommitAsync();

            return TransactionMapper.ToDTO(
                transaction);
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}