using Microsoft.Data.Sqlite;

var databasePath = Path.GetFullPath(
    Path.Combine(
        "Family_Management.WPF",
        "bin",
        "Debug",
        "net10.0-windows",
        "FamilyManagement.db"));

if (!File.Exists(databasePath))
{
    Console.WriteLine(
        $"Base de dados não encontrada: {databasePath}");

    return;
}

Console.WriteLine($"Base de dados: {databasePath}");

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadOnly
}.ToString();

await using var connection =
    new SqliteConnection(connectionString);

await connection.OpenAsync();

// ============================================================
// 1. Estrutura das tabelas
// ============================================================

var tables = new[]
{
    "Families",
    "FinancialPlans",
    "Users",
    "Transactions",
    "Categories",
    "Accounts",
    "Budgets",
    "Goals",
    "RecurringTransactions"
};

foreach (var table in tables)
{
    Console.WriteLine();
    Console.WriteLine($"=== {table} ===");

    await using var command = connection.CreateCommand();

    command.CommandText =
        $"PRAGMA table_info(\"{table}\");";

    await using var reader =
        await command.ExecuteReaderAsync();

    var found = false;

    while (await reader.ReadAsync())
    {
        found = true;

        var columnName = reader.GetString(1);
        var columnType = reader.GetString(2);

        Console.WriteLine(
            $"{columnName}  {columnType}");
    }

    if (!found)
    {
        Console.WriteLine(
            "Tabela inexistente ou sem colunas.");
    }
}

// ============================================================
// 2. Confirmar RecurringTransactions.FamilyId
// ============================================================

Console.WriteLine();
Console.WriteLine(
    "=== Verificação de RecurringTransactions.FamilyId ===");

var familyIdExists = false;

await using (var command = connection.CreateCommand())
{
    command.CommandText =
        "PRAGMA table_info(\"RecurringTransactions\");";

    await using var reader =
        await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        var columnName = reader.GetString(1);

        if (string.Equals(
            columnName,
            "FamilyId",
            StringComparison.OrdinalIgnoreCase))
        {
            familyIdExists = true;
            break;
        }
    }
}

Console.WriteLine(
    familyIdExists
        ? "OK: A coluna FamilyId existe."
        : "ERRO: A coluna FamilyId não existe.");

// ============================================================
// 3. Migrations aplicadas
// ============================================================

Console.WriteLine();
Console.WriteLine("=== Migrations aplicadas ===");

try
{
    await using var command = connection.CreateCommand();

    command.CommandText = """
        SELECT MigrationId
        FROM "__EFMigrationsHistory"
        ORDER BY MigrationId;
        """;

    await using var reader =
        await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        Console.WriteLine(reader.GetString(0));
    }
}
catch (SqliteException exception)
{
    Console.WriteLine(
        $"Erro ao consultar migrations: {exception.Message}");
}

// ============================================================
// 4. Integridade referencial
// ============================================================

Console.WriteLine();
Console.WriteLine("=== Integridade referencial ===");

var foreignKeyErrors = 0;

await using (var command = connection.CreateCommand())
{
    command.CommandText = "PRAGMA foreign_key_check;";

    await using var reader =
        await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        foreignKeyErrors++;

        var table = reader.GetString(0);
        var rowId = reader.GetValue(1);
        var parentTable = reader.GetString(2);
        var foreignKeyId = reader.GetInt64(3);

        Console.WriteLine(
            $"Tabela: {table} | " +
            $"RowId: {rowId} | " +
            $"Tabela referenciada: {parentTable} | " +
            $"FK: {foreignKeyId}");
    }
}

Console.WriteLine(
    foreignKeyErrors == 0
        ? "Integridade referencial: OK"
        : $"Foram encontrados {foreignKeyErrors} problemas de integridade.");

// ============================================================
// 5. Resumo
// ============================================================

Console.WriteLine();
Console.WriteLine("=== Resumo do diagnóstico ===");

Console.WriteLine(
    $"RecurringTransactions.FamilyId: " +
    $"{(familyIdExists ? "OK" : "EM FALTA")}");

Console.WriteLine(
    $"Integridade referencial: " +
    $"{(foreignKeyErrors == 0 ? "OK" : "COM ERROS")}");

Console.WriteLine();
Console.WriteLine("Diagnóstico concluído.");