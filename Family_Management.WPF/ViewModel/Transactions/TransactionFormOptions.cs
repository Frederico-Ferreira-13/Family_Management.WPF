using FamilyManagement.Domain.Enums;

namespace Family_Management.WPF.ViewModel.Transactions;

public sealed record TransactionTypeOption(
    TransactionType Value,
    string DisplayName);

public sealed record FrequencyOption(
    Frequency Value,
    string DisplayName);

public static class TransactionFormOptions
{
    public static IReadOnlyList<TransactionTypeOption>
        TransactionTypes { get; } =
        new List<TransactionTypeOption>
        {
            new(
                TransactionType.Income,
                "Receita"),

            new(
                TransactionType.Expense,
                "Despesa"),

            new(
                TransactionType.Transfer,
                "Transferência")
        };

    public static IReadOnlyList<FrequencyOption>
        Frequencies { get; } =
        new List<FrequencyOption>
        {
            new(
                Frequency.Daily,
                "Diária"),

            new(
                Frequency.Weekly,
                "Semanal"),

            new(
                Frequency.Biweekly,
                "Quinzenal"),

            new(
                Frequency.Monthly,
                "Mensal"),

            new(
                Frequency.Quarterly,
                "Trimestral"),

            new(
                Frequency.Semiannually,
                "Semestral"),

            new(
                Frequency.Annually,
                "Anual")
        };

    public static CategoryType GetCategoryType(
        TransactionType transactionType)
    {
        return transactionType switch
        {
            TransactionType.Income =>
                CategoryType.Income,

            TransactionType.Expense =>
                CategoryType.Expense,

            TransactionType.Transfer =>
                CategoryType.Transfer,

            _ =>
                CategoryType.NotSpecified
        };
    }

    public static string GetTransactionTypeDisplay(
        TransactionType transactionType)
    {
        return TransactionTypes
            .FirstOrDefault(option =>
                option.Value == transactionType)
            ?.DisplayName
            ?? transactionType.ToString();
    }

    public static string GetFrequencyDisplay(
        Frequency frequency)
    {
        return Frequencies
            .FirstOrDefault(option =>
                option.Value == frequency)
            ?.DisplayName
            ?? frequency.ToString();
    }
}