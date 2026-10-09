using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Currencies.Queries.GetCurrencyByCode;

public sealed class GetCurrencyByCodeHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCurrencyByCodeHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CurrencyDTO>> HandleAsync(
        GetCurrencyByCodeQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Code))
        {
            return Error.Validation(
                "O código da moeda é obrigatório.");
        }

        var normalizedCode = query.Code
            .Trim()
            .ToUpperInvariant();

        var currencies = await _unitOfWork.Currencies
            .FindAsync(c => c.Code == normalizedCode);

        var currency = currencies.FirstOrDefault();

        if (currency is null)
        {
            return Error.NotFound(
                "Currency.NotFound",
                "Código de moeda não encontrado.");
        }

        return CurrencyMapper.ToDTO(currency);
    }
}