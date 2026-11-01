using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace Family_Management.WPF.Configuration;

public static class LocalAuthenticationConfiguration
{
    public static void AddTo(
        IConfigurationBuilder configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var securityDirectory = Path.Combine(
            LocalDatabasePaths.ApplicationDirectory,
            "Security");

        Directory.CreateDirectory(securityDirectory);

        var keyPath = Path.Combine(
            securityDirectory,
            "session-signing.key");

        EnsureKeyExists(keyPath);

        var securityKey = File.ReadAllText(
            keyPath,
            Encoding.UTF8).Trim();

        ValidateKey(securityKey);

        configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["JwtSettings:SecurityKey"] = securityKey,
                ["JwtSettings:Issuer"] = "FamilyManagement.Local",
                ["JwtSettings:Audience"] = "FamilyManagement.Desktop",
                ["JwtSettings:ExpirationMinutes"] = "1440"
            });
    }

    private static void EnsureKeyExists(string keyPath)
    {
        if (File.Exists(keyPath))
        {
            return;
        }

        var securityKey = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        var temporaryPath =
            keyPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            File.WriteAllText(
                temporaryPath,
                securityKey,
                new UTF8Encoding(false));

            try
            {
                // Não substitui uma chave que outra instância
                // possa ter criado entretanto.
                File.Move(
                    temporaryPath,
                    keyPath,
                    overwrite: false);
            }
            catch (IOException) when (File.Exists(keyPath))
            {
                // A chave existente será lida e validada.
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void ValidateKey(string securityKey)
    {
        byte[] keyBytes;

        try
        {
            keyBytes = Convert.FromBase64String(securityKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "O ficheiro de configuração da sessão é inválido.",
                exception);
        }

        if (keyBytes.Length != 64)
        {
            throw new InvalidOperationException(
                "O ficheiro de configuração da sessão tem um tamanho inválido.");
        }
    }
}