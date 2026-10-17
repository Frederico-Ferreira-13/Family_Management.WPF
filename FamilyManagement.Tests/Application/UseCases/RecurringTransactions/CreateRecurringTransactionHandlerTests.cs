
using FamilyManagement.Application.UseCases.RecurringTransactions.Commands.CreateRecurringTransaction;
using Xunit;

namespace FamilyManagement.Tests.Application.UseCases.RecurringTransactions;

public sealed class CreateRecurringTransactionHandlerTests
{
    [Fact]
    public void CreateRecurringTransactionHandler_DeveEstarDisponivel()
    {
        Assert.NotNull(typeof(CreateRecurringTransactionHandler));
    }
}
