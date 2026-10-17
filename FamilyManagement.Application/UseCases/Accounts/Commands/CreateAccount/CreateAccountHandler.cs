using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Accounts.Commands.CreateAccount;

public sealed class CreateAccountHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<AccountDTO>> HandleAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Validar utilizador autenticado

        if (command.UserId == Guid.Empty)
        {
            return Error.BusinessRule(
                "Account.InvalidUser",
                "É necessário identificar o utilizador.");
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
                "Um utilizador inativo não pode criar contas.");
        }

        // 2. Validar dados da conta

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Error.BusinessRule(
                "Account.InvalidName",
                "O nome da conta é obrigatório.");
        }

        var normalizedName = command.Name
            .Trim()
            .ToLowerInvariant();

        // 3. Validar contexto familiar

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

        // 4. Validar nome duplicado no contexto correto

        var duplicate = await _unitOfWork.Accounts.AnyAsync(
            account =>
                account.IsActive &&
                account.Name.ToLower() == normalizedName &&
                (
                    command.FamilyId.HasValue
                        ? account.FamilyId == command.FamilyId
                        : account.FamilyId == null &&
                          account.UserId == command.UserId
                ));

        if (duplicate)
        {
            return Error.Conflict(
                "Account.DuplicateName",
                $"Já existe uma conta ativa com o nome '{command.Name}' neste contexto financeiro.");
        }

        // 5. Validar saldo inicial e moeda

        var moneyResult = Money.TryCreate(
            command.InitialBalance,
            command.Currency);

        if (moneyResult.IsFailure)
        {
            return moneyResult.Error;
        }

        // 6. Criar conta

        var accountResult = Account.Create(
            command.Name.Trim(),
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

        // 7. Persistir

        await _unitOfWork.Accounts.AddAsync(account);

        await _unitOfWork.CompleteAsync();

        return AccountMapper.ToDTO(account);
    }
}