using FamilyManagement.Application.UseCases.Accounts.DTOs;
using FamilyManagement.Application.UseCases.Accounts.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Accounts.Commands.UpdateAccount;

public sealed class UpdateAccountHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAccountHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<AccountDTO>> HandleAsync(
        UpdateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (command.AccountId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da conta é inválido.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Error.Validation(
                "O nome da conta é obrigatório.");
        }

        var normalizedName = command.Name.Trim();

        if (normalizedName.Length > 100)
        {
            return Error.Validation(
                "O nome da conta não pode exceder 100 caracteres.");
        }

        var normalizedBankName =
            command.BankName?.Trim() ?? string.Empty;

        if (normalizedBankName.Length > 120)
        {
            return Error.Validation(
                "O nome do banco não pode exceder 120 caracteres.");
        }

        var account = await _unitOfWork.Accounts
            .GetByIdAsync(command.AccountId);

        if (account is null)
        {
            return Error.NotFound(
                "Account.NotFound",
                "Conta não encontrada.");
        }

        if (!account.IsActive)
        {
            return Error.BusinessRule(
                "Account.Inactive",
                "Não é possível editar uma conta inativa.");
        }

        var normalizedNameLower =
            normalizedName.ToLowerInvariant();

        var duplicate = await _unitOfWork.Accounts.AnyAsync(a =>
            a.Id != account.Id &&
            a.IsActive &&
            a.Name.ToLower() == normalizedNameLower &&
            (
                account.FamilyId.HasValue
                    ? a.FamilyId == account.FamilyId
                    : a.FamilyId == null &&
                      a.UserId == account.UserId
            ));

        if (duplicate)
        {
            return Error.Conflict(
                "Account.DuplicateName",
                "Já existe outra conta ativa com este nome no mesmo contexto financeiro.");
        }

        var detailsResult = account.UpdateDetails(
            normalizedName,
            command.Type);

        if (detailsResult.IsFailure)
            return detailsResult.Error;

        var bankResult = account.UpdateBank(
            normalizedBankName);

        if (bankResult.IsFailure)
            return bankResult.Error;

        var descriptionResult = account.UpdateDescription(
            command.Description);

        if (descriptionResult.IsFailure)
            return descriptionResult.Error;

        _unitOfWork.Accounts.Update(account);

        await _unitOfWork.CompleteAsync();

        return AccountMapper.ToDTO(account);
    }
}