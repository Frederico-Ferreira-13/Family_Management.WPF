
using FamilyManagement.Application.UseCases.Budgets.DTOs;
using FamilyManagement.Application.UseCases.Budgets.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Budgets.Commands.CreateBudget;

public sealed class CreateBudgetHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateBudgetHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<BudgetDTO>> HandleAsync(
        CreateBudgetCommand command,
        CancellationToken cancellationToken = default)
    {
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

        if (command.FamilyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da família é inválido.");
        }

        var user = await _unitOfWork.Users
            .GetByIdAsync(command.UserId);

        if (user is null)
        {
            return Error.NotFound(
                "User.NotFound",
                "Utilizador não encontrado.");
        }

        if (!user.IsActive)
        {
            return Error.BusinessRule(
                "User.Inactive",
                "Um utilizador inativo não pode criar orçamentos.");
        }

        if (command.FamilyId.HasValue)
        {
            if (user.FamilyId != command.FamilyId)
            {
                return Error.BusinessRule(
                    "Budget.FamilyAccessDenied",
                    "O utilizador não pertence à família indicada.");
            }

            var family = await _unitOfWork.Families
                .GetByIdAsync(command.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.BusinessRule(
                    "Budget.FamilyUnavailable",
                    "A família não existe ou está inativa.");
            }
        }

        var category = await _unitOfWork.Categories
            .GetByIdAsync(command.CategoryId);

        if (category is null)
        {
            return Error.NotFound(
                "Category.NotFound",
                "Categoria não encontrada.");
        }

        if (!category.IsActive)
        {
            return Error.BusinessRule(
                "Category.Inactive",
                "Não é possível criar um orçamento para uma categoria inativa.");
        }

        // Categorias globais são válidas em ambos os âmbitos.
        if (!category.IsGlobal)
        {
            if (command.FamilyId.HasValue)
            {
                if (category.FamilyId != command.FamilyId)
                {
                    return Error.BusinessRule(
                        "Budget.CategoryScopeMismatch",
                        "A categoria não pertence à família indicada.");
                }
            }
            else
            {
                if (category.FamilyId.HasValue ||
                    category.UserId != command.UserId)
                {
                    return Error.BusinessRule(
                        "Budget.CategoryScopeMismatch",
                        "A categoria não pertence ao utilizador.");
                }
            }
        }

        var moneyResult = Money.TryCreate(
            command.Amount,
            command.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        // A verificação inclui orçamentos pessoais e
        // familiares, distinguindo-os pelo FamilyId.
        var existingBudgets = await _unitOfWork.Budgets
            .FindBudgetsWithDetailsAsync(b =>
                b.CategoryId == command.CategoryId &&
                b.Month == command.Month &&
                b.Year == command.Year &&
                (
                    command.FamilyId.HasValue
                        ? b.FamilyId == command.FamilyId
                        : b.FamilyId == null &&
                          b.UserId == command.UserId
                ));

        // A restrição de unicidade da base de dados
        // abrange também orçamentos inativos.
        if (existingBudgets.Any())
        {
            return Error.Conflict(
                "Budget.AlreadyExists",
                "Já existe um orçamento para esta categoria neste período.");
        }

        var budgetResult = Budget.Create(
            name: command.Name,
            amount: moneyResult.Value,
            month: command.Month,
            year: command.Year,
            categoryId: command.CategoryId,
            userId: command.UserId,
            familyId: command.FamilyId);

        if (budgetResult.IsFailure)
        {
            return budgetResult.Error;
        }

        var budget = budgetResult.Value;

        await _unitOfWork.Budgets.AddAsync(budget);

        await _unitOfWork.CompleteAsync();

        return BudgetMapper.ToDTO(budget);
    }
}
