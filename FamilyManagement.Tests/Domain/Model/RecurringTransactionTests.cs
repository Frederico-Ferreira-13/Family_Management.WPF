
using FamilyManagement.Domain.Model;
using Xunit;

namespace FamilyManagement.Tests.Domain.Model;

public sealed class RecurringTransactionTests
{
    [Fact]
    public void RecurringTransaction_DeveExistirNoDominio()
    {
        Assert.NotNull(typeof(RecurringTransaction));
    }
}
