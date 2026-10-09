using System;
using System.Linq;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.Model;

public class Currency : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;

    public byte DecimalPlaces { get; private set; }
    public bool IsDefault { get; private set; }

    private Currency()
    {
    }

    private Currency(
        string code,
        string name,
        string symbol,
        byte decimalPlaces,
        bool isDefault)
    {
        Code = code;
        Name = name;
        Symbol = symbol;
        DecimalPlaces = decimalPlaces;
        IsDefault = isDefault;
    }

    public static Result<Currency> Create(
        string code,
        string name,
        string symbol,
        byte decimalPlaces = 2,
        bool isDefault = false)
    {
        var normalizedCode = code?
            .Trim()
            .ToUpperInvariant();

        if (normalizedCode is null ||
            normalizedCode.Length != 3 ||
            !normalizedCode.All(
                character => character is >= 'A' and <= 'Z'))
        {
            return Error.Validation(
                "O código da moeda deve conter três letras, por exemplo EUR.");
        }

        var detailsValidation = ValidateDetails(
            name,
            symbol,
            decimalPlaces);

        if (detailsValidation.IsFailure)
            return detailsValidation.Error;

        return new Currency(
            normalizedCode,
            name.Trim(),
            symbol.Trim(),
            decimalPlaces,
            isDefault);
    }

    public Result UpdateDetails(
        string name,
        string symbol,
        byte decimalPlaces)
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "currency.inactive",
                "A moeda está inativa.");
        }

        var validation = ValidateDetails(
            name,
            symbol,
            decimalPlaces);

        if (validation.IsFailure)
            return validation;

        Name = name.Trim();
        Symbol = symbol.Trim();
        DecimalPlaces = decimalPlaces;

        Touch();

        return Result.Success();
    }

    public Result Disable()
    {
        if (IsDefault)
        {
            return Error.BusinessRule(
                "currency.default_cannot_be_disabled",
                "A moeda padrão não pode ser desativada.");
        }

        if (!IsActive)
            return Result.Success();

        base.Deactivate();

        return Result.Success();
    }

    public override void Deactivate()
    {
        var result = Disable();

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                result.Error.Message);
        }
    }

    public Result MarkAsDefault()
    {
        if (!IsActive)
        {
            return Error.Conflict(
                "currency.inactive",
                "Uma moeda inativa não pode ser definida como padrão.");
        }

        if (IsDefault)
            return Result.Success();

        IsDefault = true;
        Touch();

        return Result.Success();
    }

    public void UnmarkAsDefault()
    {
        if (!IsDefault)
            return;

        IsDefault = false;
        Touch();
    }

    private static Result ValidateDetails(
        string name,
        string symbol,
        byte decimalPlaces)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(
                "O nome da moeda é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(symbol))
        {
            return Error.Validation(
                "O símbolo da moeda é obrigatório.");
        }

        if (decimalPlaces > 4)
        {
            return Error.Validation(
                "A moeda pode ter, no máximo, quatro casas decimais.");
        }

        return Result.Success();
    }
}