using MigracaoDados.Application.Database;
using MigracaoDados.Application.Migration;
using MigracaoDados.Application.Session;

namespace MigracaoDados.Application.Interfaces;

public interface IDatabaseMigrationExecutor
{
    Task<DatabaseMigrationExecutionResult> ExecuteAsync(
        DatabaseConnectionParameters connectionParameters,
        MigrationSessionParameters sessionParameters,
        CancellationToken cancellationToken = default);
}
