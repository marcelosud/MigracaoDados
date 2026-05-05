using MigracaoDados.Domain.Importacao;

namespace MigracaoDados.Application.Interfaces;

public interface IImportSchemaReader
{
    Task<ImportSchema> ReadAsync(string schemaPath, CancellationToken cancellationToken = default);
}
