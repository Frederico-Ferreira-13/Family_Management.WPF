using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Accounts.Commands.CreateAccount;

public sealed class CreateAccountHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AccountDTO>> HandleAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(command.UserId);

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
                "Um utilizador inativo não pode criar contas.");
        }

        var normalizedName = command.Name
            .Trim()
            .ToLowerInvariant();

        var duplicate = await _unitOfWork.Accounts.AnyAsync(a =>
            a.UserId == command.UserId &&
            a.Name.ToLower() == normalizedName &&
            a.IsActive);

        if (duplicate)
        {
            return Error.Conflict(
                "Account.DuplicateName",
                $"Já existe uma conta ativa com o nome '{command.Name}'.");
        }

        if (command.FamilyId.HasValue)
        {
            var family = await _unitOfWork.Families
                .GetByIdAsync(command.FamilyId.Value);

            if (family is null || !family.IsActive)
            {
                return Error.NotFound(
                    "Family.NotFound",
                    "A família especificada não existe ou está inativa.");
            }

            if (user.FamilyId != command.FamilyId.Value)
            {
                return Error.BusinessRule(
                    "Account.FamilyMismatch",
                    "O utilizador não pertence à família indicada.");
            }
        }

        var moneyResult = Money.TryCreate(
            command.InitialBalance,
            command.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        var accountResult = Account.Create(
            command.Name,
            moneyResult.Value,
            command.Type,
            command.UserId,
            command.FamilyId,
            command.Description);

        if (accountResult.IsFailure)
        {
            return accountResult.Error;
        }

        var account = accountResult.Value;

        await _unitOfWork.Accounts.AddAsync(account);

        await _unitOfWork.CompleteAsync();

        return AccountMapper.ToDTO(account);
    }
}