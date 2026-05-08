namespace MigracaoDados.Application.Session;

public sealed record MigrationSessionParameters(
    string ENT_DT_MOVTO,
    string EM700_DT_ANT,
    string EM700_DT_PRX,
    string ENT_NR_INST,
    string ENT_PATH_CSV);
