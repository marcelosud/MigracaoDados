using MigracaoDados.Application.Database;

namespace MigracaoDados.Application.Interfaces;

public interface IDatabaseConnectionRepository
{
    Task<DatabaseConnectionProfile?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task SaveAsync(DatabaseConnectionProfile profile, CancellationToken cancellationToken = default);
}
