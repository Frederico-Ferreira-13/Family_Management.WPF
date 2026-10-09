using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;
using FamilyManagement.Domain.Interfaces;

namespace FamilyManagement.Application.UseCases.Currencies.Commands.SetDefaultCurrency;

public sealed class SetDefaultCurrencyHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public SetDefaultCurrencyHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        SetDefaultCurrencyCommand command,
        CancellationToken cancellationToken = default)
    {
        var newDefault = await _unitOfWork.Currencies
            .GetByIdAsync(command.CurrencyId);

        if (newDefault is null)
        {
            return Error.NotFound(
                "Currency.NotFound",
                "Moeda não encontrada.");
        }

        if (!newDefault.IsActive)
        {
            return Error.BusinessRule(
                "Currency.Inactive",
                "Uma moeda inativa não pode ser definida como padrão.");
        }

        if (newDefault.IsDefault)
        {
            return Result.Success();
        }

        var markResult = newDefault.MarkAsDefault();

        if (markResult.IsFailure)
        {
            return markResult.Error;
        }

        var currentDefaults =
            await _unitOfWork.Currencies.FindAsync(c =>
                c.IsDefault &&
                c.Id != newDefault.Id);

        foreach (var currentDefault in currentDefaults)
        {
            currentDefault.UnmarkAsDefault();

            _unitOfWork.Currencies.Update(currentDefault);
        }

        _unitOfWork.Currencies.Update(newDefault);

        await _unitOfWork.CompleteAsync();

        return Result.Success();
    }
}