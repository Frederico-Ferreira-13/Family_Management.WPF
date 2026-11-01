using System;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Transactions.Commands
    .ReclassifyTransaction;

public sealed class ReclassifyTransactionHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ReclassifyTransactionHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<TransactionDTO>> HandleAsync(
        ReclassifyTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Validar a sessão.

        if (!_currentUserService.IsAuthenticated ||
            !_currentUserService.UserId.HasValue)
        {
            return Error.BusinessRule(
                "Transaction.AuthenticationRequired",
                "É necessário iniciar sessão.");
        }

        var currentUserId = _currentUserService.UserId.Value;

        if (command.UserId == Guid.Empty ||
            command.UserId != currentUserId)
        {
            return Error.BusinessRule(
                "Transaction.AccessDenied",
                "O utilizador indicado não corresponde à sessão atual.");
        }

        if (command.TransactionId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da transação é obrigatório.");
        }

        if (!Enum.IsDefined(command.TargetScope))
        {
            return Error.Validation(
                "O âmbito financeiro indicado é inválido.");
        }

        // 2. Obter o utilizador.

        var user = await _unitOfWork.Users
            .GetByIdAsync(currentUserId);

        if (user is null || !user.IsActive)
        {
            return Error.NotFound(
                "User.NotFound",
                "O utilizador não existe ou está inativo.");
        }

        // 3. Obter a transação.

        var transaction = await _unitOfWork.Transactions
            .GetByIdAsync(command.TransactionId);

        if (transaction is null || !transaction.IsActive)
        {
            return Error.NotFound(
                "Transaction.NotFound",
                "A transação não existe ou está inativa.");
        }

        // Primeira versão: apenas o responsável pelo
        // movimento pode alterar o respetivo âmbito.

        if (transaction.UserId != currentUserId)
        {
            return Error.BusinessRule(
                "Transaction.AccessDenied",
                "Não tem autorização para reclassificar esta transação.");
        }

        if (transaction.Type == TransactionType.Income &&
            transaction.IsConfirmed)
        {
            return Error.BusinessRule(
                "Transaction.ConfirmedIncomeReclassification",
                "Desconfirme a receita antes de alterar o seu âmbito. "
                + "Depois da alteração, confirme-a novamente para aplicar "
                + "a contribuição ao fundo correto.");
        }

        // 4. Validar o tipo e as associações existentes.

        if (transaction.Type == TransactionType.Transfer)
        {
            return Error.BusinessRule(
                "Transaction.TransferReclassificationNotAllowed",
                "Não é possível reclassificar transferências bancárias.");
        }

        if (transaction.BudgetId.HasValue ||
            transaction.GoalId.HasValue)
        {
            return Error.BusinessRule(
                "Transaction.LinkedFinancialImpact",
                "O movimento possui um orçamento ou uma meta associados. " +
                "É necessário tratar essas associações antes da reclassificação.");
        }

        // 5. Validar o âmbito atual.

        if (transaction.Scope == RecurringTransactionScope.Personal)
        {
            if (transaction.FamilyId.HasValue ||
                transaction.FinancialPlanId.HasValue ||
                transaction.BeneficiaryUserId.HasValue)
            {
                return Error.BusinessRule(
                    "Transaction.InvalidPersonalScope",
                    "O movimento pessoal possui associações familiares inconsistentes.");
            }
        }
        else if (transaction.Scope == RecurringTransactionScope.Family)
        {
            if (!transaction.FamilyId.HasValue)
            {
                return Error.BusinessRule(
                    "Transaction.InvalidFamilyScope",
                    "O movimento familiar não possui uma família associada.");
            }
        }
        else
        {
            return Error.Validation(
                "O âmbito atual da transação é inválido.");
        }

        // 6. Validar a conta existente.
        // Não alteramos AccountId.

        if (transaction.AccountId.HasValue)
        {
            var account = await _unitOfWork.Accounts
                .GetByIdAsync(transaction.AccountId.Value);

            if (account is null)
            {
                return Error.NotFound(
                    "Account.NotFound",
                    "A conta associada não foi encontrada.");
            }

            var isOwnPersonalAccount =
                !account.FamilyId.HasValue &&
                account.UserId == currentUserId;

            var isCurrentFamilyAccount =
                transaction.FamilyId.HasValue &&
                account.FamilyId == transaction.FamilyId;

            if (!isOwnPersonalAccount &&
                !isCurrentFamilyAccount)
            {
                return Error.BusinessRule(
                    "Transaction.AccountAccessDenied",
                    "A conta associada não pertence ao responsável nem à família atual.");
            }

            // Nesta primeira versão, não permitimos converter
            // para pessoal um movimento de uma conta familiar.

            if (command.TargetScope == RecurringTransactionScope.Personal &&
                !isOwnPersonalAccount)
            {
                return Error.BusinessRule(
                    "Transaction.PersonalAccountRequired",
                    "Para reclassificar como pessoal, o movimento deve utilizar uma conta pessoal própria.");
            }
        }

        // 7. Validar a categoria existente.

        if (transaction.CategoryId.HasValue)
        {
            var category = await _unitOfWork.Categories
                .GetByIdAsync(transaction.CategoryId.Value);

            if (category is null || !category.IsActive)
            {
                return Error.NotFound(
                    "Category.NotFound",
                    "A categoria não existe ou está inativa.");
            }

            if (!category.IsGlobal)
            {
                var isOwnPersonalCategory =
                    !category.FamilyId.HasValue &&
                    category.UserId == currentUserId;

                var isTargetFamilyCategory =
                    command.TargetScope == RecurringTransactionScope.Family &&
                    command.FamilyId.HasValue &&
                    category.FamilyId == command.FamilyId;

                if (!isOwnPersonalCategory &&
                    !isTargetFamilyCategory)
                {
                    return Error.BusinessRule(
                        "Transaction.CategoryScopeMismatch",
                        "A categoria não é compatível com o novo âmbito financeiro.");
                }
            }
        }

        // 8. Preparar a reclassificação.

        Result reclassificationResult;

        if (command.TargetScope == RecurringTransactionScope.Family)
        {
            if (!command.FamilyId.HasValue ||
                command.FamilyId.Value == Guid.Empty)
            {
                return Error.Validation(
                    "É obrigatório selecionar uma família.");
            }

            var familyId = command.FamilyId.Value;

            var family = await _unitOfWork.Families
                .GetByIdAsync(familyId);

            if (family is null || !family.IsActive)
            {
                return Error.NotFound(
                    "Family.NotFound",
                    "A família não existe ou está inativa.");
            }

            if (user.FamilyId != familyId)
            {
                return Error.BusinessRule(
                    "Transaction.FamilyAccessDenied",
                    "O utilizador não pertence à família selecionada.");
            }

            if (transaction.AccountId.HasValue)
            {
                var account = await _unitOfWork.Accounts
                    .GetByIdAsync(transaction.AccountId.Value);

                if (account is null ||
                    (account.FamilyId != familyId &&
                     !(account.FamilyId is null &&
                       account.UserId == currentUserId)))
                {
                    return Error.BusinessRule(
                        "Transaction.AccountScopeMismatch",
                        "A conta não é compatível com a família selecionada.");
                }
            }
            else if (!command.FinancialPlanId.HasValue)
            {
                return Error.Validation(
                    "Um movimento sem conta deve estar associado a um plano financeiro.");
            }

            if (command.FinancialPlanId.HasValue)
            {
                if (command.FinancialPlanId.Value == Guid.Empty)
                {
                    return Error.Validation(
                        "O identificador do plano financeiro é inválido.");
                }

                var financialPlan = await _unitOfWork.FinancialPlans
                    .GetByIdAsync(command.FinancialPlanId.Value);

                if (financialPlan is null || !financialPlan.IsActive)
                {
                    return Error.NotFound(
                        "FinancialPlan.NotFound",
                        "O plano financeiro não existe ou está inativo.");
                }

                if (financialPlan.FamilyId != familyId)
                {
                    return Error.BusinessRule(
                        "Transaction.FinancialPlanFamilyMismatch",
                        "O plano financeiro não pertence à família selecionada.");
                }

                if (!string.Equals(
                    financialPlan.CurrencyCode,
                    transaction.Amount.Currency,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return DomainErrors.Money.CurrencyMismatch;
                }
            }

            if (command.BeneficiaryUserId.HasValue)
            {
                if (transaction.Type != TransactionType.Income)
                {
                    return Error.Validation(
                        "Apenas receitas podem ter um beneficiário.");
                }

                if (command.BeneficiaryUserId.Value == Guid.Empty)
                {
                    return Error.Validation(
                        "O identificador do beneficiário é inválido.");
                }

                var beneficiary = await _unitOfWork.Users
                    .GetByIdAsync(command.BeneficiaryUserId.Value);

                if (beneficiary is null || !beneficiary.IsActive)
                {
                    return Error.NotFound(
                        "User.BeneficiaryNotFound",
                        "O beneficiário não existe ou está inativo.");
                }

                if (beneficiary.FamilyId != familyId)
                {
                    return Error.BusinessRule(
                        "Transaction.BeneficiaryFamilyMismatch",
                        "O beneficiário não pertence à família selecionada.");
                }
            }

            reclassificationResult = transaction.ReclassifyAsFamily(
                familyId,
                command.FinancialPlanId,
                command.BeneficiaryUserId);
        }
        else
        {
            if (command.FamilyId.HasValue ||
                command.FinancialPlanId.HasValue ||
                command.BeneficiaryUserId.HasValue)
            {
                return Error.Validation(
                    "Uma transação pessoal não pode ter associações familiares.");
            }

            if (!transaction.AccountId.HasValue)
            {
                return Error.BusinessRule(
                    "Transaction.PersonalAccountRequired",
                    "A reclassificação como pessoal exige uma conta pessoal própria.");
            }

            reclassificationResult = transaction.ReclassifyAsPersonal();
        }

        if (reclassificationResult.IsFailure)
            return reclassificationResult.Error;

        // 9. Persistir a alteração.
        // Não chamar ApplyAsync nem RevertAsync:
        // o saldo bancário não deve ser modificado.

        cancellationToken.ThrowIfCancellationRequested();

        using var dbTransaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            _unitOfWork.Transactions.Update(transaction);

            await _unitOfWork.CompleteAsync();

            await dbTransaction.CommitAsync();

            return Result<TransactionDTO>.Success(
                TransactionMapper.ToDTO(transaction));
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            _unitOfWork.ClearTrackedChanges();
            throw;
        }
    }
}