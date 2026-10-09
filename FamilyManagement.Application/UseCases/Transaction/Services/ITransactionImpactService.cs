using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Model;

namespace FamilyManagement.Application.UseCases.Transactions.Services;

public interface ITransactionImpactService
{
    Task<Result> ApplyAsync(Transaction transaction);

    Task<Result> RevertAsync(Transaction transaction);
}