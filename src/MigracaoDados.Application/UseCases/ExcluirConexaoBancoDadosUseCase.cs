using MigracaoDados.Application.Database;
using MigracaoDados.Application.Interfaces;

namespace MigracaoDados.Application.UseCases;

public sealed class ExcluirConexaoBancoDadosUseCase
{
    private readonly IDatabaseConnectionRepository _repository;

    public ExcluirConexaoBancoDadosUseCase(IDatabaseConnectionRepository repository)
    {
        _repository = repository;
    }

    public async Task<DatabaseConnectionDeleteResult> ExecutarAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new DatabaseConnectionDeleteResult(false, "Conexao nao identificada para exclusao.");
        }

        await _repository.DeleteAsync(key, cancellationToken);
        return new DatabaseConnectionDeleteResult(true, "Parametros excluidos com sucesso.");
    }
}
