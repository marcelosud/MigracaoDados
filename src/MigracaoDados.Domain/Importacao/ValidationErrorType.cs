namespace MigracaoDados.Domain.Importacao;

public enum ValidationErrorType
{
    EmptyFile,
    MissingColumn,
    UnexpectedColumn,
    ColumnOutOfOrder,
    RequiredValue,
    InvalidType,
    MaxLengthExceeded
}
