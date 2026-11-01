using System;
using System.Linq;
using System.Net.Mail;
using FamilyManagement.Domain.Common;
using FamilyManagement.Domain.Errors;

namespace FamilyManagement.Domain.ValueObjects;

/// <summary>
/// Criei esta classe para representar um endereço de e-mail
/// validado e normalizado dentro do domínio.
/// </summary>
/// <remarks>
/// Em vez de repetir a validação do e-mail em várias entidades,
/// concentrei aqui as regras necessárias à sua criação.
///
/// Utilizo um record para comparar os objetos pelo valor que guardam.
/// A propriedade apenas permite leitura, preservando o endereço
/// depois de o objeto ser criado.
///
/// Esta validação verifica o formato aceite pela aplicação.
/// Não confirma que a caixa de correio existe ou pertence ao utilizador.
/// </remarks>
public sealed record EmailAddress
{
     /// <summary>
    /// Guardo o endereço validado, sem espaços exteriores
    /// e convertido para minúsculas.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Mantive o construtor privado para encaminhar a criação
    /// do endereço através do método Create.
    /// </summary>
    /// <param name="value">Endereço já validado e normalizado.</param>
    private EmailAddress(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Criei este método para validar o endereço recebido
    /// antes de construir o objeto.
    /// </summary>
    /// <param name="email">Endereço introduzido pelo utilizador.</param>
    /// <returns>
    /// O EmailAddress criado ou um erro de validação.
    /// </returns>
    public static Result<EmailAddress> Create(string? email)
    {
        // Um endereço vazio não representa um e-mail utilizável
        // no contexto desta aplicação.
        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Validation(
                "O email é obrigatório.");
        }

        // Removo espaços exteriores que possam ter sido introduzidos
        // ao escrever ou copiar o endereço.
        var normalizedEmail = email.Trim();

        if (!IsValidEmail(normalizedEmail))
        {
            return Error.Validation(
                "O formato do email é inválido.");
        }

         // Nesta aplicação, optei por guardar todo o endereço
        // em minúsculas para uniformizar a sua representação.
        // Esta é uma regra de normalização da aplicação.
        return new EmailAddress(
            normalizedEmail.ToLowerInvariant());
    }

    /// <summary>
    /// Reuni neste método as verificações de formato
    /// aplicadas antes de criar um EmailAddress.
    /// </summary>
    /// <param name="email">
    /// Endereço não vazio, já sem espaços exteriores.
    /// </param>
    /// <returns>
    /// True quando o endereço cumpre todas as verificações;
    /// caso contrário, false.
    /// </returns>
    private static bool IsValidEmail(string email)
    {
        // Defini um limite de comprimento para os endereços
        // aceites pela aplicação.
        if (email.Length > 254)
            return false;

        // Não aceito espaços ou outros caracteres em branco
        // no interior do endereço.
        if (email.Any(char.IsWhiteSpace))
            return false;

        // Reutilizo o analisador do .NET em vez de tentar
        // reproduzir toda a análise com uma expressão regular.
        if (!MailAddress.TryCreate(email, out var parsedEmail))
            return false;

        // Exijo que o endereço analisado corresponda ao texto recebido.
        // Assim, não aceito uma representação com informação adicional,
        // como um nome de apresentação seguido do endereço.
        if (!string.Equals(
                parsedEmail.Address,
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = parsedEmail.Host;

        // Nesta aplicação, exijo um domínio com pelo menos um ponto.
        // Esta regra exclui endereços com domínios locais simples.
        if (!host.Contains('.'))
            return false;

        // Rejeito pontos nas extremidades do domínio
        // e sequências de pontos consecutivos.
        if (host.StartsWith('.') ||
            host.EndsWith('.') ||
            host.Contains(".."))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Redefini este método para que a representação textual
    /// do objeto seja o próprio endereço de e-mail.
    /// </summary>
    /// <returns>O endereço guardado em Value.</returns>    
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Disponibilizei a conversão implícita para string
    /// para facilitar a utilização do endereço onde é esperado texto.
    /// </summary>
    /// <param name="email">Objeto que contém o endereço.</param>
    /// <returns>O endereço guardado em Value.</returns>
    /// <exception cref="ArgumentNullException">
    /// O objeto recebido é null.
    /// </exception>
    /// <remarks>
    /// Esta conversão apenas lê o valor já existente.
    /// A criação a partir de texto continua a passar por Create,
    /// para executar as validações.
    /// </remarks>
    public static implicit operator string(EmailAddress email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return email.Value;
    }
}