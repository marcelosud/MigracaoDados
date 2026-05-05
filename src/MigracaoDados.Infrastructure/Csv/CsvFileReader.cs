using MigracaoDados.Application.Csv;
using MigracaoDados.Application.Interfaces;

namespace MigracaoDados.Infrastructure.Csv;

public sealed class CsvFileReader : ICsvFileReader
{
    public async Task<CsvDataFile> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

        if (lines.Length == 0)
        {
            return new CsvDataFile(Array.Empty<string>(), Array.Empty<CsvDataRow>());
        }

        var separator = DetectSeparator(lines[0]);
        var headers = ParseLine(lines[0], separator)
            .Select(header => header.Trim())
            .ToArray();

        var rows = new List<CsvDataRow>();

        for (var index = 1; index < lines.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
            {
                continue;
            }

            var values = ParseLine(lines[index], separator);
            var rowValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
            {
                rowValues[headers[columnIndex]] = columnIndex < values.Count
                    ? values[columnIndex].Trim()
                    : string.Empty;
            }

            rows.Add(new CsvDataRow(index + 1, rowValues));
        }

        return new CsvDataFile(headers, rows);
    }

    private static char DetectSeparator(string headerLine)
    {
        var semicolonCount = headerLine.Count(character => character == ';');
        var commaCount = headerLine.Count(character => character == ',');

        return semicolonCount >= commaCount ? ';' : ',';
    }

    private static IReadOnlyList<string> ParseLine(string line, char separator)
    {
        var values = new List<string>();
        var currentValue = new List<char>();
        var insideQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (insideQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    currentValue.Add('"');
                    index++;
                    continue;
                }

                insideQuotes = !insideQuotes;
                continue;
            }

            if (character == separator && !insideQuotes)
            {
                values.Add(new string(currentValue.ToArray()));
                currentValue.Clear();
                continue;
            }

            currentValue.Add(character);
        }

        values.Add(new string(currentValue.ToArray()));
        return values;
    }
}
