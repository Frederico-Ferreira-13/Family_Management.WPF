using System.Globalization;
using System.Text;
using FamilyManagement.Application.UseCases.Transactions.DTOs;
using Microsoft.Win32;
using System.IO;

namespace Family_Management.WPF.Services.Export;

public sealed class TransactionCsvExportService
    : ITransactionExportService
{
    public async Task<bool> ExportToCsvAsync(
        IEnumerable<TransactionDTO> transactions,
        DateTime referenceDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        var dialog = new SaveFileDialog
        {
            Title = "Exportar movimentos mensais",
            FileName =
                $"gestao-familiar-{referenceDate:yyyy-MM}.csv",
            Filter =
                "Ficheiro compatível com Excel (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true
        };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        var lines = new List<string>
        {
            "Data;Tipo;Descrição;Categoria;Conta;Utilizador;Valor;Moeda;Confirmada;Recorrente;Notas"
        };

        foreach (var transaction in transactions
                     .OrderBy(t => t.Date))
        {
            lines.Add(
                string.Join(
                    ';',
                    transaction.Date
                        .ToLocalTime()
                        .ToString(
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture),

                    Csv(transaction.TypeDisplay),

                    Csv(transaction.Description),

                    Csv(transaction.CategoryName),

                    Csv(transaction.AccountName),

                    Csv(transaction.UserName),

                    transaction.Amount.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture),

                    Csv(transaction.Currency),

                    transaction.IsConfirmed
                        ? "Sim"
                        : "Não",

                    transaction.IsRecurringGenerated
                        ? "Sim"
                        : "Não",

                    Csv(transaction.Notes)));
        }

        await File.WriteAllLinesAsync(
            dialog.FileName,
            lines,
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: true),
            cancellationToken);

        return true;
    }

    private static string Csv(
        string? value)
    {
        var escaped =
            (value ?? string.Empty)
            .Replace("\"", "\"\"");

        return $"\"{escaped}\"";
    }
}