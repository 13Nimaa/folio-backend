using Microsoft.Data.Sqlite;

if (args.Length != 3 || args[0] is not ("inspect" or "promote"))
{
    Console.Error.WriteLine("Usage: <inspect|promote> <database-path> <email>");
    return 2;
}

SQLitePCL.Batteries_V2.Init();

var operation = args[0];
var databasePath = Path.GetFullPath(args[1]);
var email = args[2].Trim();

await using var connection = new SqliteConnection(
    $"Data Source={databasePath};Mode=ReadWrite");
await connection.OpenAsync();

if (operation == "promote")
{
    await using var transaction = await connection.BeginTransactionAsync();
    await using var update = connection.CreateCommand();
    update.Transaction = (SqliteTransaction)transaction;
    update.CommandText = """
        UPDATE Users
        SET Role = 'Admin'
        WHERE lower(Email) = lower($email);
        """;
    update.Parameters.AddWithValue("$email", email);

    var affectedRows = await update.ExecuteNonQueryAsync();
    await transaction.CommitAsync();
    Console.WriteLine($"AFFECTED_ROWS={affectedRows}");
}

await using var select = connection.CreateCommand();
select.CommandText = """
    SELECT Id, Email, Role
    FROM Users
    WHERE lower(Email) = lower($email);
    """;
select.Parameters.AddWithValue("$email", email);

await using var reader = await select.ExecuteReaderAsync();
if (!await reader.ReadAsync())
{
    Console.WriteLine("USER_NOT_FOUND");
    return 3;
}

Console.WriteLine($"ID={reader.GetInt32(0)}");
Console.WriteLine($"EMAIL={reader.GetString(1)}");
Console.WriteLine($"ROLE={reader.GetString(2)}");
return 0;
