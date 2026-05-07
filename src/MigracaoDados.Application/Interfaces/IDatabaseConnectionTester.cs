using MigracaoDados.Application.Database;

namespace MigracaoDados.Application.Interfaces;

public interface IDatabaseConnectionTester
{
    Task<DatabaseConnectionTestResult> TestAsync(
        DatabaseConnectionParameters parameters,
        CancellationToken cancellationToken = default);
}
