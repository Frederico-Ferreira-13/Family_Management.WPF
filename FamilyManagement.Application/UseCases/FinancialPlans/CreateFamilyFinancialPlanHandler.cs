using System;
using System.Threading.Tasks;

using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.FinancialPlans;

public sealed class CreateFamilyFinancialPlanHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateFamilyFinancialPlanHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<FinancialPlan>> HandleAsync(
        CreateFamilyFinancialPlanCommand command)
    {
        if (command is null)
        {
            return Error.Validation(
                "Os dados do plano financeiro são obrigatórios.");
        }

        if (command.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é obrigatório.");
        }

        if (command.CreatedByUserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do utilizador é obrigatório.");
        }

        // 1. Verificar se o utilizador é o criador da família.
        var isCreator = await _unitOfWork.Families
            .IsUserCreatorOfFamilyAsync(
                command.FamilyId,
                command.CreatedByUserId);

        if (!isCreator)
        {
            return Error.BusinessRule(
                "FinancialPlan.NotAuthorized",
                "Apenas o criador da família pode criar " +
                "o respetivo plano financeiro.");
        }

        // 2. Impedir a criação de outro plano ativo.
        var existingPlan = await _unitOfWork.FinancialPlans
            .GetActiveByFamilyIdAsync(command.FamilyId);

        if (existingPlan is not null)
        {
            return Error.Conflict(
                "FinancialPlan.AlreadyExists",
                "Esta família já possui um plano financeiro ativo.");
        }

        // 3. Criar a entidade através das regras do Domain.
        var planResult = FinancialPlan.Create(
            name: command.Name,
            currencyCode: command.CurrencyCode,
            familyId: command.FamilyId,
            createdByUserId: command.CreatedByUserId,
            description: command.Description);

        if (planResult.IsFailure)
        {
            return planResult.Error;
        }

        var financialPlan = planResult.Value;

        // 4. Adicionar ao contexto.
        await _unitOfWork.FinancialPlans
            .AddAsync(financialPlan);

        // 5. Persistir as alterações.
        await _unitOfWork.CompleteAsync();

        return financialPlan;
    }
}
