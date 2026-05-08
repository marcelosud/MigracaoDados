using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using MigracaoDados.Application.Database;
using MigracaoDados.Application.Interfaces;
using MigracaoDados.Application.Migration;
using MigracaoDados.Application.Session;

namespace MigracaoDados.Infrastructure.Database;

public sealed class SqlServerStoredProceduresMigrationExecutor : IDatabaseMigrationExecutor
{
    public async Task<DatabaseMigrationExecutionResult> ExecuteAsync(
        DatabaseConnectionParameters connectionParameters,
        MigrationSessionParameters sessionParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var em700DtAnt = ParseDateParameter(sessionParameters.EM700_DT_ANT);
            var em700DtAtu = ParseDateParameter(sessionParameters.ENT_DT_MOVTO);
            var em700DtPrx = ParseDateParameter(sessionParameters.EM700_DT_PRX);
            var csvDirectoryPath = EnsureTrailingDirectorySeparator(sessionParameters.ENT_PATH_CSV);

            await using var connection = new SqlConnection(CreateConnectionString(connectionParameters));
            await connection.OpenAsync(cancellationToken);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                await ExecuteAsync(connection, transaction, "SET XACT_ABORT ON;", cancellationToken: cancellationToken);

                await ExecuteAsync(
                    connection,
                    transaction,
                    """
                    IF OBJECT_ID('tempdb..#BACKUP_EM700') IS NOT NULL
                        DROP TABLE #BACKUP_EM700;

                    SELECT
                        EM700_DT_ANT,
                        EM700_DT_ATU,
                        EM700_DT_PRX
                    INTO #BACKUP_EM700
                    FROM EM700;
                    """,
                    cancellationToken: cancellationToken);

                await ExecuteAsync(
                    connection,
                    transaction,
                    """
                    UPDATE EM700
                    SET
                        EM700_DT_ANT = @EM700_DT_ANT,
                        EM700_DT_ATU = @EM700_DT_ATU,
                        EM700_DT_PRX = @EM700_DT_PRX,
                        EM700_ST_PROC = 'A';
                    """,
                    new
                    {
                        EM700_DT_ANT = em700DtAnt,
                        EM700_DT_ATU = em700DtAtu,
                        EM700_DT_PRX = em700DtPrx
                    },
                    cancellationToken);

                var contractsWithoutTypeBefore = await QuerySingleAsync<int>(
                    connection,
                    transaction,
                    "SELECT COUNT(1) FROM EM01 WHERE EM01_TP_CONTRA IS NULL;",
                    cancellationToken: cancellationToken);

                await ExecuteAsync(
                    connection,
                    transaction,
                    """
                    EXEC P040100996
                        @ENT_NR_VRS    = @ENT_NR_VRS,
                        @ENT_DT_MOVTO  = @ENT_DT_MOVTO,
                        @ENT_NR_LOTE   = @ENT_NR_LOTE,
                        @ENT_NR_CTRTER = @ENT_NR_CTRTER,
                        @ENT_ID_CTRTER = @ENT_ID_CTRTER,
                        @ENT_ID_GARANT = @ENT_ID_GARANT;
                    """,
                    CreateP040100996Parameters(em700DtAtu),
                    cancellationToken);

                await ExecuteAsync(
                    connection,
                    transaction,
                    """
                    EXEC dbo.P040100999
                        @ENT_NR_VRS,
                        @ENT_ID_TPAMB,
                        @ENT_DT_MOVTO,
                        @ENT_NR_LOTE,
                        @ENT_NR_CTRTER,
                        @ENT_ID_IMPCSV,
                        @ENT_DS_DIRCSV,
                        @ENT_ID_CTRPRE,
                        @ENT_ID_BAIXAS,
                        @ENT_ID_LIBERA,
                        @ENT_ID_CLIATU,
                        @ENT_ID_TOTCLI,
                        @ENT_ID_QUITA,
                        @ENT_ID_GARANT,
                        @ENT_ID_FINALI,
                        @ENT_CD_OPESIS;
                    """,
                    CreateP040100999Parameters(em700DtAtu, csvDirectoryPath),
                    cancellationToken);

                var logRecordCount = await QuerySingleAsync<int>(
                    connection,
                    transaction,
                    "SELECT COUNT(1) FROM TB_LOGS_REGISTRO_PROCESSO;",
                    cancellationToken: cancellationToken);

                var contractsWithoutTypeAfter = await QuerySingleAsync<int>(
                    connection,
                    transaction,
                    "SELECT COUNT(1) FROM EM01 WHERE EM01_TP_CONTRA IS NULL;",
                    cancellationToken: cancellationToken);

                await ExecuteAsync(
                    connection,
                    transaction,
                    """
                    UPDATE EM700
                    SET
                        EM700_DT_ANT = A.EM700_DT_ANT,
                        EM700_DT_ATU = A.EM700_DT_ATU,
                        EM700_DT_PRX = A.EM700_DT_PRX,
                        EM700_ST_PROC = 'A'
                    FROM #BACKUP_EM700 A;
                    """,
                    cancellationToken: cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return new DatabaseMigrationExecutionResult(
                    true,
                    $"Stored procedures executadas com sucesso no banco {connectionParameters.Database}. "
                    + $"Datas EM700: ANT={sessionParameters.EM700_DT_ANT}, ATU={sessionParameters.ENT_DT_MOVTO}, PRX={sessionParameters.EM700_DT_PRX}. "
                    + $"Diretorio CSV: {csvDirectoryPath}. "
                    + $"EM01_TP_CONTRA nulo antes/depois: {contractsWithoutTypeBefore}/{contractsWithoutTypeAfter}. "
                    + $"Registros em TB_LOGS_REGISTRO_PROCESSO: {logRecordCount}.");
            }
            catch (Exception exception)
            {
                await RollbackSafelyAsync(transaction);

                return new DatabaseMigrationExecutionResult(
                    false,
                    $"Falha ao executar as stored procedures de migracao: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            return new DatabaseMigrationExecutionResult(
                false,
                $"Falha ao preparar a execucao da Etapa 3: {exception.Message}");
        }
    }

    private static DynamicParameters CreateP040100996Parameters(decimal movementDate)
    {
        var parameters = new DynamicParameters();
        parameters.Add("ENT_NR_VRS", 1m, DbType.Decimal);
        parameters.Add("ENT_DT_MOVTO", movementDate, DbType.Decimal);
        parameters.Add("ENT_NR_LOTE", 1m, DbType.Decimal);
        parameters.Add("ENT_NR_CTRTER", null, DbType.String);
        parameters.Add("ENT_ID_CTRTER", "N", DbType.StringFixedLength, size: 1);
        parameters.Add("ENT_ID_GARANT", "N", DbType.StringFixedLength, size: 1);

        return parameters;
    }

    private static DynamicParameters CreateP040100999Parameters(decimal movementDate, string csvDirectoryPath)
    {
        var parameters = new DynamicParameters();
        parameters.Add("ENT_NR_VRS", "1", DbType.String, size: 4);
        parameters.Add("ENT_ID_TPAMB", "H", DbType.StringFixedLength, size: 1);
        parameters.Add("ENT_DT_MOVTO", movementDate, DbType.Decimal);
        parameters.Add("ENT_NR_LOTE", 1m, DbType.Decimal);
        parameters.Add("ENT_NR_CTRTER", string.Empty, DbType.String, size: 20);
        parameters.Add("ENT_ID_IMPCSV", "S", DbType.StringFixedLength, size: 1);
        parameters.Add("ENT_DS_DIRCSV", csvDirectoryPath, DbType.String, size: 256);
        parameters.Add("ENT_ID_CTRPRE", "S", DbType.StringFixedLength, size: 1);
        parameters.Add("ENT_ID_BAIXAS", "S", DbType.StringFixedLength, size: 1);
        parameters.Add("ENT_ID_LIBERA", "S", DbType.StringFixedLength, size: 1);
        parameters.Add("ENT_ID_CLIATU", null, DbType.Decimal);
        parameters.Add("ENT_ID_TOTCLI", null, DbType.Decimal);
        parameters.Add("ENT_ID_QUITA", "S", DbType.StringFixedLength, size: 1);
        parameters.Add("ENT_ID_GARANT", "N", DbType.StringFixedLength, size: 1);
        parameters.Add("ENT_ID_FINALI", "S", DbType.StringFixedLength, size: 1);
        parameters.Add("ENT_CD_OPESIS", 27m, DbType.Decimal);

        return parameters;
    }

    private static Task<int> ExecuteAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string commandText,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return connection.ExecuteAsync(new CommandDefinition(
            commandText,
            parameters,
            transaction,
            commandTimeout: 0,
            cancellationToken: cancellationToken));
    }

    private static Task<T> QuerySingleAsync<T>(
        SqlConnection connection,
        SqlTransaction transaction,
        string commandText,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return connection.QuerySingleAsync<T>(new CommandDefinition(
            commandText,
            parameters,
            transaction,
            commandTimeout: 0,
            cancellationToken: cancellationToken));
    }

    private static decimal ParseDateParameter(string value)
    {
        return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        return path.EndsWith('\\') || path.EndsWith('/')
            ? path
            : path + "\\";
    }

    private static string CreateConnectionString(DatabaseConnectionParameters parameters)
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

        return builder.ConnectionString;
    }

    private static async Task RollbackSafelyAsync(SqlTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch
        {
            // The transaction can already be aborted by XACT_ABORT.
        }
    }
}
