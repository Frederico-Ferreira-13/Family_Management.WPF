using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Dashboard;

public sealed record UpcomingRecurringPaymentDTO(
    Guid RecurringTransactionId,
    DateTime Date,
    string Description,
    decimal Amount,
    string Currency,
    Frequency Frequency,
    bool IsGenerated,
    bool IsConfirmed)
{
    public string FrequencyDisplay =>
        Frequency == Frequency.Quarterly
            ? "Trimestral"
            : "Semestral";

    public string StatusDisplay =>
        IsConfirmed
            ? "Confirmado"
            : IsGenerated
                ? "Por pagar"
                : "Previsto";

    public string AmountDisplay =>
        Amount.ToString(
            "N2",
            CultureInfo.GetCultureInfo("pt-PT"))
        + " "
        + Currency;
}

public sealed class GetUpcomingRecurringPaymentsHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUpcomingRecurringPaymentsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<UpcomingRecurringPaymentDTO>>>
        HandleAsync(
            Guid userId,
            Guid? familyId,
            DateTime month,
            CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.FindAsync(
            user => user.Id == userId && user.IsActive,
            cancellationToken);

        if (!users.Any())
        {
            return Error.Validation(
                "Não existe um utilizador ativo.");
        }

        var memberIds = new[] { userId };

        if (familyId.HasValue)
        {
            var isMember =
                await _unitOfWork.Families.IsUserMemberOfFamilyAsync(
                    familyId.Value,
                    userId);

            var isCreator =
                await _unitOfWork.Families.IsUserCreatorOfFamilyAsync(
                    familyId.Value,
                    userId);

            if (!isMember && !isCreator)
            {
                return Error.Validation(
                    "Não tem acesso a esta família.");
            }

            var members = await _unitOfWork.Users.FindAsync(
                user => user.IsActive &&
                        user.FamilyId == familyId,
                cancellationToken);

            memberIds = members
                .Select(user => user.Id)
                .Append(userId)
                .Distinct()
                .ToArray();
        }

        // Incluímos também regras inativas:
        // podem ter terminado depois de gerar um pagamento pendente.
        var recurrences = (
            await _unitOfWork.RecurringTransactions.FindAsync(
                recurring =>
                    recurring.Type == TransactionType.Expense &&
                    (recurring.Frequency == Frequency.Quarterly ||
                     recurring.Frequency == Frequency.Semiannually) &&
                    (
                        (familyId.HasValue &&
                         recurring.FamilyId == familyId) ||
                        (recurring.FamilyId == null &&
                         memberIds.Contains(recurring.UserId))
                    ),
                cancellationToken))
            .ToList();

        if (recurrences.Count == 0)
        {
            return Result<IReadOnlyList<UpcomingRecurringPaymentDTO>>
                .Success(Array.Empty<UpcomingRecurringPaymentDTO>());
        }

        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);

        var startUtc = start.ToUniversalTime();
        var endUtc = end.ToUniversalTime();

        var recurringIds = recurrences
            .Select(recurring => recurring.Id)
            .ToArray();

        // Procuramos pela data atual e pela data prevista original.
        // Isto permite reconhecer pagamentos cuja data foi corrigida.
        var transactions = (
            await _unitOfWork.Transactions.FindAsync(
                transaction =>
                    transaction.IsActive &&
                    transaction.Type == TransactionType.Expense &&
                    transaction.RecurringTransactionId.HasValue &&
                    recurringIds.Contains(
                        transaction.RecurringTransactionId.Value) &&
                    (
                        (transaction.Date >= startUtc &&
                         transaction.Date < endUtc) ||
                        (
                            transaction.ScheduledDate.HasValue &&
                            transaction.ScheduledDate.Value >= startUtc &&
                            transaction.ScheduledDate.Value < endUtc
                        )
                    ) &&
                    (
                        (familyId.HasValue &&
                         transaction.FamilyId == familyId) ||
                        (transaction.FamilyId == null &&
                         memberIds.Contains(transaction.UserId))
                    ),
                cancellationToken))
            .ToList();

        cancellationToken.ThrowIfCancellationRequested();

        return Result<IReadOnlyList<UpcomingRecurringPaymentDTO>>
            .Success(BuildForMonth(
                recurrences,
                transactions,
                start));
    }

    public static IReadOnlyList<UpcomingRecurringPaymentDTO> BuildForMonth(
        IEnumerable<RecurringTransaction> recurrences,
        IEnumerable<Transaction> transactions,
        DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);

        var eligibleRecurrences = recurrences
            .Where(recurring =>
                recurring.Type == TransactionType.Expense &&
                (
                    recurring.Frequency == Frequency.Quarterly ||
                    recurring.Frequency == Frequency.Semiannually
                ))
            .ToDictionary(recurring => recurring.Id);

        var generatedTransactions = transactions
            .Where(transaction =>
                transaction.IsActive &&
                transaction.Type == TransactionType.Expense &&
                transaction.RecurringTransactionId.HasValue &&
                eligibleRecurrences.ContainsKey(
                    transaction.RecurringTransactionId.Value))
            .ToList();

        // A identidade da ocorrência mantém a data prevista original.
        var generatedOccurrences = generatedTransactions
            .Select(transaction => (
                transaction.RecurringTransactionId!.Value,
                LocalDate(
                    transaction.ScheduledDate ?? transaction.Date)))
            .ToHashSet();

        var payments = new List<UpcomingRecurringPaymentDTO>();

        // Movimentos já existentes usam a sua data atual.
        foreach (var transaction in generatedTransactions)
        {
            var paymentDate = LocalDate(transaction.Date);

            if (paymentDate < start || paymentDate >= end)
                continue;

            var recurring = eligibleRecurrences[
                transaction.RecurringTransactionId!.Value];

            payments.Add(new UpcomingRecurringPaymentDTO(
                RecurringTransactionId: recurring.Id,
                Date: paymentDate,
                Description: transaction.Description,
                Amount: transaction.Amount.Amount,
                Currency: transaction.Amount.Currency,
                Frequency: recurring.Frequency,
                IsGenerated: true,
                IsConfirmed: transaction.IsConfirmed));
        }

        // Só regras ativas acrescentam novas previsões.
        foreach (var recurring in eligibleRecurrences.Values
                     .Where(recurring => recurring.IsActive))
        {
            var dto = RecurringTransactionMapper.ToDTO(recurring);

            dto = dto with
            {
                StartDate = LocalDate(dto.StartDate),
                NextDueDate = LocalDate(dto.NextDueDate),
                EndDate = dto.EndDate.HasValue
                    ? LocalDate(dto.EndDate.Value)
                    : null,
                ScheduleAnchorDate = dto.ScheduleAnchorDate.HasValue
                    ? LocalDate(dto.ScheduleAnchorDate.Value)
                    : null
            };

            foreach (var date in GetDashboardHandler.Occurrences(
                         dto,
                         start))
            {
                if (generatedOccurrences.Contains((recurring.Id, date)))
                    continue;

                payments.Add(new UpcomingRecurringPaymentDTO(
                    RecurringTransactionId: recurring.Id,
                    Date: date,
                    Description: recurring.Description,
                    Amount: recurring.Amount.Amount,
                    Currency: recurring.Amount.Currency,
                    Frequency: recurring.Frequency,
                    IsGenerated: false,
                    IsConfirmed: false));
            }
        }

        return payments
            .OrderBy(payment => payment.Date)
            .ThenBy(payment => payment.Description)
            .ToList();
    }

    private static DateTime LocalDate(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value.ToLocalTime().Date
            : value.Date;
    }
}