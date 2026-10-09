using System;
using System.Collections.Generic;
using System.Text;

namespace FamilyManagement.Domain.Interfaces;

public interface IDbTransaction : IDisposable, IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();       
}