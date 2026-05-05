using MigracaoDados.Domain.Importacao;

namespace MigracaoDados.Domain.Tests;

public class ImportSchemaTests
{
    [Fact]
    public void FindColumn_Should_Find_Column_Ignoring_Case()
    {
        var schema = new ImportSchema(
            "Contrato",
            "1.0",
            [
                new ImportColumnDefinition(1, "ENT_NR_CTRTER", "Codigo contrato terceiro", ImportColumnType.Text, true)
            ]);

        var column = schema.FindColumn("ent_nr_ctrter");

        Assert.NotNull(column);
        Assert.Equal("ENT_NR_CTRTER", column.Name);
    }

    [Fact]
    public void CsvValidationResult_Should_Be_Valid_When_It_Has_No_Errors()
    {
        var result = new CsvValidationResult(2, []);

        Assert.True(result.IsValid);
    }
}
