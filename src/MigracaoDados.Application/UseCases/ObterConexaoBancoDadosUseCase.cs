using MigracaoDados.Application.Database;
using MigracaoDados.Application.Interfaces;

namespace MigracaoDados.Application.UseCases;

public sealed class ObterConexaoBancoDadosUseCase
{
    private readonly IDatabaseConnectionRepository _repository;

    public ObterConexaoBancoDadosUseCase(IDatabaseConnectionRepository repository)
    {
        _repository = repository;
    }

    public Task<DatabaseConnectionProfile?> ExecutarAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetAsync(key, cancellationToken);
    }
}
