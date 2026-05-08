using MigracaoDados.Application.Database;
using MigracaoDados.Application.Interfaces;
using MigracaoDados.Application.Migration;
using MigracaoDados.Application.Session;

namespace MigracaoDados.Application.UseCases;

public sealed class ExecutarStoredProceduresMigracaoUseCase
{
    private readonly IDatabaseMigrationExecutor _migrationExecutor;

    public ExecutarStoredProceduresMigracaoUseCase(IDatabaseMigrationExecutor migrationExecutor)
    {
        _migrationExecutor = migrationExecutor;
    }

    public Task<DatabaseMigrationExecutionResult> ExecutarAsync(
        DatabaseConnectionParameters connectionParameters,
        MigrationSessionParameters? sessionParameters,
        CancellationToken cancellationToken = default)
    {
        if (sessionParameters is null)
        {
            return Task.FromResult(new DatabaseMigrationExecutionResult(
                false,
                "Parametros da Etapa 1 nao encontrados. Valide os arquivos CSV antes de executar a migracao."));
        }

        if (connectionParameters.Provider != DatabaseProviderType.SqlServer)
        {
            return Task.FromResult(new DatabaseMigrationExecutionResult(
                false,
                "A Etapa 3 esta preparada para SQL Server. Selecione SQL Server no banco de destino."));
        }

        if (string.IsNullOrWhiteSpace(connectionParameters.Database))
        {
            return Task.FromResult(new DatabaseMigrationExecutionResult(
                false,
                "Informe o banco de dados de destino antes de executar a Etapa 3."));
        }

        if (string.IsNullOrWhiteSpace(sessionParameters.ENT_PATH_CSV))
        {
            return Task.FromResult(new DatabaseMigrationExecutionResult(
                false,
                "Caminho dos arquivos CSV nao encontrado nos parametros da sessao."));
        }

        if (!IsNumericDate(sessionParameters.EM700_DT_ANT)
            || !IsNumericDate(sessionParameters.ENT_DT_MOVTO)
            || !IsNumericDate(sessionParameters.EM700_DT_PRX))
        {
            return Task.FromResult(new DatabaseMigrationExecutionResult(
                false,
                "As datas da sessao devem estar no formato numerico yyyyMMdd para executar a Etapa 3."));
        }

        return _migrationExecutor.ExecuteAsync(connectionParameters, sessionParameters, cancellationToken);
    }

    private static bool IsNumericDate(string value)
    {
        return value.Length == 8
            && value.All(char.IsDigit);
    }
}
