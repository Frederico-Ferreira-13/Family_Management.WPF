
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Application.UseCases.Transactions.Services;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Transactions.Commands.CreateTransaction;

public sealed class CreateTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionImpactService _impactService;

    public CreateTransactionHandler(
        IUnitOfWork unitOfWork,
        ITransactionImpactService impactService)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _impactService = impactService
            ?? throw new ArgumentNullException(nameof(impactService));
    }

    public async Task<Result<TransactionDTO>> HandleAsync(
        CreateTransactionCommand command)
    {
        // --------------------------------------------------
        // 1. Validação dos identificadores
        // --------------------------------------------------

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

        // Um movimento sem conta bancária deve
        // estar associado a um plano financeiro.
        if (!command.AccountId.HasValue &&
            !command.FinancialPlanId.HasValue)
        {
            return Error.Validation(
                "Um movimento sem conta bancária deve estar associado a um plano financeiro.");
        }

        // --------------------------------------------------
        // 2. Validar utilizador
        // --------------------------------------------------

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado ou inativo.");
        }

        // --------------------------------------------------
        // 3. Validar âmbito familiar
        // --------------------------------------------------

        if (command.FamilyId.HasValue)
        {
            if (user.FamilyId != command.FamilyId)
            {
                return Error.BusinessRule(
                    "Transaction.FamilyAccessDenied",
                    "O utilizador não pertence à família indicada.");
            }

            var family = await _unitOfWork.Families
                .GetByIdAsync(command.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.NotFound(
                    "Family.NotFound",
                    "Família não encontrada ou inativa.");
            }
        }

        // --------------------------------------------------
        // 4. Validar plano financeiro
        // --------------------------------------------------

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

            // O plano financeiro tem de pertencer
            // à família indicada.
            if (command.FamilyId.HasValue)
            {
                if (financialPlan.FamilyId != command.FamilyId.Value)
                {
                    return Error.BusinessRule(
                        "Transaction.FinancialPlanFamilyMismatch",
                        "O plano financeiro não pertence à família indicada.");
                }
            }
            else
            {
                // Não permitir associar um movimento
                // pessoal a um plano familiar.
                return Error.BusinessRule(
                    "Transaction.FinancialPlanRequiresFamily",
                    "É necessário indicar a família para utilizar um plano financeiro familiar.");
            }
        }

        // --------------------------------------------------
        // 5. Validar conta bancária (opcional)
        // --------------------------------------------------

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

            if (command.FamilyId.HasValue)
            {
                // A conta pode pertencer a outro membro,
                // desde que esteja associada à família.
                if (account.FamilyId != command.FamilyId)
                {
                    return Error.BusinessRule(
                        "Transaction.AccountFamilyMismatch",
                        "A conta bancária não pertence à família indicada.");
                }
            }
            else
            {
                // Movimento pessoal:
                // apenas contas pessoais do utilizador.
                if (account.UserId != command.UserId ||
                    account.FamilyId.HasValue)
                {
                    return Error.BusinessRule(
                        "Transaction.AccountOwnerMismatch",
                        "A conta não é uma conta pessoal do utilizador.");
                }
            }
        }

        // --------------------------------------------------
        // 6. Validar categoria
        // --------------------------------------------------

        var category = await _unitOfWork.Categories
            .GetByIdAsync(command.CategoryId);

        if (category is null || !category.IsActive)
        {
            return Error.NotFound(
                "Category.NotFound",
                "Categoria não encontrada ou inativa.");
        }

        // Categorias globais podem ser utilizadas
        // em movimentos pessoais e familiares.
        if (!category.IsGlobal)
        {
            if (command.FamilyId.HasValue)
            {
                if (category.FamilyId != command.FamilyId)
                {
                    return Error.BusinessRule(
                        "Transaction.CategoryScopeMismatch",
                        "A categoria não pertence à família indicada.");
                }
            }
            else
            {
                if (category.FamilyId.HasValue ||
                    category.UserId != command.UserId)
                {
                    return Error.BusinessRule(
                        "Transaction.CategoryScopeMismatch",
                        "A categoria não pertence ao utilizador.");
                }
            }
        }

        // --------------------------------------------------
        // 7. Criar e validar valor monetário
        // --------------------------------------------------

        var moneyResult = Money.TryCreate(
            command.Amount,
            command.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var money = moneyResult.Value;

        // --------------------------------------------------
        // 8. Validar moeda da conta bancária
        // --------------------------------------------------

        if (account is not null &&
            !string.Equals(
                account.Balance.Currency,
                money.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        // --------------------------------------------------
        // 9. Validar moeda do plano financeiro
        // --------------------------------------------------

        if (financialPlan is not null &&
            !string.Equals(
                financialPlan.CurrencyCode,
                money.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            return DomainErrors.Money.CurrencyMismatch;
        }

        // --------------------------------------------------
        // 10. Criar movimento
        // --------------------------------------------------

        var transactionResult =
            Transaction.CreateIncomeOrExpense(
                command.Date,
                money,
                command.Description,
                command.Type,
                command.UserId,
                command.AccountId,
                command.CategoryId,
                command.Notes,
                command.BudgetId,
                command.GoalId,
                command.IsConfirmed,
                familyId: command.FamilyId,
                financialPlanId: command.FinancialPlanId);

        if (transactionResult.IsFailure)
        {
            return transactionResult.Error;
        }

        var transaction = transactionResult.Value;

        // --------------------------------------------------
        // 11. Persistir numa transação de base de dados
        // --------------------------------------------------

        using var databaseTransaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Apenas movimentos confirmados
            // produzem impactos financeiros reais.
            if (transaction.IsConfirmed)
            {
                var impactResult =
                    await _impactService.ApplyAsync(transaction);

                if (impactResult.IsFailure)
                {
                    await databaseTransaction.RollbackAsync();
                    return impactResult.Error;
                }
            }

            await _unitOfWork.Transactions
                .AddAsync(transaction);

            await _unitOfWork.CompleteAsync();

            await databaseTransaction.CommitAsync();

            return TransactionMapper.ToDTO(transaction);
        }
        catch
        {
            await databaseTransaction.RollbackAsync();
            throw;
        }
    }
}