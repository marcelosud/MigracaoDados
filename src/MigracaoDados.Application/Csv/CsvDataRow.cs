namespace MigracaoDados.Application.Csv;

public sealed record CsvDataRow(
    int RowNumber,
    IReadOnlyDictionary<string, string> Values);
