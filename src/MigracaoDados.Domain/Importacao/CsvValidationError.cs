namespace MigracaoDados.Domain.Importacao;

public sealed record CsvValidationError(
    int RowNumber,
    string ColumnName,
    string ProvidedValue,
    string Message,
    ValidationErrorType Type);
