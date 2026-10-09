using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Investments.Commands.CreateInvestment;

public sealed class CreateInvestmentHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateInvestmentHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InvestmentDTO>> HandleAsync(
        CreateInvestmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            return Error.Validation("O utilizador é obrigatório.");

        if (string.IsNullOrWhiteSpace(command.Name))
            return Error.Validation("O nome do investimento é obrigatório.");

        if (string.IsNullOrWhiteSpace(command.Currency))
            return Error.Validation("A moeda do investimento é obrigatória.");

        if (command.AccountId == Guid.Empty)
            return Error.Validation("O identificador da conta é inválido.");

        var name = command.Name.Trim();
        var currency = command.Currency.Trim().ToUpperInvariant();

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        if (command.AccountId.HasValue)
        {
            var account = await _unitOfWork.Accounts
                .GetByIdAsync(command.AccountId.Value);

            if (account is null ||
                !account.IsActive ||
                account.UserId != command.UserId)
            {
                return Error.Validation(
                    "A conta selecionada é inválida ou não pertence ao utilizador.");
            }

            if (!string.Equals(
                    account.Balance.Currency,
                    currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Error.BusinessRule(
                    "Investment.AccountCurrencyMismatch",
                    "A moeda do investimento deve ser igual à moeda da conta associada.");
            }
        }

        var exists = await _unitOfWork.Investments
            .InvestmentExistsForUserByNameAsync(
                name,
                command.UserId);

        if (exists)
        {
            return Error.Conflict(
                "Investment.Duplicate",
                $"Já existe um investimento chamado '{name}' para este utilizador.");
        }

        var moneyResult = Money.TryCreate(
            command.InitialValue,
            currency);

        if (moneyResult.IsFailure)
            return moneyResult.Error;

        var investmentResult = Investment.Create(
            name: name,
            type: command.Type,
            initialValue: moneyResult.Value,
            purchaseDate: command.PurchaseDate,
            userId: command.UserId,
            accountId: command.AccountId);

        if (investmentResult.IsFailure)
            return investmentResult.Error;

        var investment = investmentResult.Value;

        await _unitOfWork.Investments.AddAsync(investment);

        await _unitOfWork.CompleteAsync();

        return InvestmentMapper.ToDTO(investment);
    }
}