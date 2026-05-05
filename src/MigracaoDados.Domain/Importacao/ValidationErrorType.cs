using System.ComponentModel;

namespace MigracaoDados.Domain.Importacao;

public enum ValidationErrorType
{
    [Description("Arquivo vazio")]
    EmptyFile,

    [Description("Coluna ausente")]
    MissingColumn,

    [Description("Coluna não prevista")]
    UnexpectedColumn,

    [Description("Coluna fora de ordem")]
    ColumnOutOfOrder,

    [Description("Valor requerido")]
    RequiredValue,

    [Description("Tipo inválido")]
    InvalidType,

    [Description("Tamanho máximo excedido")]
    MaxLengthExceeded
}
