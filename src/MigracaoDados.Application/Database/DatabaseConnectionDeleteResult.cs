namespace MigracaoDados.Application.Database;

public sealed record DatabaseConnectionDeleteResult(
    bool Success,
    string Message);
