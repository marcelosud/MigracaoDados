using MigracaoDados.Domain.Importacao;

namespace MigracaoDados.Domain.Tests;

public class ValidationErrorTypeExtensionsTests
{
    [Theory]
    [InlineData(ValidationErrorType.EmptyFile, "Arquivo vazio")]
    [InlineData(ValidationErrorType.MissingColumn, "Coluna ausente")]
    [InlineData(ValidationErrorType.UnexpectedColumn, "Coluna não prevista")]
    [InlineData(ValidationErrorType.ColumnOutOfOrder, "Coluna fora de ordem")]
    [InlineData(ValidationErrorType.RequiredValue, "Valor requerido")]
    [InlineData(ValidationErrorType.InvalidType, "Tipo inválido")]
    [InlineData(ValidationErrorType.MaxLengthExceeded, "Tamanho máximo excedido")]
    [InlineData(ValidationErrorType.InconsistentValue, "Valor divergente")]
    public void GetDescription_Should_Return_Portuguese_Description(
        ValidationErrorType errorType,
        string expectedDescription)
    {
        Assert.Equal(expectedDescription, errorType.GetDescription());
    }
}
