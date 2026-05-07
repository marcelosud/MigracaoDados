namespace MigracaoDados.Application.Database;

public sealed record DatabaseConnectionTestResult(
    bool Success,
    string Message);
