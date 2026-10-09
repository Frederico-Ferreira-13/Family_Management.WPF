using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace FamilyManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var startupDirectory = ResolveStartupDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(startupDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: false)
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string 'DefaultConnection' não está configurada.");
        }

        var outputDirectory = Path.Combine(
            startupDirectory,
            "bin",
            "Debug",
            "net10.0-windows");

        Directory.CreateDirectory(outputDirectory);

        var normalizedConnectionString =
            SqliteConnectionHelper.Normalize(
                connectionString,
                outputDirectory);

        var optionsBuilder =
            new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseSqlite(normalizedConnectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string ResolveStartupDirectory()
    {
        var directory = new DirectoryInfo(
            Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var startupDirectory = Path.Combine(
                directory.FullName,
                "Family_Management.WPF");

            if (File.Exists(Path.Combine(
                    startupDirectory,
                    "appsettings.json")))
            {
                return startupDirectory;
            }

            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "appsettings.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Não foi encontrado o appsettings.json do projeto WPF.");
    }
}