using MigracaoDados.Application.Database;
using MigracaoDados.Application.Interfaces;

namespace MigracaoDados.Application.UseCases;

public sealed class SalvarConexaoBancoDadosUseCase
{
    private readonly IDatabaseConnectionRepository _repository;

    public SalvarConexaoBancoDadosUseCase(IDatabaseConnectionRepository repository)
    {
        _repository = repository;
    }

    public async Task<DatabaseConnectionSaveResult> ExecutarAsync(
        DatabaseConnectionProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profile.Server))
        {
            return new DatabaseConnectionSaveResult(false, "Informe o servidor antes de salvar.");
        }

        if (string.IsNullOrWhiteSpace(profile.User))
        {
            return new DatabaseConnectionSaveResult(false, "Informe o usuario antes de salvar.");
        }

        if (string.IsNullOrWhiteSpace(profile.Password))
        {
            return new DatabaseConnectionSaveResult(false, "Informe a senha antes de salvar.");
        }

        await _repository.SaveAsync(profile, cancellationToken);
        return new DatabaseConnectionSaveResult(true, "Parametros salvos com sucesso.");
    }
}
