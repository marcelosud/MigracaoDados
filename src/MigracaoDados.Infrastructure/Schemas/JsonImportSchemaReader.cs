using System.Text.Json;
using System.Text.Json.Serialization;
using MigracaoDados.Application.Interfaces;
using MigracaoDados.Domain.Importacao;

namespace MigracaoDados.Infrastructure.Schemas;

public sealed class JsonImportSchemaReader : IImportSchemaReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ImportSchema> ReadAsync(string schemaPath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(schemaPath);
        var schema = await JsonSerializer.DeserializeAsync<ImportSchema>(
            stream,
            SerializerOptions,
            cancellationToken);

        return schema ?? throw new InvalidOperationException("Nao foi possivel ler o schema de importacao.");
    }
}
