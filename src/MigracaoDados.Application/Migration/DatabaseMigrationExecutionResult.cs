namespace MigracaoDados.Application.Migration;

public sealed record DatabaseMigrationExecutionResult(
    bool Success,
    string Message);
