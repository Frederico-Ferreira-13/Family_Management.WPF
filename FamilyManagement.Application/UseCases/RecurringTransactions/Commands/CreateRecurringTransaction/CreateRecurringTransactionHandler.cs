using System;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.CreateRecurringTransaction;

public sealed class CreateRecurringTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateRecurringTransactionHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<RecurringTransactionDTO>> HandleAsync(
        CreateRecurringTransactionCommand command,
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId == Guid.Empty)
        {
            return Error.Validation(
                "O utilizador é obrigatório.");
        }

        if (command.CategoryId == Guid.Empty)
        {
            return Error.Validation(
                "A categoria é obrigatória.");
        }

        if (command.AccountId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da conta é inválido.");
        }

        if (command.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é inválido.");
        }

        if (command.FinancialPlanId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do plano financeiro é inválido.");
        }

        if (command.BeneficiaryUserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do beneficiário é inválido.");
        }

        if (!Enum.IsDefined(command.Scope))
        {
            return Error.Validation(
                "O âmbito da recorrência é inválido.");
        }

        if (command.Scope == RecurringTransactionScope.Personal)
        {
            if (command.FamilyId.HasValue ||
                command.FinancialPlanId.HasValue ||
                command.BeneficiaryUserId.HasValue)
            {
                return Error.Validation(
                    "Uma recorrência pessoal não pode ter família, plano financeiro familiar ou beneficiário familiar.");
            }
        }
        else if (command.Scope == RecurringTransactionScope.Family)
        {
            if (!command.FamilyId.HasValue)
            {
                return Error.Validation(
                    "Seleciona a família à qual pertence a recorrência.");
            }

            if (command.Type == TransactionType.Income &&
                !command.BeneficiaryUserId.HasValue)
            {
                return Error.Validation(
                    "Seleciona o membro da família que recebe esta receita.");
            }

            if (command.Type != TransactionType.Income &&
                command.BeneficiaryUserId.HasValue)
            {
                return Error.Validation(
                    "Só é possível identificar um beneficiário numa receita.");
            }
        }

        if (!command.AccountId.HasValue &&
            !command.FinancialPlanId.HasValue)
        {
            return Error.Validation(
                "Uma recorrência sem conta bancária deve estar associada a um plano financeiro.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        Family? family = null;

        if (command.Scope == RecurringTransactionScope.Family)
        {
            var familyId = command.FamilyId!.Value;

            if (user.FamilyId != familyId)
            {
                return Error.BusinessRule(
                    "Recurring.FamilyAccessDenied",
                    "O utilizador não pertence à família indicada.");
            }

            family = await _unitOfWork.Families
                .GetByIdAsync(familyId);

            if (family is null || !family.IsActive)
            {
                return Error.NotFound(
                    "Family.NotFound",
                    "Família não encontrada ou inativa.");
            }
        }

        User? beneficiary = null;

        if (command.BeneficiaryUserId.HasValue)
        {
            if (command.Type != TransactionType.Income)
            {
                return Error.Validation(
                    "Só é possível associar um beneficiário a uma receita.");
            }

            if (command.Scope != RecurringTransactionScope.Family)
            {
                return Error.Validation(
                    "O beneficiário exige uma recorrência familiar.");
            }

            beneficiary = await _unitOfWork.Users
                .GetByIdAsync(command.BeneficiaryUserId.Value);

            if (beneficiary is null || !beneficiary.IsActive)
            {
                return Error.NotFound(
                    "Recurring.BeneficiaryNotFound",
                    "O membro beneficiário não existe ou está inativo.");
            }

            if (beneficiary.FamilyId != command.FamilyId)
            {
                return Error.BusinessRule(
                    "Recurring.BeneficiaryFamilyMismatch",
                    "O beneficiário não pertence à família indicada.");
            }
        }

        if (command.Scope == RecurringTransactionScope.Family &&
            command.Type == TransactionType.Income &&
            beneficiary is null)
        {
            return Error.Validation(
                "Seleciona o membro da família que recebe esta receita.");
        }

        FinancialPlan? financialPlan = null;

        if (command.FinancialPlanId.HasValue)
        {
            financialPlan = await _unitOfWork.FinancialPlans
                .GetByIdAsync(command.FinancialPlanId.Value);

            if (financialPlan is null || !financialPlan.IsActive)
            {
                return Error.NotFound(
                    "FinancialPlan.NotFound",
                    "Plano financeiro não encontrado ou inativo.");
            }

            if (command.Scope != RecurringTransactionScope.Family ||
                financialPlan.FamilyId != command.FamilyId)
            {
                return Error.BusinessRule(
                    "Recurring.FinancialPlanFamilyMismatch",
                    "O plano financeiro não pertence à família indicada.");
            }
        }

        Account? account = null;

        if (command.AccountId.HasValue)
        {
            account = await _unitOfWork.Accounts
                .GetByIdAsync(command.AccountId.Value);

            if (account is null || !account.IsActive)
            {
                return Error.NotFound(
                    "Account.NotFound",
                    "Conta bancária não encontrada ou inativa.");
            }

            if (command.Scope == RecurringTransactionScope.Family)
            {
                var isFamilyAccount =
                    account.FamilyId == command.FamilyId;

                var isOwnPersonalAccount =
                    !account.FamilyId.HasValue &&
                    account.UserId == command.UserId;

                if (!isFamilyAccount && !isOwnPersonalAccount)
                {
                    return Error.BusinessRule(
                        "Recurring.AccountScopeMismatch",
                        "A conta não pertence à família nem ao responsável pela recorrência.");
                }
            }
            else
            {
                if (account.UserId != command.UserId ||
                    account.FamilyId.HasValue)
                {
                    return Error.BusinessRule(
                        "Recurring.AccountOwnerMismatch",
                        "A conta pessoal não pertence ao utilizador.");
                }
            }
        }

        var category = await _unitOfWork.Categories
            .GetByIdAsync(command.CategoryId);

        if (category is null || !category.IsActive)
        {
            return Error.NotFound(
                "Category.NotFound",
                "A categoria selecionada é inválida.");
        }

        if (!category.IsGlobal)
        {
            if (command.Scope == RecurringTransactionScope.Family)
            {
                var isFamilyCategory =
                    category.FamilyId == command.FamilyId;

                var isOwnPersonalCategory =
                    !category.FamilyId.HasValue &&
                    category.UserId == command.UserId;

                if (!isFamilyCategory && !isOwnPersonalCategory)
                {
                    return Error.BusinessRule(
                        "Recurring.CategoryScopeMismatch",
                        "A categoria não pertence à família nem ao responsável.");
                }
            }
            else
            {
                if (category.FamilyId.HasValue ||
                    category.UserId != command.UserId)
                {
                    return Error.BusinessRule(
                        "Recurring.CategoryScopeMismatch",
                        "A categoria não pertence ao utilizador.");
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        bool exists;

        if (command.Scope == RecurringTransactionScope.Family &&
            command.Type == TransactionType.Income &&
            command.FamilyId.HasValue &&
            command.BeneficiaryUserId.HasValue)
        {
            exists = await _unitOfWork.RecurringTransactions
                .RecurringTransactionExistsForBeneficiaryByDescriptionAsync(
                    command.Description,
                    command.FamilyId.Value,
                    command.BeneficiaryUserId.Value);
        }
        else
        {
            exists = await _unitOfWork.RecurringTransactions
                .RecurringTransactionExistsForUserByDescriptionAsync(
                    command.Description,
                    command.UserId);
        }

        if (exists)
        {
            return Error.Conflict(
                "Recurring.Duplicate",
                "Já existe uma recorrência ativa com esta descrição para o mesmo responsável ou beneficiário.");
        }        

        var moneyResult = Money.TryCreate(
            command.Amount,
            command.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var money = moneyResult.Value;

        if (account is not null &&
            !string.Equals(
                account.Balance.Currency,
                money.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        if (financialPlan is not null &&
            !string.Equals(
                financialPlan.CurrencyCode,
                money.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            return DomainErrors.Money.CurrencyMismatch;
        }        

        cancellationToken.ThrowIfCancellationRequested();

        var recurringResult = RecurringTransaction.Create(
            description: command.Description,
            amount: money,
            type: command.Type,
            frequency: command.Frequency,
            startDate: command.StartDate,
            categoryId: command.CategoryId,
            accountId: command.AccountId,
            userId: command.UserId,
            familyId: command.FamilyId,
            financialPlanId: command.FinancialPlanId,
            endDate: command.EndDate,
            beneficiaryUserId: command.BeneficiaryUserId);

        if (recurringResult.IsFailure)
        {
            return recurringResult.Error;
        }

        var recurringTransaction = recurringResult.Value;        

        if (recurringTransaction.Scope != command.Scope)
        {
            return Error.BusinessRule(
                "Recurring.ScopeMismatch",
                "O âmbito da recorrência criada não corresponde ao âmbito solicitado.");
        }        

        await _unitOfWork.RecurringTransactions
            .AddAsync(recurringTransaction);

        cancellationToken.ThrowIfCancellationRequested();

        await _unitOfWork.CompleteAsync();        

        return RecurringTransactionMapper.ToDTO(
            recurringTransaction);
    }
}