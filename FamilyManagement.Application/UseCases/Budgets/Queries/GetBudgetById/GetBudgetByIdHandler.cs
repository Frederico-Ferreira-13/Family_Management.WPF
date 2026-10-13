
using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetById;

public sealed class GetBudgetByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBudgetByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<BudgetDTO>> HandleAsync(
        GetBudgetByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.BudgetId == Guid.Empty)
        {
            return Error.Validation(
                "O orçamento é obrigatório.");
        }

        if (query.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(query.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        var budget = await _unitOfWork.Budgets
            .GetBudgetByIdWithDetailsAsync(query.BudgetId);

        if (budget is null)
        {
            return Error.NotFound(
                "Budget.NotFound",
                "Orçamento não encontrado.");
        }

        if (budget.FamilyId.HasValue)
        {
            // Orçamento familiar:
            // qualquer membro ativo da mesma família
            // pode consultar o orçamento.
            if (user.FamilyId != budget.FamilyId)
            {
                return Error.BusinessRule(
                    "Budget.AccessDenied",
                    "Não tem autorização para consultar este orçamento.");
            }

            var family = await _unitOfWork.Families
                .GetByIdAsync(budget.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.BusinessRule(
                    "Budget.FamilyUnavailable",
                    "A família não existe ou está inativa.");
            }
        }
        else
        {
            // Orçamento pessoal:
            // apenas o respetivo proprietário.
            if (budget.UserId != query.UserId)
            {
                return Error.BusinessRule(
                    "Budget.AccessDenied",
                    "Não tem autorização para consultar este orçamento.");
            }
        }

        return BudgetMapper.ToDTO(budget);
    }
}
