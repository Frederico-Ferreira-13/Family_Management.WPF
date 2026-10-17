using System;
using System.Threading.Tasks;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.FinancialPlans;

public sealed class GetActiveFamilyFinancialPlanHandler
{
    private readonly IFamilyRepository _families;
    private readonly IFinancialPlanRepository _financialPlans;

    public GetActiveFamilyFinancialPlanHandler(
        IFamilyRepository families,
        IFinancialPlanRepository financialPlans)
    {
        _families = families
            ?? throw new ArgumentNullException(nameof(families));

        _financialPlans = financialPlans
            ?? throw new ArgumentNullException(nameof(financialPlans));
    }

    public async Task<Result<FinancialPlan>> HandleAsync(
        Guid familyId,
        Guid userId)
    {
        if (familyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        if (userId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        var isCreator =
            await _families.IsUserCreatorOfFamilyAsync(
                familyId,
                userId);

        var isMember =
            await _families.IsUserMemberOfFamilyAsync(
                familyId,
                userId);

        if (!isCreator && !isMember)
        {
            return Error.NotFound(
                "FinancialPlan.NotFound",
                "Plano financeiro familiar não encontrado.");
        }

        var financialPlan =
            await _financialPlans.GetActiveByFamilyIdAsync(
                familyId);

        if (financialPlan is null)
        {
            return Error.NotFound(
                "FinancialPlan.NotFound",
                "A família ainda não possui um plano financeiro ativo.");
        }

        return financialPlan;
    }
}