using MigracaoDados.Domain.Importacao;
using MigracaoDados.Infrastructure.Schemas;

namespace MigracaoDados.Application.Tests;

public class ExcelLayoutImportSchemaReaderTests
{
    [Fact]
    public async Task ReadAsync_Should_Load_Contrato_Layout_Ordered_By_Id()
    {
        var layoutPath = GetRepositoryPath("Template Layout XLSX", "Contrato.xlsx");

        var schema = await new ExcelLayoutImportSchemaReader().ReadAsync(layoutPath);

        Assert.Equal("Contrato", schema.Name);
        Assert.Equal(96, schema.Columns.Count);
        Assert.Equal("ENT_DT_MOVTO", schema.Columns[0].Name);
        Assert.Equal("ENT_CD_EQLSTN", schema.Columns[^1].Name);
        Assert.Equal(ImportColumnType.Date, schema.Columns[0].Type);
        Assert.True(schema.Columns[0].Required);
        Assert.Equal(8, schema.Columns[0].MaxLength);
        Assert.Equal("AAAAMMDD", schema.Columns[0].Format);
    }

    private static string GetRepositoryPath(params string[] paths)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MigracaoDados.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Nao foi possivel localizar a raiz do repositorio.");
        }

        return Path.Combine([directory.FullName, .. paths]);
    }
}
