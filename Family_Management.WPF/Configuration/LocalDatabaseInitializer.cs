using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Family_Management.WPF.Configuration;

public static class LocalDatabaseInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var databasePath = LocalDatabasePaths.DatabasePath;

        var existingDatabase =
            File.Exists(databasePath)
            && new FileInfo(databasePath).Length > 0;

        var pendingMigrations =
            (await dbContext.Database.GetPendingMigrationsAsync())
            .ToArray();

        if (pendingMigrations.Length == 0)
        {
            return;
        }

        if (existingDatabase)
        {
            await CreateBackupAsync(databasePath);
        }

        // Se o backup falhar, a execução não chega a esta linha.
        await dbContext.Database.MigrateAsync();
    }

    private static async Task CreateBackupAsync(
        string databasePath)
    {
        var backupDirectory = Path.Combine(
            LocalDatabasePaths.ApplicationDirectory,
            "Backups");

        Directory.CreateDirectory(backupDirectory);

        var backupFileName =
            $"FamilyManagement-before-update-"
            + $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-"
            + $"{Guid.NewGuid():N}.db";

        var backupPath = Path.Combine(
            backupDirectory,
            backupFileName);

        var sourceConnectionString =
            new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false
            }.ToString();

        var backupConnectionString =
            new SqliteConnectionStringBuilder
            {
                DataSource = backupPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString();

        using var source =
            new SqliteConnection(sourceConnectionString);

        using var destination =
            new SqliteConnection(backupConnectionString);

        await source.OpenAsync();
        await destination.OpenAsync();

        source.BackupDatabase(destination);

        using var command = destination.CreateCommand();

        command.CommandText = "PRAGMA integrity_check;";

        var result = await command.ExecuteScalarAsync();

        if (!string.Equals(
                result?.ToString(),
                "ok",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A verificação da cópia de segurança falhou. "
                + "A atualização da base de dados foi interrompida.");
        }
    }
}