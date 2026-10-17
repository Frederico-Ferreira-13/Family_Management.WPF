
using FamilyManagement.Application.UseCases.Transactions.Commands.CreateTransaction;
using Xunit;

namespace FamilyManagement.Tests.Application.UseCases.Transactions;

public sealed class CreateTransactionHandlerTests
{
    [Fact]
    public void CreateTransactionHandler_DeveEstarDisponivel()
    {
        Assert.NotNull(typeof(CreateTransactionHandler));
    }
}
