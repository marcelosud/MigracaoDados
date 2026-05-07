using MigracaoDados.Application.Database;
using MigracaoDados.Application.Interfaces;

namespace MigracaoDados.Application.UseCases;

public sealed class TestarConexaoBancoDadosUseCase
{
    private readonly IDatabaseConnectionTester _connectionTester;

    public TestarConexaoBancoDadosUseCase(IDatabaseConnectionTester connectionTester)
    {
        _connectionTester = connectionTester;
    }

    public Task<DatabaseConnectionTestResult> ExecutarAsync(
        DatabaseConnectionParameters parameters,
        CancellationToken cancellationToken = default)
    {
        return _connectionTester.TestAsync(parameters, cancellationToken);
    }
}
