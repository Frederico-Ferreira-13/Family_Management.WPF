
using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetByCategoryAndPeriod;

public sealed class GetBudgetByCategoryAndPeriodHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBudgetByCategoryAndPeriodHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<BudgetDTO>> HandleAsync(
        GetBudgetByCategoryAndPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CategoryId == Guid.Empty)
        {
            return Error.Validation(
                "A categoria é obrigatória.");
        }

        if (query.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
        }

        if (query.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é inválido.");
        }

        if (query.Month is < 1 or > 12)
        {
            return Error.Validation(
                "O mês deve estar entre 1 e 12.");
        }

        if (query.Year is < 2000 or > 9999)
        {
            return Error.Validation(
                "O ano é inválido.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(query.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        if (query.FamilyId.HasValue)
        {
            if (user.FamilyId != query.FamilyId)
            {
                return Error.BusinessRule(
                    "Budget.FamilyAccessDenied",
                    "O utilizador não pertence à família indicada.");
            }

            var family = await _unitOfWork.Families
                .GetByIdAsync(query.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.NotFound(
                    "Family.NotFound",
                    "Família não encontrada ou inativa.");
            }
        }

        var budget = query.FamilyId.HasValue
            ? await _unitOfWork.Budgets
                .GetBudgetByFamilyCategoryAndPeriodAsync(
                    query.FamilyId.Value,
                    query.CategoryId,
                    query.Month,
                    query.Year)
            : await _unitOfWork.Budgets
                .GetBudgetsByCategoryIdAndUserIdAndMonthYearAsync(
                    query.CategoryId,
                    query.UserId,
                    query.Month,
                    query.Year);

        if (budget is null)
        {
            return Error.NotFound(
                "Budget.NotFound",
                "Orçamento não encontrado.");
        }

        return BudgetMapper.ToDTO(budget);
    }
}
