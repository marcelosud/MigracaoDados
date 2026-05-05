using ClosedXML.Excel;
using MigracaoDados.Application.Interfaces;
using MigracaoDados.Domain.Importacao;

namespace MigracaoDados.Infrastructure.Schemas;

public sealed class ExcelLayoutImportSchemaReader : IImportSchemaReader
{
    public Task<ImportSchema> ReadAsync(string schemaPath, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(schemaPath);
        var worksheet = workbook.Worksheets.First();
        var columns = new List<ImportColumnDefinition>();

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!row.Cell(1).TryGetValue<int>(out var id))
            {
                continue;
            }

            var mnemonic = row.Cell(3).GetString().Trim();
            if (string.IsNullOrWhiteSpace(mnemonic))
            {
                continue;
            }

            columns.Add(new ImportColumnDefinition(
                id,
                mnemonic,
                row.Cell(2).GetString().Trim(),
                MapColumnType(row.Cell(5).GetString(), row.Cell(7).GetString()),
                IsRequired(row.Cell(4).GetString()),
                ReadOptionalInteger(row.Cell(6)),
                NormalizeText(row.Cell(7).GetString()),
                NormalizeText(row.Cell(5).GetString()),
                ReadDateFormats(row.Cell(7).GetString())));
        }

        var orderedColumns = columns
            .OrderBy(column => column.Id)
            .ToArray();

        return Task.FromResult(new ImportSchema(
            worksheet.Name,
            "1.0",
            orderedColumns));
    }

    private static ImportColumnType MapColumnType(string type, string format)
    {
        if (NormalizeForComparison(format) == "aaaammdd")
        {
            return ImportColumnType.Date;
        }

        var normalizedType = NormalizeForComparison(type);

        return normalizedType switch
        {
            "numerico" => ImportColumnType.Numeric,
            "alfanumerico" => ImportColumnType.Text,
            "char1" => ImportColumnType.Text,
            _ => ImportColumnType.Text
        };
    }

    private static bool IsRequired(string value)
    {
        return string.Equals(value.Trim(), "Sim", StringComparison.OrdinalIgnoreCase);
    }

    private static int? ReadOptionalInteger(IXLCell cell)
    {
        if (cell.TryGetValue<int>(out var value))
        {
            return value;
        }

        return int.TryParse(cell.GetString(), out value) ? value : null;
    }

    private static IReadOnlyList<string>? ReadDateFormats(string format)
    {
        var normalizedFormat = NormalizeForComparison(format);

        if (normalizedFormat == "aaaammdd")
        {
            return ["yyyyMMdd"];
        }

        return null;
    }

    private static string? NormalizeText(string value)
    {
        var trimmedValue = value.Trim();
        return string.IsNullOrWhiteSpace(trimmedValue) ? null : trimmedValue;
    }

    private static string NormalizeForComparison(string value)
    {
        return value
            .Trim()
            .ToLowerInvariant()
            .Replace("é", "e", StringComparison.OrdinalIgnoreCase)
            .Replace("ê", "e", StringComparison.OrdinalIgnoreCase)
            .Replace("í", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("ú", "u", StringComparison.OrdinalIgnoreCase)
            .Replace("á", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("ã", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("ó", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("õ", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("(", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(")", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
