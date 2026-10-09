using FamilyManagement.Application.UseCases.Transactions.DTOs;

namespace Family_Management.WPF.Services.Export;

public interface ITransactionExportService
{
    Task<bool> ExportToCsvAsync(
        IEnumerable<TransactionDTO> transactions,
        DateTime referenceDate,
        CancellationToken cancellationToken = default);
}