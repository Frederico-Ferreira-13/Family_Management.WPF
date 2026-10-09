using FamilyManagement.Application.UseCases.Currencies.DTOs;
using FamilyManagement.Application.UseCases.Currencies.Mapping;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Currencies.Queries.GetCurrencyById;

public sealed class GetCurrencyByIdHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCurrencyByIdHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CurrencyDTO>> HandleAsync(
        GetCurrencyByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CurrencyId == Guid.Empty)
        {
            return Error.Validation(
                "O identificador da moeda é obrigatório.");
        }

        var currency = await _unitOfWork.Currencies
            .GetByIdAsync(query.CurrencyId);

        if (currency is null)
        {
            return Error.NotFound(
                "Currency.NotFound",
                "Moeda não encontrada.");
        }

        return CurrencyMapper.ToDTO(currency);
    }
}