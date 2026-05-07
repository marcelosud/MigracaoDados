namespace MigracaoDados.Application.Database;

public sealed record DatabaseConnectionParameters(
    DatabaseProviderType Provider,
    string Server,
    string Database,
    string User,
    string Password);
