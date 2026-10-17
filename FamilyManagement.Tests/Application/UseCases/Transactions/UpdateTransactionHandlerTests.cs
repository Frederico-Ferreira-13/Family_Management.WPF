
using FamilyManagement.Application.UseCases.Transactions.Commands.UpdateTransaction;
using Xunit;

namespace FamilyManagement.Tests.Application.UseCases.Transactions;

public sealed class UpdateTransactionHandlerTests
{
    [Fact]
    public void UpdateTransactionHandler_DeveEstarDisponivel()
    {
        Assert.NotNull(typeof(UpdateTransactionHandler));
    }
}
