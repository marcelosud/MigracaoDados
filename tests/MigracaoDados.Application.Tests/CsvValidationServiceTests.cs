using MigracaoDados.Application.Csv;
using MigracaoDados.Application.Services;
using MigracaoDados.Domain.Importacao;

namespace MigracaoDados.Application.Tests;

public class CsvValidationServiceTests
{
    private readonly CsvValidationService _service = new();

    [Fact]
    public void Validate_Should_Return_Missing_Column_Error()
    {
        var schema = CreateSchema();
        var csv = new CsvDataFile(
            ["Codigo", "Nome"],
            [
                new CsvDataRow(2, new Dictionary<string, string>
                {
                    ["Codigo"] = "1",
                    ["Nome"] = "Maria"
                })
            ]);

        var result = _service.Validate(schema, csv);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Type == ValidationErrorType.MissingColumn && error.ColumnName == "Ativo");
    }

    [Fact]
    public void Validate_Should_Return_Required_Value_Error()
    {
        var schema = CreateSchema();
        var csv = new CsvDataFile(
            ["Codigo", "Nome", "Ativo"],
            [
                new CsvDataRow(2, new Dictionary<string, string>
                {
                    ["Codigo"] = "1",
                    ["Nome"] = "",
                    ["Ativo"] = "true"
                })
            ]);

        var result = _service.Validate(schema, csv);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Type == ValidationErrorType.RequiredValue && error.ColumnName == "Nome");
    }

    [Fact]
    public void Validate_Should_Return_Invalid_Type_Error()
    {
        var schema = CreateSchema();
        var csv = new CsvDataFile(
            ["Codigo", "Nome", "Ativo"],
            [
                new CsvDataRow(2, new Dictionary<string, string>
                {
                    ["Codigo"] = "ABC",
                    ["Nome"] = "Maria",
                    ["Ativo"] = "true"
                })
            ]);

        var result = _service.Validate(schema, csv);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Type == ValidationErrorType.InvalidType && error.ColumnName == "Codigo");
    }

    [Fact]
    public void Validate_Should_Return_Column_Order_Error()
    {
        var schema = CreateSchema();
        var csv = new CsvDataFile(
            ["Nome", "Codigo", "Ativo"],
            [
                new CsvDataRow(2, new Dictionary<string, string>
                {
                    ["Codigo"] = "1",
                    ["Nome"] = "Maria",
                    ["Ativo"] = "true"
                })
            ]);

        var result = _service.Validate(schema, csv);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Type == ValidationErrorType.ColumnOutOfOrder);
    }

    [Fact]
    public void Validate_Should_Return_Max_Length_Error()
    {
        var schema = new ImportSchema(
            "Contrato",
            "1.0",
            [
                new ImportColumnDefinition(1, "ENT_NR_CTRTER", "Contrato terceiro", ImportColumnType.Text, true, 5)
            ]);

        var csv = new CsvDataFile(
            ["ENT_NR_CTRTER"],
            [
                new CsvDataRow(2, new Dictionary<string, string>
                {
                    ["ENT_NR_CTRTER"] = "123456"
                })
            ]);

        var result = _service.Validate(schema, csv);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Type == ValidationErrorType.MaxLengthExceeded);
    }

    [Fact]
    public void Validate_Should_Return_Valid_Result_For_Valid_Csv()
    {
        var schema = CreateSchema();
        var csv = new CsvDataFile(
            ["Codigo", "Nome", "Ativo"],
            [
                new CsvDataRow(2, new Dictionary<string, string>
                {
                    ["Codigo"] = "1",
                    ["Nome"] = "Maria",
                    ["Ativo"] = "sim"
                })
            ]);

        var result = _service.Validate(schema, csv);

        Assert.True(result.IsValid);
        Assert.Equal(1, result.TotalRows);
    }

    private static ImportSchema CreateSchema()
    {
        return new ImportSchema(
            "Contrato",
            "1.0",
            [
                new ImportColumnDefinition(1, "Codigo", "Codigo", ImportColumnType.Integer, true),
                new ImportColumnDefinition(2, "Nome", "Nome", ImportColumnType.Text, true),
                new ImportColumnDefinition(3, "Ativo", "Ativo", ImportColumnType.Boolean, true)
            ]);
    }
}
