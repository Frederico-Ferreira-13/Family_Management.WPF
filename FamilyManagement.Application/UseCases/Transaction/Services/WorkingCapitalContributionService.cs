using System;
using System.Linq;
using System.Threading.Tasks;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;
using FamilyManagement.Domain.ValueObjects;

namespace FamilyManagement.Application.UseCases.Transactions.Services;

public sealed class WorkingCapitalContributionService
{
    private const decimal ContributionRate = 0.10m;

    private readonly IUnitOfWork _unitOfWork;

    public WorkingCapitalContributionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> ApplyAsync(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        // Nesta versão, o fundo recebe apenas receitas familiares.
        if (transaction.Type != TransactionType.Income ||
            !transaction.FamilyId.HasValue)
        {
            return Result.Success();
        }

        if (!transaction.IsActive || !transaction.IsConfirmed)
        {
            return Error.BusinessRule(
                "WorkingCapital.UnconfirmedIncome",
                "A contribuição exige uma receita ativa e confirmada.");
        }

        var existingImpact =
            await _unitOfWork.GoalContributionImpacts
                .GetByTransactionIdAsync(transaction.Id);

        if (existingImpact?.IsApplied == true)
        {
            return Error.Conflict(
                "WorkingCapital.AlreadyApplied",
                "Esta receita já contribuiu para o fundo de maneio.");
        }

        var contributionValue = decimal.Round(
            transaction.Amount.Amount * ContributionRate,
            2,
            MidpointRounding.AwayFromZero);

        // Valores muito pequenos podem arredondar para zero.
        if (contributionValue == 0m)
            return Result.Success();

        var key = Goal.GetWorkingCapitalKey(
            transaction.FamilyId.Value,
            transaction.Amount.Currency);

        var matches = (
            await _unitOfWork.Goals.FindAsync(
                goal => goal.WorkingCapitalKey == key))
            .ToList();

        if (matches.Count > 1)
        {
            return Error.Conflict(
                "WorkingCapital.DuplicateFund",
                "Existe mais de um fundo de maneio para esta família e moeda.");
        }

        Goal fund;

        if (matches.Count == 0)
        {
            var createResult = Goal.CreateWorkingCapitalFund(
                transaction.FamilyId.Value,
                transaction.UserId,
                transaction.Amount.Currency);

            if (createResult.IsFailure)
                return createResult.Error;

            fund = createResult.Value;

            await _unitOfWork.Goals.AddAsync(fund);
        }
        else
        {
            // FindAsync devolve entidades sem tracking.
            // Obtemos a instância rastreada antes de a modificar.
            var trackedFund = await _unitOfWork.Goals
                .GetByIdAsync(matches[0].Id);

            if (trackedFund is null)
            {
                return Error.NotFound(
                    "WorkingCapital.NotFound",
                    "O fundo de maneio não foi encontrado.");
            }

            fund = trackedFund;
        }

        if (!fund.IsActive)
        {
            return Error.BusinessRule(
                "WorkingCapital.Inactive",
                "O fundo de maneio está inativo.");
        }

        if (fund.FamilyId != transaction.FamilyId ||
            fund.TargetAmount.Currency != transaction.Amount.Currency)
        {
            return Error.BusinessRule(
                "WorkingCapital.InvalidScope",
                "O fundo não corresponde à família e moeda da receita.");
        }

        var contribution = Money.Create(
            contributionValue,
            transaction.Amount.Currency);

        var depositResult = fund.Deposit(contribution);

        if (depositResult.IsFailure)
            return depositResult;

        if (existingImpact is null)
        {
            var impactResult = GoalContributionImpact.Create(
                transaction.Id,
                fund.Id,
                contribution);

            if (impactResult.IsFailure)
                return impactResult.Error;

            await _unitOfWork.GoalContributionImpacts
                .AddAsync(impactResult.Value);
        }
        else
        {
            var reapplyResult = existingImpact.Reapply(
                fund.Id,
                contribution);

            if (reapplyResult.IsFailure)
                return reapplyResult;
        }

        // A gravação pertence ao handler que está a confirmar
        // ou atualizar a receita, dentro da mesma transação de BD.
        return Result.Success();
    }

    public async Task<Result> RevertAsync(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction.Type != TransactionType.Income)
            return Result.Success();

        var impact = await _unitOfWork.GoalContributionImpacts
            .GetByTransactionIdAsync(transaction.Id);

        // Receitas anteriores à funcionalidade podem não ter contribuição.
        if (impact is null)
            return Result.Success();

        if (!impact.IsApplied)
        {
            return Error.Conflict(
                "WorkingCapital.AlreadyReverted",
                "A contribuição desta receita já foi revertida.");
        }

        var fund = await _unitOfWork.Goals
            .GetByIdAsync(impact.GoalId);

        if (fund is null || !fund.IsWorkingCapitalFund)
        {
            return Error.BusinessRule(
                "WorkingCapital.InvalidHistory",
                "Não foi encontrado o fundo associado à contribuição.");
        }

        if (fund.FamilyId != transaction.FamilyId ||
            fund.TargetAmount.Currency != impact.Currency)
        {
            return Error.BusinessRule(
                "WorkingCapital.InvalidScope",
                "A contribuição não corresponde ao fundo da receita.");
        }

        var amount = Money.Create(
            impact.AppliedAmount,
            impact.Currency);

        // Retira o valor realmente aplicado, não um novo cálculo dos 10%.
        var withdrawResult = fund.Withdraw(amount);

        if (withdrawResult.IsFailure)
        {
            return Error.BusinessRule(
                "WorkingCapital.InsufficientFunds",
                "O fundo de maneio não tem saldo suficiente para reverter "
                + "esta contribuição. Regularize o saldo antes de corrigir "
                + "ou desconfirmar a receita.");
        }

        return impact.MarkReverted();
    }
}