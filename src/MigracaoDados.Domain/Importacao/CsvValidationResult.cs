namespace MigracaoDados.Domain.Importacao;

public sealed record CsvValidationResult(
    int TotalRows,
    IReadOnlyList<CsvValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
