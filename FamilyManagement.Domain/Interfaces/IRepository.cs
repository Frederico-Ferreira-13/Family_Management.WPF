using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FamilyManagement.Domain.Common;

namespace FamilyManagement.Domain.Interfaces;

public interface IRepository<T>
    where T : BaseEntity
{
    Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<IEnumerable<T>> GetAllAsync(
        CancellationToken ct = default);

    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    Task AddAsync(
        T entity,
        CancellationToken ct = default);

    void Update(T entity);

    void Remove(T entity);
}