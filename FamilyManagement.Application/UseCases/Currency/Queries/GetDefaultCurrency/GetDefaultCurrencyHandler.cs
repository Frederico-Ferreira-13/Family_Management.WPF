using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Currencies.Queries.GetDefaultCurrency;

public sealed class GetDefaultCurrencyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDefaultCurrencyHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CurrencyDTO>> HandleAsync(
        GetDefaultCurrencyQuery query,
        CancellationToken cancellationToken = default)
    {
        var currencies = await _unitOfWork.Currencies
            .FindAsync(c =>
                c.IsDefault &&
                c.IsActive);

        var currency = currencies.FirstOrDefault();

        if (currency is null)
        {
            return Error.NotFound(
                "Currency.NoDefault",
                "Não foi definida nenhuma moeda padrão no sistema.");
        }

        return CurrencyMapper.ToDTO(currency);
    }
}