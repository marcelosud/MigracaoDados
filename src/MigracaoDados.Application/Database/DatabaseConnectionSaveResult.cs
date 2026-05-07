namespace MigracaoDados.Application.Database;

public sealed record DatabaseConnectionSaveResult(
    bool Success,
    string Message);
