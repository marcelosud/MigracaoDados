namespace MigracaoDados.Domain.Importacao;

public sealed record ImportSchema(
    string Name,
    string Version,
    IReadOnlyList<ImportColumnDefinition> Columns)
{
    public ImportColumnDefinition? FindColumn(string name)
    {
        return Columns.FirstOrDefault(column =>
            string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
