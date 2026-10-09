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
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BudgetDTO>> HandleAsync(
        CreateBudgetCommand command,
        CancellationToken cancellationToken = default)
    {
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

        var existingBudget = await _unitOfWork.Budgets
            .GetBudgetsByCategoryIdAndUserIdAndMonthYearAsync(
                command.CategoryId,
                command.UserId,
                command.Month,
                command.Year);

        if (existingBudget is not null)
        {
            return Error.Conflict(
                "Budget.AlreadyExists",
                "Já existe um orçamento para esta categoria neste período.");
        }

        var moneyResult = Money.TryCreate(
            command.Amount,
            command.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var budgetResult = Budget.Create(
            name: command.Name,
            amount: moneyResult.Value,
            month: command.Month,
            year: command.Year,
            categoryId: command.CategoryId,
            userId: command.UserId);

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