using System;
using System.Linq;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.ValueObjects;

/// <summary>
/// Criei esta classe para representar um valor monetário juntamente
/// com a moeda a que pertence.
/// </summary>
/// <remarks>
/// Optei por juntar o montante e a moeda para evitar trabalhar
/// com números cujo significado financeiro não esteja definido.
///
/// Utilizo um record para comparar estes objetos pelos seus valores.
/// As propriedades apenas permitem leitura: quando faço uma operação,
/// devolvo um novo Money em vez de alterar o objeto original.
///
/// Nesta implementação, permito valores iguais a zero, mas não negativos.
/// A distinção entre uma receita e uma despesa pertence ao movimento
/// financeiro que utiliza este valor.
/// </remarks>
public sealed record Money
{
    /// <summary>
    /// Utilizo decimal para representar o montante, por ser adequado
    /// a cálculos financeiros com representação decimal.
    /// </summary>
    /// <remarks>
    /// Esta propriedade não limita o valor a duas casas decimais.
    /// Não faço arredondamentos automaticamente nesta classe.
    /// </remarks>
    public decimal Amount { get; }

    /// <summary>
    /// Guardo o código da moeda normalizado em maiúsculas,
    /// como EUR ou USD.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Mantive o construtor privado para controlar a criação
    /// dos objetos através dos métodos desta classe.
    /// </summary>
    /// <param name="amount">Montante já validado.</param>
    /// <param name="currency">Código da moeda já normalizado.</param>
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Criei este método para validar os dados e tentar construir
    /// um valor monetário, devolvendo um Result com sucesso ou erro.
    /// </summary>
    /// <param name="amount">Montante, que não pode ser negativo.</param>
    /// <param name="currency">
    /// Código da moeda. Utilizo EUR quando o argumento é omitido.
    /// </param>
    /// <returns>
    /// O Money criado ou um erro que explica a validação falhada.
    /// </returns>
    public static Result<Money> TryCreate(
        decimal amount,
        string? currency = "EUR")
    {
        // Permito zero para representar, por exemplo,
        // o saldo inicial de um objetivo.
        if (amount < 0)
            return DomainErrors.Money.InvalidAmount;

        // Normalizo o código para que "eur", "EUR" e " EUR "
        // tenham a mesma representação dentro da aplicação.
        var normalizedCurrency = currency?
            .Trim()
            .ToUpperInvariant();

        // Valido apenas a estrutura do código: três letras de A a Z.
        // Esta verificação não confirma que a moeda existe
        // nem que está ativa na base de dados.
        if (normalizedCurrency is null ||
            normalizedCurrency.Length != 3 ||
            !normalizedCurrency.All(
                character => character is >= 'A' and <= 'Z'))
        {
            return DomainErrors.Money.InvalidCurrency;
        }

        return new Money(
            amount,
            normalizedCurrency);
    }


    /// <summary>
    /// Disponibilizei esta alternativa para criar um Money diretamente,
    /// lançando uma exceção quando os dados são inválidos.
    /// </summary>
    /// <param name="amount">Montante a representar.</param>
    /// <param name="currency">Código da moeda, por omissão EUR.</param>
    /// <returns>O valor monetário criado.</returns>
    /// <exception cref="ArgumentException">
    /// O montante ou o código da moeda são inválidos.
    /// </exception>
    /// <remarks>
    /// Reutilizo TryCreate para manter as regras de validação
    /// num único local.
    ///
    /// Quando a falha é esperada, como na validação dos dados
    /// de um formulário, TryCreate permite tratá-la através de Result.
    /// </remarks>
    public static Money Create(
        decimal amount,
        string currency = "EUR")
    {
        var result = TryCreate(amount, currency);

        if (result.IsFailure)
        {
            throw new ArgumentException(
                result.Error.Message);
        }

        return result.Value;
    }

    /// <summary>
    /// Criei este método para somar dois valores monetários,
    /// verificando a moeda e os limites do tipo decimal.
    /// </summary>
    /// <param name="other">Valor que pretendo adicionar.</param>
    /// <returns>
    /// Um novo Money com a soma ou um erro que impede a operação.
    /// </returns>
    public Result<Money> TryAdd(Money? other)
    {
        if (other is null)
            return DomainErrors.Money.MissingAmount;

        // Não somo moedas diferentes porque esta classe
        // não faz conversões cambiais.
        if (Currency != other.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        // Verifico o limite antes de executar a soma.
        // Como os montantes são não negativos, esta comparação
        // permite detetar se o resultado ultrapassaria decimal.MaxValue.
        if (other.Amount > decimal.MaxValue - Amount)
            return DomainErrors.Money.Overflow;

        // Devolvo um novo objeto para preservar os valores originais.
        return new Money(
            Amount + other.Amount,
            Currency);
    }

    /// <summary>
    /// Criei este método para subtrair um valor monetário,
    /// garantindo que a moeda coincide e que o resultado não é negativo.
    /// </summary>
    /// <param name="other">Valor que pretendo subtrair.</param>
    /// <returns>
    /// Um novo Money com a diferença ou um erro que impede a operação.
    /// </returns>
    public Result<Money> TrySubtract(Money? other)
    {
        if (other is null)
            return DomainErrors.Money.MissingAmount;

        if (Currency != other.Currency)
            return DomainErrors.Money.CurrencyMismatch;

        // Mantenho a regra de que um Money não representa
        // um montante negativo.
        if (other.Amount > Amount)
            return DomainErrors.Money.NegativeResult;

        return new Money(
            Amount - other.Amount,
            Currency);
    }

    /// <summary>
    /// Disponibilizei uma forma de obter diretamente o resultado
    /// da soma, lançando uma exceção se a operação não for válida.
    /// </summary>
    /// <param name="other">Valor que pretendo adicionar.</param>
    /// <returns>Um novo Money com o resultado da soma.</returns>
    /// <exception cref="InvalidOperationException">
    /// Falta o valor, as moedas são diferentes ou ocorre
    /// uma ultrapassagem do limite do tipo decimal.
    /// </exception>
    public Money Add(Money other)
    {
        // Reutilizo TryAdd para não duplicar as regras da operação.
        var result = TryAdd(other);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                result.Error.Message);
        }

        return result.Value;
    }

    /// <summary>
    /// Disponibilizei uma forma de obter diretamente o resultado
    /// da subtração, lançando uma exceção se a operação não for válida.
    /// </summary>
    /// <param name="other">Valor que pretendo subtrair.</param>
    /// <returns>Um novo Money com o resultado da subtração.</returns>
    /// <exception cref="InvalidOperationException">
    /// Falta o valor, as moedas são diferentes ou o resultado
    /// seria negativo.
    /// </exception>
    public Money Subtract(Money other)
    {
        var result = TrySubtract(other);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                result.Error.Message);
        }

        return result.Value;
    }
}