using MigracaoDados.Application.Csv;
using MigracaoDados.Domain.Importacao;

namespace MigracaoDados.Application.Interfaces;

public interface ICsvValidationService
{
    CsvValidationResult Validate(ImportSchema schema, CsvDataFile csvFile);
}
