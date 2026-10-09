using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Currencies.Queries.GetCurrencies;

public sealed class GetCurrenciesHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCurrenciesHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<CurrencyDTO>>> HandleAsync(
        GetCurrenciesQuery query,
        CancellationToken cancellationToken = default)
    {
        var currencies = query.OnlyActive
            ? await _unitOfWork.Currencies.FindAsync(c => c.IsActive)
            : await _unitOfWork.Currencies.GetAllAsync();

        var result = currencies
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Code)
            .Select(CurrencyMapper.ToDTO)
            .ToList();

        return result;
    }
}