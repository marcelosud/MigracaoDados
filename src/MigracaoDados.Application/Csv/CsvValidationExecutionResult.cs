using MigracaoDados.Domain.Importacao;

namespace MigracaoDados.Application.Csv;

public sealed record CsvValidationExecutionResult(
    CsvValidationResult ValidationResult,
    CsvDataFile CsvFile);
