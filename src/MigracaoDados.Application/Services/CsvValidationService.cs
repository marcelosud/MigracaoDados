using System.Globalization;
using MigracaoDados.Application.Csv;
using MigracaoDados.Application.Interfaces;
using MigracaoDados.Domain.Importacao;

namespace MigracaoDados.Application.Services;

public sealed class CsvValidationService : ICsvValidationService
{
    public CsvValidationResult Validate(ImportSchema schema, CsvDataFile csvFile)
    {
        var errors = new List<CsvValidationError>();

        if (csvFile.Headers.Count == 0)
        {
            errors.Add(new CsvValidationError(
                1,
                string.Empty,
                string.Empty,
                "O arquivo CSV está vazio ou não possui cabeçalho.",
                ValidationErrorType.EmptyFile));

            return new CsvValidationResult(0, errors);
        }

        ValidateHeaders(schema, csvFile, errors);
        ValidateHeaderOrder(schema, csvFile, errors);

        foreach (var row in csvFile.Rows)
        {
            foreach (var column in schema.Columns)
            {
                if (!csvFile.Headers.Any(header => string.Equals(header, column.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                row.Values.TryGetValue(column.Name, out var value);
                value ??= string.Empty;

                if (column.Required && string.IsNullOrWhiteSpace(value))
                {
                    errors.Add(new CsvValidationError(
                        row.RowNumber,
                        column.Name,
                        value,
                        $"A coluna '{column.Name}' é obrigatória.",
                        ValidationErrorType.RequiredValue));

                    continue;
                }

                if (column.MaxLength is not null && value.Length > column.MaxLength.Value)
                {
                    errors.Add(new CsvValidationError(
                        row.RowNumber,
                        column.Name,
                        value,
                        $"O valor informado possui {value.Length} caractere(s), mas o tamanho máximo da coluna '{column.Name}' é {column.MaxLength}.",
                        ValidationErrorType.MaxLengthExceeded));
                }

                if (!string.IsNullOrWhiteSpace(value) && !IsValidType(value, column))
                {
                    errors.Add(new CsvValidationError(
                        row.RowNumber,
                        column.Name,
                        value,
                        $"O valor '{value}' não corresponde ao tipo esperado '{column.Type}'.",
                        ValidationErrorType.InvalidType));
                }
            }
        }

        return new CsvValidationResult(csvFile.Rows.Count, errors);
    }

    private static void ValidateHeaders(ImportSchema schema, CsvDataFile csvFile, List<CsvValidationError> errors)
    {
        foreach (var column in schema.Columns)
        {
            if (!csvFile.Headers.Any(header => string.Equals(header, column.Name, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(new CsvValidationError(
                    1,
                    column.Name,
                    string.Empty,
                    $"A coluna obrigatória do schema '{column.Name}' não foi encontrada no CSV.",
                    ValidationErrorType.MissingColumn));
            }
        }

        foreach (var header in csvFile.Headers)
        {
            if (schema.FindColumn(header) is null)
            {
                errors.Add(new CsvValidationError(
                    1,
                    header,
                    header,
                    $"A coluna '{header}' não esta prevista no schema.",
                    ValidationErrorType.UnexpectedColumn));
            }
        }
    }

    private static void ValidateHeaderOrder(ImportSchema schema, CsvDataFile csvFile, List<CsvValidationError> errors)
    {
        var expectedColumns = schema.Columns
            .OrderBy(column => column.Id)
            .Select(column => column.Name)
            .ToArray();

        var comparedCount = Math.Min(expectedColumns.Length, csvFile.Headers.Count);

        for (var index = 0; index < comparedCount; index++)
        {
            if (!string.Equals(expectedColumns[index], csvFile.Headers[index], StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new CsvValidationError(
                    1,
                    csvFile.Headers[index],
                    csvFile.Headers[index],
                    $"A coluna na posição {index + 1} deveria ser '{expectedColumns[index]}', mas foi encontrada '{csvFile.Headers[index]}'.",
                    ValidationErrorType.ColumnOutOfOrder));
            }
        }
    }

    private static bool IsValidType(string value, ImportColumnDefinition column)
    {
        return column.Type switch
        {
            ImportColumnType.Text => true,
            ImportColumnType.Numeric => IsValidNumeric(value),
            ImportColumnType.Integer => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            ImportColumnType.Decimal => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
                || decimal.TryParse(value, NumberStyles.Number, new CultureInfo("pt-BR"), out _),
            ImportColumnType.Date => IsValidDate(value, column.DateFormats),
            ImportColumnType.Boolean => bool.TryParse(value, out _)
                || value is "0" or "1"
                || string.Equals(value, "sim", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "nao", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool IsValidNumeric(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            || double.TryParse(value, NumberStyles.Float, new CultureInfo("pt-BR"), out _);
    }

    private static bool IsValidDate(string value, IReadOnlyList<string>? dateFormats)
    {
        if (dateFormats is { Count: > 0 })
        {
            return DateTime.TryParseExact(
                value,
                dateFormats.ToArray(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _);
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            || DateTime.TryParse(value, new CultureInfo("pt-BR"), DateTimeStyles.None, out _);
    }
}
