using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace FamilyManagement.Infrastructure.Persistence;

public static class LocalDatabasePaths
{
    public static string ApplicationDirectory
    {
        get
        {
            var localApplicationData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrWhiteSpace(localApplicationData))
            {
                throw new InvalidOperationException(
                    "Não foi possível localizar a pasta de dados do utilizador.");
            }

            return Path.Combine(
                localApplicationData,
                "FamilyManagement");
        }
    }

    public static string DataDirectory =>
        Path.Combine(
            ApplicationDirectory,
            "Data");

    public static string DatabasePath =>
        Path.Combine(
            DataDirectory,
            "FamilyManagement.db");

    public static string GetConnectionString()
    {
        Directory.CreateDirectory(DataDirectory);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        return builder.ToString();
    }
}