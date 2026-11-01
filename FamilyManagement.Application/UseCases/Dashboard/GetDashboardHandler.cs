using System.Globalization;
using FamilyManagement.Application.UseCases.Goals.DTOs;
using FamilyManagement.Application.UseCases.Goals.Mapping;
using FamilyManagement.Application.UseCases.Investments.DTOs;
using FamilyManagement.Application.UseCases.Investments.Mapping;
using FamilyManagement.Application.UseCases.RecurringTransactions.DTOs;
using FamilyManagement.Application.UseCases.RecurringTransactions.Mapping;
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using FamilyManagement.Application.UseCases.Transactions.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Enums;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Dashboard;

public sealed record DashboardMetric(string Label, decimal Value);

public sealed record DashboardHistory(
    string Month,
    decimal Income,
    decimal Expenses)
{
    public decimal Balance => Income - Expenses;
}

public sealed record DashboardCurrency(
    string Currency,
    IReadOnlyList<DashboardMetric> Metrics,
    IReadOnlyList<DashboardHistory> History);

public sealed record DashboardSnapshot(
    IReadOnlyList<DashboardCurrency> Currencies,
    IReadOnlyList<TransactionDTO> Transactions,
    IReadOnlyList<RecurringTransactionDTO> Recurrences,
    IReadOnlyList<GoalDTO> Goals,
    IReadOnlyList<InvestmentDTO> Investments);

public sealed class GetDashboardHandler(IUnitOfWork unitOfWork)
{
    public async Task<Result<DashboardSnapshot>> HandleAsync(
        Guid userId,
        Guid? familyId,
        DateTime month,
        CancellationToken cancellationToken = default)
    {
        var user = (await unitOfWork.Users.FindAsync(
            u => u.Id == userId && u.IsActive,
            cancellationToken)).SingleOrDefault();

        if (user is null)
            return Error.Validation("Não existe um utilizador ativo.");

        var memberIds = new[] { userId };

        if (familyId.HasValue)
        {
            if (!await unitOfWork.Families.IsUserMemberOfFamilyAsync(
                    familyId.Value, userId)
                && !await unitOfWork.Families.IsUserCreatorOfFamilyAsync(
                    familyId.Value, userId))
            {
                return Error.Validation("Não tem acesso a esta família.");
            }

            memberIds = (await unitOfWork.Users.FindAsync(
                    u => u.IsActive && u.FamilyId == familyId,
                    cancellationToken))
                .Select(u => u.Id)
                .Append(userId)
                .Distinct()
                .ToArray();
        }

        // Consolidação familiar: registos familiares e pessoais dos membros.
        // Consultas sequenciais porque partilham o mesmo DbContext.
        var accounts = (await unitOfWork.Accounts.FindAsync(
            a => a.IsActive &&
                 ((familyId.HasValue && a.FamilyId == familyId) ||
                  (a.FamilyId == null && memberIds.Contains(a.UserId))),
            cancellationToken)).ToList();

        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);
        var historyStartUtc = start.AddMonths(-5).ToUniversalTime();
        var endUtc = end.ToUniversalTime();

        var transactions = (await unitOfWork.Transactions.FindAsync(
                t => t.IsActive &&
                     t.Date >= historyStartUtc &&
                     t.Date < endUtc &&
                     ((familyId.HasValue && t.FamilyId == familyId) ||
                      (t.FamilyId == null && memberIds.Contains(t.UserId))),
                cancellationToken))
            .Select(TransactionMapper.ToDTO)
            .Select(t => t with
            {
                Date = LocalDate(t.Date),
                ScheduledDate = t.ScheduledDate.HasValue
                    ? LocalDate(t.ScheduledDate.Value)
                    : null
            })
            .ToList();

        var categoryIds = transactions
            .Where(t => t.CategoryId.HasValue)
            .Select(t => t.CategoryId!.Value)
            .Distinct()
            .ToArray();

        var categories = (await unitOfWork.Categories.FindAsync(
                c => categoryIds.Contains(c.Id),
                cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);

        var accountNames = accounts.ToDictionary(a => a.Id, a => a.Name);

        transactions = transactions.Select(t => t with
        {
            CategoryName = t.CategoryId.HasValue
                ? categories.GetValueOrDefault(t.CategoryId.Value)
                : null,
            AccountName = t.AccountId.HasValue
                ? accountNames.GetValueOrDefault(t.AccountId.Value)
                : null,
            TargetAccountName = t.TargetAccountId.HasValue
                ? accountNames.GetValueOrDefault(t.TargetAccountId.Value)
                : null
        }).ToList();

        var recurrences = (await unitOfWork.RecurringTransactions
                .FindRecurringTransactionsWithDetailsAsync(
                    r => r.IsActive &&
                         ((familyId.HasValue && r.FamilyId == familyId) ||
                          (r.FamilyId == null && memberIds.Contains(r.UserId)))))
            .Select(RecurringTransactionMapper.ToDTO)
            .Select(r => r with
            {
                StartDate = LocalDate(r.StartDate),
                NextDueDate = LocalDate(r.NextDueDate),
                EndDate = r.EndDate.HasValue
                    ? LocalDate(r.EndDate.Value)
                    : null
            })
            .ToList();

        var goals = (await unitOfWork.Goals.FindGoalsWithDetailsAsync(
                g => g.IsActive &&
                     ((familyId.HasValue && g.FamilyId == familyId) ||
                      (g.FamilyId == null && memberIds.Contains(g.UserId)))))
            .Select(GoalMapper.ToDTO)
            .ToList();

        var investments = (await unitOfWork.Investments
                .FindInvestmentsWithDetailsAsync(
                    i => i.IsActive && memberIds.Contains(i.UserId)))
            .Select(InvestmentMapper.ToDTO)
            .ToList();

        cancellationToken.ThrowIfCancellationRequested();

        var current = transactions
            .Where(t => t.Date >= start && t.Date < end)
            .ToList();

        var currentIncomeIds = current
            .Where(transaction => transaction.Type == TransactionType.Income)
            .Select(transaction => transaction.Id)
            .ToArray();
        
        var workingCapitalGoalIds = goals
            .Where(goal => goal.IsWorkingCapitalFund)
            .Select(goal => goal.Id)
            .ToArray();

        var monthlyWorkingCapitalImpacts =
            await unitOfWork.GoalContributionImpacts.FindAsync(
                impact =>
                    impact.IsApplied &&
                    currentIncomeIds.Contains(impact.TransactionId) &&
                    workingCapitalGoalIds.Contains(impact.GoalId),
                cancellationToken);

        var codes = accounts.Select(a => a.Balance.Currency)
            .Concat(transactions.Select(t => t.Currency))
            .Concat(recurrences.Select(r => r.Currency))
            .Concat(goals.Select(g => g.Currency))
            .Concat(investments.Select(i => i.Currency))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var currencies = new List<DashboardCurrency>();

        foreach (var code in codes)
        {
            var tx = current.Where(t => t.Currency == code).ToList();
            var confirmed = tx.Where(t => t.IsConfirmed).ToList();

            decimal Sum(
                IEnumerable<TransactionDTO> rows,
                TransactionType type) =>
                rows.Where(t => t.Type == type).Sum(t => t.Amount);

            var income = Sum(confirmed, TransactionType.Income);
            var expenses = Sum(confirmed, TransactionType.Expense);

            // A previsão começa pelos movimentos já registados:
            // confirmados e pendentes.
            var plannedIncome = Sum(tx, TransactionType.Income);
            var plannedExpenses = Sum(tx, TransactionType.Expense);

            var occurrences = tx
                .Where(t => t.RecurringTransactionId.HasValue)
                .Select(t => (
                    t.RecurringTransactionId!.Value,
                    (t.ScheduledDate ?? t.Date).Date))
                .ToHashSet();

            // Acrescenta apenas ocorrências ainda sem movimento associado.
            foreach (var recurrence in recurrences.Where(r => r.Currency == code))
            {
                foreach (var date in Occurrences(recurrence, start))
                {
                    if (occurrences.Contains((recurrence.Id, date)))
                        continue;

                    if (recurrence.Type == TransactionType.Income)
                        plannedIncome += recurrence.Amount;

                    if (recurrence.Type == TransactionType.Expense)
                        plannedExpenses += recurrence.Amount;
                }
            }

            var currencyGoals = goals
                .Where(g => g.Currency == code)
                .ToList();

            var currencyInvestments = investments
                .Where(i => i.Currency == code)
                .ToList();

            var metrics = new List<DashboardMetric>
            {
                new("Saldo atual das contas",
                    accounts.Where(a => a.Balance.Currency == code)
                        .Sum(a => a.Balance.Amount)),

                new("Receitas confirmadas do mês", income),
                new("Despesas confirmadas do mês", expenses),
                new("Resultado confirmado do mês", income - expenses),

                new("Despesas recorrentes confirmadas",
                    confirmed.Where(t =>
                            t.Type == TransactionType.Expense &&
                            t.IsRecurringGenerated)
                        .Sum(t => t.Amount)),

                new("Outras despesas confirmadas",
                    confirmed.Where(t =>
                            t.Type == TransactionType.Expense &&
                            !t.IsRecurringGenerated)
                        .Sum(t => t.Amount)),

                new("Receitas previstas do mês (total)", plannedIncome),
                new("Despesas previstas do mês (total)", plannedExpenses),
                new("Resultado previsto do mês", plannedIncome - plannedExpenses),

                new("Transferências confirmadas do mês",
                    Sum(confirmed, TransactionType.Transfer)),

                new("Transferências pendentes do mês",
                    Sum(tx.Where(t => !t.IsConfirmed), TransactionType.Transfer)),

                new("Objetivos: valor acumulado atual",
                    currencyGoals.Sum(g => g.CurrentAmount)),

                new("Objetivos: valor em falta",
                    currencyGoals.Sum(g => g.RemainingAmount)),

                new("Investimentos: capital inicial",
                    currencyInvestments.Sum(i => i.InitialValue)),

                new("Investimentos: valor atual",
                    currencyInvestments.Sum(i => i.CurrentValue)),

                new("Investimentos: ganho/perda atual",
                    currencyInvestments.Sum(i => i.ProfitLoss)),

                new("Fundo de maneio: saldo",
                    currencyGoals
                        .Where(goal => goal.IsWorkingCapitalFund)
                        .Sum(goal => goal.CurrentAmount)),

                new("Fundo de maneio: reservado no mês",
                    monthlyWorkingCapitalImpacts
                        .Where(impact => impact.Currency == code)
                        .Sum(impact => impact.AppliedAmount))
            };

            var history = Enumerable.Range(-5, 6).Select(offset =>
            {
                var first = start.AddMonths(offset);
                var next = first.AddMonths(1);

                var rows = transactions.Where(t =>
                    t.Currency == code &&
                    t.IsConfirmed &&
                    t.Date >= first &&
                    t.Date < next).ToList();

                return new DashboardHistory(
                    first.ToString(
                        "MMM yyyy",
                        CultureInfo.GetCultureInfo("pt-PT")),
                    Sum(rows, TransactionType.Income),
                    Sum(rows, TransactionType.Expense));
            }).ToList();

            currencies.Add(new DashboardCurrency(code, metrics, history));
        }

        return new DashboardSnapshot(
            currencies,
            current,
            recurrences,
            goals,
            investments);
    }

    // O conversor de persistência existente guarda DateTime em UTC.
    private static DateTime LocalDate(DateTime value) =>
        (value.Kind == DateTimeKind.Utc
            ? value.ToLocalTime()
            : value).Date;

    public static IEnumerable<DateTime> Occurrences(
        RecurringTransactionDTO recurrence,
        DateTime month)
    {
        if (!recurrence.IsActive)
            yield break;

        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);
        var date = recurrence.NextDueDate.Date;
        var anchor = LocalDate(
            recurrence.ScheduleAnchorDate ?? recurrence.StartDate);

        var days = recurrence.Frequency switch
        {
            Frequency.Daily => 1,
            Frequency.Weekly => 7,
            Frequency.Biweekly => 14,
            _ => 0
        };

        var months = recurrence.Frequency switch
        {
            Frequency.Monthly => 1,
            Frequency.Quarterly => 3,
            Frequency.Semiannually => 6,
            Frequency.Annually => 12,
            _ => 0
        };

        if (days == 0 && months == 0)
        {
            throw new InvalidOperationException(
                "Frequência de recorrência inválida.");
        }

        if (date < start && days > 0)
        {
            date = date.AddDays(
                Math.Ceiling((start - date).TotalDays / days) * days);
        }

        while (date < end &&
               (!recurrence.EndDate.HasValue ||
                date <= recurrence.EndDate.Value.Date))
        {
            if (date >= start && date >= anchor)
                yield return date;

            if (days > 0)
            {
                date = date.AddDays(days);
            }
            else
            {
                var elapsed =
                    (date.Year - anchor.Year) * 12 +
                    date.Month - anchor.Month;

                // Preserva o dia original: 31 jan → 28 fev → 31 mar.
                date = anchor.AddMonths(elapsed + months);
            }
        }
    }
}