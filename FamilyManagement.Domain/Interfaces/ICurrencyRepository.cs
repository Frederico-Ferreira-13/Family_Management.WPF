using FamilyManagement.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace FamilyManagement.Domain.Interfaces;

public interface ICurrencyRepository : IRepository<Currency>
{
    Task<Currency?> GetByCodeAsync(string code);
    Task<Currency?> GetDefaultCurrencyAsync();

    Task<IEnumerable<Currency>> GetAllActiveAsync();
    Task<IEnumerable<Currency>> GetAllCurrenciesAsync();
    Task<Currency?> GetActiveByIdAsync(Guid currencyId);

    Task<bool> ExistsByCodeAsync(string code);
}