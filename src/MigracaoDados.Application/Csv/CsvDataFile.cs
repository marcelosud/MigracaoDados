namespace MigracaoDados.Application.Csv;

public sealed record CsvDataFile(
    IReadOnlyList<string> Headers,
    IReadOnlyList<CsvDataRow> Rows);
