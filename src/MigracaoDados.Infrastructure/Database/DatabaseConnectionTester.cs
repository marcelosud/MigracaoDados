using Microsoft.Data.SqlClient;
using MigracaoDados.Application.Database;
using MigracaoDados.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace MigracaoDados.Infrastructure.Database;

public sealed class DatabaseConnectionTester : IDatabaseConnectionTester
{
    public async Task<DatabaseConnectionTestResult> TestAsync(
        DatabaseConnectionParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateRequiredParameters(parameters);

            switch (parameters.Provider)
            {
                case DatabaseProviderType.SqlServer:
                    await TestSqlServerAsync(parameters, cancellationToken);
                    break;
                case DatabaseProviderType.Oracle:
                    await TestOracleAsync(parameters, cancellationToken);
                    break;
                default:
                    return new DatabaseConnectionTestResult(false, "Tipo de banco de dados nao suportado.");
            }

            return new DatabaseConnectionTestResult(true, "Conexao realizada com sucesso.");
        }
        catch (Exception exception)
        {
            return new DatabaseConnectionTestResult(false, $"Falha ao conectar: {exception.Message}");
        }
    }

    private static void ValidateRequiredParameters(DatabaseConnectionParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters.Server))
        {
            throw new ArgumentException("Informe o servidor.");
        }

        if (string.IsNullOrWhiteSpace(parameters.User))
        {
            throw new ArgumentException("Informe o usuario.");
        }

        if (string.IsNullOrWhiteSpace(parameters.Password))
        {
            throw new ArgumentException("Informe a senha.");
        }

    }

    private static async Task TestSqlServerAsync(
        DatabaseConnectionParameters parameters,
        CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = parameters.Server,
            InitialCatalog = parameters.Database,
            UserID = parameters.User,
            Password = parameters.Password,
            TrustServerCertificate = true,
            ConnectTimeout = 10
        };

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
    }

    private static async Task TestOracleAsync(
        DatabaseConnectionParameters parameters,
        CancellationToken cancellationToken)
    {
        var builder = new OracleConnectionStringBuilder
        {
            DataSource = string.IsNullOrWhiteSpace(parameters.Database)
                ? parameters.Server
                : $"{parameters.Server}/{parameters.Database}",
            UserID = parameters.User,
            Password = parameters.Password,
            ConnectionTimeout = 10
        };

        await using var connection = new OracleConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
    }
}
