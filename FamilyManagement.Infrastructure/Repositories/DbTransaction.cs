using FamilyManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace FamilyManagement.Infrastructure.Repositories;

public sealed class DbTransaction : IDbTransaction
{
    private readonly IDbContextTransaction _transaction;

    public DbTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction
            ?? throw new ArgumentNullException(nameof(transaction));
    }

    public Task CommitAsync()
    {
        return _transaction.CommitAsync();
    }

    public Task RollbackAsync()
    {
        return _transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _transaction.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}