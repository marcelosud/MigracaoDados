using MigracaoDados.Application.Csv;

namespace MigracaoDados.Application.Interfaces;

public interface ICsvFileReader
{
    Task<CsvDataFile> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}
