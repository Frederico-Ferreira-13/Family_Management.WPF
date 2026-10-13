using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Budgets.Queries.GetBudgetsByUserAndPeriod;

public sealed class GetBudgetsByUserAndPeriodHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBudgetsByUserAndPeriodHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<IReadOnlyCollection<BudgetDTO>>> HandleAsync(
        GetBudgetsByUserAndPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
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

        var budgets = query.FamilyId.HasValue
            ? await _unitOfWork.Budgets
                .GetBudgetsByFamilyAndMonthYearAsync(
                    query.FamilyId.Value,
                    query.Month,
                    query.Year)
            : await _unitOfWork.Budgets
                .GetBudgetsByUserAndMonthYearAsync(
                    query.UserId,
                    query.Month,
                    query.Year);

        IReadOnlyCollection<BudgetDTO> result = budgets
            .Select(BudgetMapper.ToDTO)
            .ToList();

        return Result<IReadOnlyCollection<BudgetDTO>>.Success(result);
    }
}
