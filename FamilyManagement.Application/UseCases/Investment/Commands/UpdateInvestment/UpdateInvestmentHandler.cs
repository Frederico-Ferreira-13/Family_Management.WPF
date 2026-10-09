using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Investments.Commands.UpdateInvestment;

public sealed class UpdateInvestmentHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateInvestmentHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InvestmentDTO>> HandleAsync(
        UpdateInvestmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvestmentId == Guid.Empty)
            return Error.Validation("O identificador do investimento é obrigatório.");

        if (string.IsNullOrWhiteSpace(command.Name))
            return Error.Validation("O nome do investimento é obrigatório.");

        if (command.AccountId == Guid.Empty)
            return Error.Validation("O identificador da conta é inválido.");

        var name = command.Name.Trim();

        var investment = await _unitOfWork.Investments
            .GetByIdAsync(command.InvestmentId);

        if (investment is null)
        {
            return Error.NotFound(
                "Investment.NotFound",
                "Investimento não encontrado.");
        }

        if (!investment.Name.Equals(
                name,
                StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _unitOfWork.Investments
                .InvestmentExistsForUserByNameAndIdAsync(
                    name,
                    investment.UserId,
                    investment.Id);

            if (exists)
            {
                return Error.Conflict(
                    "Investment.Duplicate",
                    "Já existe outro investimento com este nome.");
            }
        }

        if (command.AccountId.HasValue)
        {
            var account = await _unitOfWork.Accounts
                .GetByIdAsync(command.AccountId.Value);

            if (account is null ||
                !account.IsActive ||
                account.UserId != investment.UserId)
            {
                return Error.Validation(
                    "A conta selecionada é inválida ou não pertence ao utilizador.");
            }

            if (!string.Equals(
                    account.Balance.Currency,
                    investment.InitialValue.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Error.BusinessRule(
                    "Investment.AccountCurrencyMismatch",
                    "A moeda da conta não corresponde à moeda do investimento.");
            }
        }

        var updateResult = investment.UpdateDetails(
            name,
            command.Type,
            command.AccountId);

        if (updateResult.IsFailure)
            return updateResult.Error;

        _unitOfWork.Investments.Update(investment);

        await _unitOfWork.CompleteAsync();

        return InvestmentMapper.ToDTO(investment);
    }
}