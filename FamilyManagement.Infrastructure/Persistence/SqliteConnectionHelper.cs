using Microsoft.Data.Sqlite;
using System.IO;

namespace FamilyManagement.Infrastructure.Persistence;

public static class SqliteConnectionHelper
{
    public static string Normalize(
        string connectionString,
        string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var builder = new SqliteConnectionStringBuilder(
            connectionString);

        if (builder.DataSource == ":memory:" ||
            string.IsNullOrWhiteSpace(builder.DataSource))
        {
            return builder.ToString();
        }

        if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.GetFullPath(
                Path.Combine(
                    baseDirectory,
                    builder.DataSource));
        }

        return builder.ToString();
    }
}