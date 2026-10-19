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

        // 1. Validar utilizador

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

        // 2. Validar nome da conta

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Error.BusinessRule(
                "Account.InvalidName",
                "O nome da conta é obrigatório.");
        }

        var accountName = command.Name.Trim();

        if (accountName.Length > 100)
        {
            return Error.BusinessRule(
                "Account.InvalidNameLength",
                "O nome da conta não pode exceder 100 caracteres.");
        }

        var normalizedName = accountName.ToLowerInvariant();

        // 3. Validar banco associado

        var bankName = command.BankName?.Trim()
            ?? string.Empty;

        if (bankName.Length > 120)
        {
            return Error.BusinessRule(
                "Account.InvalidBankName",
                "O nome do banco não pode exceder 120 caracteres.");
        }

        // Durante a transição, o campo pode estar vazio
        // para manter compatibilidade com clientes antigos.
        // O novo formulário deverá exigir uma seleção.

        // 4. Validar contexto familiar

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

        // 5. Validar nome duplicado no contexto

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
                $"Já existe uma conta ativa com o nome '{accountName}' neste contexto financeiro.");
        }

        // 6. Validar saldo e moeda

        var moneyResult = Money.TryCreate(
            command.InitialBalance,
            command.Currency);

        if (moneyResult.IsFailure)
            return moneyResult.Error;

        // 7. Criar entidade

        var accountResult = Account.Create(
            accountName,
            bankName,
            moneyResult.Value,
            command.Type,
            command.UserId,
            command.FamilyId,
            command.Description);

        if (accountResult.IsFailure)
            return accountResult.Error;

        var account = accountResult.Value;

        // 8. Persistir

        await _unitOfWork.Accounts.AddAsync(account);

        await _unitOfWork.CompleteAsync();

        // O mapper também terá de incluir BankName.
        return AccountMapper.ToDTO(account);
    }
}