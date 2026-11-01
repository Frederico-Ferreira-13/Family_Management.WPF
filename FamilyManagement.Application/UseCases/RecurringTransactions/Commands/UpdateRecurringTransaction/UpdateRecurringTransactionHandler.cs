
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.RecurringTransactions.Commands.UpdateRecurringTransaction;

public sealed class UpdateRecurringTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRecurringTransactionHandler(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<RecurringTransactionDTO>> HandleAsync(
        UpdateRecurringTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // =====================================================
        // 1. VALIDAR DADOS INICIAIS
        // =====================================================

        if (command.RecurringTransactionId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da recorrência é inválido.");
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            return Error.Validation(
                "A descrição é obrigatória.");
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

        if (command.BeneficiaryUserId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador do beneficiário é inválido.");
        }

        var description = command.Description.Trim();

        // =====================================================
        // 2. OBTER A RECORRÊNCIA
        // =====================================================

        var recurringTransaction =
            await _unitOfWork.RecurringTransactions
                .GetByIdAsync(command.RecurringTransactionId);

        if (recurringTransaction is null)
        {
            return Error.NotFound(
                "Recurring.NotFound",
                "Recorrência não encontrada.");
        }

        if (!recurringTransaction.IsActive)
        {
            return Error.Conflict(
                "Recurring.Inactive",
                "Não é possível editar uma recorrência inativa.");
        }

        // =====================================================
        // 3. VALIDAR ÂMBITO FINANCEIRO
        // =====================================================

        var scope = recurringTransaction.Scope;
        var familyId = recurringTransaction.FamilyId;
        var userId = recurringTransaction.UserId;

        if (!Enum.IsDefined(scope))
        {
            return Error.Validation(
                "O âmbito da recorrência é inválido.");
        }

        if (scope == RecurringTransactionScope.Personal)
        {
            if (familyId.HasValue ||
                recurringTransaction.FinancialPlanId.HasValue ||
                recurringTransaction.BeneficiaryUserId.HasValue)
            {
                return Error.BusinessRule(
                    "Recurring.InvalidPersonalScope",
                    "A recorrência pessoal possui associações familiares inconsistentes.");
            }
        }
        else if (scope == RecurringTransactionScope.Family)
        {
            if (!familyId.HasValue)
            {
                return Error.BusinessRule(
                    "Recurring.MissingFamily",
                    "A recorrência familiar não tem uma família associada.");
            }
        }

        // A edição normal não altera o Scope nem o FamilyId.
        // A conversão pessoal -> familiar é uma operação própria.

        // =====================================================
        // 4. VALIDAR UTILIZADOR RESPONSÁVEL
        // =====================================================

        var user = await _unitOfWork.Users
            .GetByIdAsync(userId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "O responsável pela recorrência não existe ou está inativo.");
        }

        // =====================================================
        // 5. VALIDAR FAMÍLIA
        // =====================================================

        if (scope == RecurringTransactionScope.Family)
        {
            var family = await _unitOfWork.Families
                .GetByIdAsync(familyId!.Value);

            if (family is null || !family.IsActive)
            {
                return Error.NotFound(
                    "Family.NotFound",
                    "A família não existe ou está inativa.");
            }

            if (user.FamilyId != familyId.Value)
            {
                return Error.BusinessRule(
                    "Recurring.FamilyAccessDenied",
                    "O responsável não pertence à família da recorrência.");
            }
        }

        // =====================================================
        // 6. DETERMINAR O BENEFICIÁRIO
        // =====================================================

        // Mantemos a semântica atual do comando:
        //
        // - Numa receita, null preserva o beneficiário.
        // - Numa despesa, o beneficiário é removido.
        //
        // Uma receita pessoal não pode ter beneficiário familiar.

        Guid? beneficiaryUserId =
            command.Type == TransactionType.Income
                ? command.BeneficiaryUserId
                    ?? recurringTransaction.BeneficiaryUserId
                : null;

        if (command.Type != TransactionType.Income &&
            command.BeneficiaryUserId.HasValue)
        {
            return Error.Validation(
                "Só é possível associar beneficiários a receitas.");
        }

        if (scope == RecurringTransactionScope.Personal &&
            beneficiaryUserId.HasValue)
        {
            return Error.Validation(
                "Uma recorrência pessoal não pode ter um beneficiário familiar. Converte primeiro a recorrência para o âmbito familiar.");
        }

        if (scope == RecurringTransactionScope.Family &&
            command.Type == TransactionType.Income &&
            !beneficiaryUserId.HasValue)
        {
            return Error.Validation(
                "Seleciona o membro da família que recebe esta receita.");
        }

        // =====================================================
        // 7. VALIDAR BENEFICIÁRIO
        // =====================================================

        if (beneficiaryUserId.HasValue)
        {
            if (scope != RecurringTransactionScope.Family ||
                !familyId.HasValue)
            {
                return Error.BusinessRule(
                    "Recurring.BeneficiaryScopeMismatch",
                    "O beneficiário exige uma recorrência familiar.");
            }

            var beneficiary = await _unitOfWork.Users
                .GetByIdAsync(beneficiaryUserId.Value);

            if (beneficiary is null || !beneficiary.IsActive)
            {
                return Error.NotFound(
                    "Recurring.BeneficiaryNotFound",
                    "O beneficiário não existe ou está inativo.");
            }

            if (beneficiary.FamilyId != familyId.Value)
            {
                return Error.BusinessRule(
                    "Recurring.BeneficiaryFamilyMismatch",
                    "O beneficiário não pertence à família da recorrência.");
            }
        }

        // =====================================================
        // 8. VALIDAR CONTA BANCÁRIA OPCIONAL
        // =====================================================

        if (!command.AccountId.HasValue &&
            !recurringTransaction.FinancialPlanId.HasValue)
        {
            return Error.Validation(
                "Uma recorrência sem conta deve estar associada a um plano financeiro.");
        }

        if (command.AccountId.HasValue)
        {
            var account = await _unitOfWork.Accounts
                .GetByIdAsync(command.AccountId.Value);

            if (account is null || !account.IsActive)
            {
                return Error.NotFound(
                    "Account.NotFound",
                    "A conta selecionada não existe ou está inativa.");
            }

            if (scope == RecurringTransactionScope.Family)
            {
                // =================================================
                // RECORRÊNCIA FAMILIAR
                // =================================================
                //
                // Permitimos:
                //
                // 1. Conta pertencente à família.
                // 2. Conta pessoal do responsável.
                //
                // Não permitimos contas pessoais de terceiros.
                //
                // A conta pessoal mantém a sua titularidade
                // e as suas regras de privacidade.

                var isFamilyAccount =
                    account.FamilyId == familyId;

                var isOwnPersonalAccount =
                    !account.FamilyId.HasValue &&
                    account.UserId == userId;

                if (!isFamilyAccount &&
                    !isOwnPersonalAccount)
                {
                    return Error.BusinessRule(
                        "Recurring.AccountScopeMismatch",
                        "A conta não pertence à família nem ao responsável pela recorrência.");
                }
            }
            else
            {
                // =================================================
                // RECORRÊNCIA PESSOAL
                // =================================================
                //
                // Só pode utilizar uma conta pessoal
                // pertencente ao responsável.

                if (account.UserId != userId ||
                    account.FamilyId.HasValue)
                {
                    return Error.BusinessRule(
                        "Recurring.AccountOwnerMismatch",
                        "A conta pessoal não pertence ao responsável.");
                }
            }

            // Validar moeda da conta.

            if (!string.Equals(
                    account.Balance.Currency,
                    recurringTransaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }
        }

        // =====================================================
        // 9. VALIDAR PLANO FINANCEIRO EXISTENTE
        // =====================================================

        if (recurringTransaction.FinancialPlanId.HasValue)
        {
            if (scope != RecurringTransactionScope.Family ||
                !familyId.HasValue)
            {
                return Error.BusinessRule(
                    "Recurring.FinancialPlanScopeMismatch",
                    "Um plano financeiro familiar exige uma recorrência familiar.");
            }

            var financialPlan =
                await _unitOfWork.FinancialPlans
                    .GetByIdAsync(
                        recurringTransaction.FinancialPlanId.Value);

            if (financialPlan is null ||
                !financialPlan.IsActive)
            {
                return Error.NotFound(
                    "FinancialPlan.NotFound",
                    "O plano financeiro não existe ou está inativo.");
            }

            if (financialPlan.FamilyId != familyId.Value)
            {
                return Error.BusinessRule(
                    "Recurring.FinancialPlanFamilyMismatch",
                    "O plano financeiro não pertence à família da recorrência.");
            }

            if (!string.Equals(
                    financialPlan.CurrencyCode,
                    recurringTransaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                return DomainErrors.Money.CurrencyMismatch;
            }
        }

        // =====================================================
        // 10. VALIDAR CATEGORIA
        // =====================================================

        var category = await _unitOfWork.Categories
            .GetByIdAsync(command.CategoryId);

        if (category is null || !category.IsActive)
        {
            return Error.NotFound(
                "Category.NotFound",
                "A categoria selecionada não existe ou está inativa.");
        }

        if (!category.IsGlobal)
        {
            if (scope == RecurringTransactionScope.Family)
            {
                // Uma recorrência familiar pode utilizar:
                //
                // - Categoria da família.
                // - Categoria pessoal do responsável.

                var isFamilyCategory =
                    category.FamilyId == familyId;

                var isOwnPersonalCategory =
                    !category.FamilyId.HasValue &&
                    category.UserId == userId;

                if (!isFamilyCategory &&
                    !isOwnPersonalCategory)
                {
                    return Error.BusinessRule(
                        "Recurring.CategoryScopeMismatch",
                        "A categoria não pertence à família nem ao responsável.");
                }
            }
            else
            {
                // Uma recorrência pessoal só pode utilizar
                // categorias globais ou pessoais próprias.

                if (category.FamilyId.HasValue ||
                    category.UserId != userId)
                {
                    return Error.BusinessRule(
                        "Recurring.CategoryScopeMismatch",
                        "A categoria não pertence ao responsável.");
                }
            }
        }

        // =====================================================
        // 11. VERIFICAR DUPLICADOS
        // =====================================================

        cancellationToken.ThrowIfCancellationRequested();

        if (scope == RecurringTransactionScope.Family &&
            command.Type == TransactionType.Income &&
            familyId.HasValue &&
            beneficiaryUserId.HasValue)
        {
            // Receitas familiares:
            //
            // A combinação relevante é:
            // Família + Beneficiário + Descrição.
            //
            // Dois membros diferentes podem ter
            // uma recorrência chamada "Salário".

            var currentFamilyId = familyId.Value;

            var familyIncome = await _unitOfWork
                .RecurringTransactions
                .FindRecurringTransactionsWithDetailsAsync(
                    rt =>
                        rt.IsActive &&
                        rt.FamilyId == currentFamilyId &&
                        rt.Type == TransactionType.Income);

            var duplicate = familyIncome.Any(rt =>
                rt.Id != recurringTransaction.Id &&
                rt.BeneficiaryUserId == beneficiaryUserId &&
                string.Equals(
                    rt.Description.Trim(),
                    description,
                    StringComparison.OrdinalIgnoreCase));

            if (duplicate)
            {
                return Error.Conflict(
                    "Recurring.Duplicate",
                    "Este beneficiário já tem uma receita recorrente com esta descrição.");
            }
        }
        else if (!recurringTransaction.Description.Equals(
                     description,
                     StringComparison.OrdinalIgnoreCase))
        {
            // Mantemos a verificação existente para
            // recorrências pessoais e despesas.

            var exists = await _unitOfWork
                .RecurringTransactions
                .RecurringTransactionExistsForUserByDescriptionAndIdAsync(
                    description,
                    userId,
                    recurringTransaction.Id);

            if (exists)
            {
                return Error.Conflict(
                    "Recurring.Duplicate",
                    "Já existe outra recorrência com esta descrição.");
            }
        }

        // =====================================================
        // 12. VALIDAR VALOR MONETÁRIO
        // =====================================================

        var moneyResult = Money.TryCreate(
            command.Amount,
            recurringTransaction.Amount.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // =====================================================
        // 13. ATUALIZAR DETALHES
        // =====================================================

        // O Domain continua responsável por validar:
        //
        // - Datas.
        // - Frequência.
        // - Valor monetário.
        // - Restrições após movimentos gerados.
        //
        // Não alteramos Scope nem FamilyId nesta operação.

        var updateResult = recurringTransaction.UpdateDetails(
            description: description,
            amount: moneyResult.Value,
            type: command.Type,
            frequency: command.Frequency,
            categoryId: command.CategoryId,
            accountId: command.AccountId,
            endDate: command.EndDate,
            isActive: recurringTransaction.IsActive,
            scheduleDate: command.ScheduleDate);

        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        // =====================================================
        // 14. ATUALIZAR BENEFICIÁRIO
        // =====================================================

        // As condições de âmbito, tipo e pertença
        // à família já foram verificadas acima.

        var beneficiaryResult =
            recurringTransaction.AssignBeneficiary(
                beneficiaryUserId);

        if (beneficiaryResult.IsFailure)
        {
            return beneficiaryResult.Error;
        }

        // =====================================================
        // 15. PERSISTIR
        // =====================================================

        cancellationToken.ThrowIfCancellationRequested();

        _unitOfWork.RecurringTransactions.Update(
            recurringTransaction);

        await _unitOfWork.CompleteAsync();

        // =====================================================
        // 16. DEVOLVER DTO
        // =====================================================

        return RecurringTransactionMapper.ToDTO(
            recurringTransaction);
    }
}