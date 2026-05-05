namespace MigracaoDados.Domain.Importacao;

public sealed record ImportColumnDefinition(
    int Id,
    string Name,
    string Description,
    ImportColumnType Type,
    bool Required,
    int? MaxLength = null,
    string? Format = null,
    string? SourceType = null,
    IReadOnlyList<string>? DateFormats = null);
