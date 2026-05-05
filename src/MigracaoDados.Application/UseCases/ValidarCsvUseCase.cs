using MigracaoDados.Application.Interfaces;
using MigracaoDados.Domain.Importacao;

namespace MigracaoDados.Application.UseCases;

public sealed class ValidarCsvUseCase
{
    private readonly IImportSchemaReader _schemaReader;
    private readonly ICsvFileReader _csvFileReader;
    private readonly ICsvValidationService _validationService;

    public ValidarCsvUseCase(
        IImportSchemaReader schemaReader,
        ICsvFileReader csvFileReader,
        ICsvValidationService validationService)
    {
        _schemaReader = schemaReader;
        _csvFileReader = csvFileReader;
        _validationService = validationService;
    }

    public async Task<CsvValidationResult> ExecutarAsync(
        string csvPath,
        string schemaPath,
        CancellationToken cancellationToken = default)
    {
        var schema = await _schemaReader.ReadAsync(schemaPath, cancellationToken);
        var csvFile = await _csvFileReader.ReadAsync(csvPath, cancellationToken);

        return _validationService.Validate(schema, csvFile);
    }
}
