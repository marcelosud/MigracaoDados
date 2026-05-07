namespace MigracaoDados.Application.Database;

public sealed record DatabaseConnectionProfile(
    string Key,
    DatabaseProviderType Provider,
    string Server,
    string Database,
    string User,
    string Password);
