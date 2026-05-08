namespace MigracaoDados.Application.Session;

public sealed class MigrationSessionState
{
    public MigrationSessionParameters? Parameters { get; private set; }

    public void SetParameters(MigrationSessionParameters parameters)
    {
        Parameters = parameters;
    }

    public void Clear()
    {
        Parameters = null;
    }
}
