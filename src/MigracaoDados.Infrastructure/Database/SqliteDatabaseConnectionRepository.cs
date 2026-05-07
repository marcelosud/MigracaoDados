using Microsoft.Data.Sqlite;
using MigracaoDados.Application.Database;
using MigracaoDados.Application.Interfaces;
using MigracaoDados.Infrastructure.Security;

namespace MigracaoDados.Infrastructure.Database;

public sealed class SqliteDatabaseConnectionRepository : IDatabaseConnectionRepository
{
    private readonly ISecretProtector _secretProtector;
    private readonly string _databasePath;

    public SqliteDatabaseConnectionRepository(ISecretProtector secretProtector)
    {
        _secretProtector = secretProtector;
        _databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MigracaoDados",
            "MigracaoDados.db");
    }

    public async Task<DatabaseConnectionProfile?> GetAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Key, Provider, Server, DatabaseName, User, Password
            FROM DatabaseConnections
            WHERE Key = $key
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$key", key);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new DatabaseConnectionProfile(
            reader.GetString(0),
            Enum.Parse<DatabaseProviderType>(reader.GetString(1)),
            _secretProtector.Unprotect(reader.GetString(2)),
            _secretProtector.Unprotect(reader.GetString(3)),
            _secretProtector.Unprotect(reader.GetString(4)),
            _secretProtector.Unprotect(reader.GetString(5)));
    }

    public async Task SaveAsync(
        DatabaseConnectionProfile profile,
        CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO DatabaseConnections
                (Key, Provider, Server, DatabaseName, User, Password, UpdatedAt)
            VALUES
                ($key, $provider, $server, $databaseName, $user, $password, $updatedAt)
            ON CONFLICT(Key) DO UPDATE SET
                Provider = excluded.Provider,
                Server = excluded.Server,
                DatabaseName = excluded.DatabaseName,
                User = excluded.User,
                Password = excluded.Password,
                UpdatedAt = excluded.UpdatedAt;
            """;

        command.Parameters.AddWithValue("$key", profile.Key);
        command.Parameters.AddWithValue("$provider", profile.Provider.ToString());
        command.Parameters.AddWithValue("$server", _secretProtector.Protect(profile.Server));
        command.Parameters.AddWithValue("$databaseName", _secretProtector.Protect(profile.Database));
        command.Parameters.AddWithValue("$user", _secretProtector.Protect(profile.User));
        command.Parameters.AddWithValue("$password", _secretProtector.Protect(profile.Password));
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureDatabaseAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_databasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS DatabaseConnections
            (
                Key TEXT PRIMARY KEY,
                Provider TEXT NOT NULL,
                Server TEXT NOT NULL,
                DatabaseName TEXT NOT NULL,
                User TEXT NOT NULL,
                Password TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={_databasePath}");
    }
}
