using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeltaV.Administration;

[Serializable, NetSerializable]
public sealed class JobWhitelistsEuiState : EuiStateBase
{
    public string PlayerName;
    public HashSet<ProtoId<JobPrototype>> Whitelists;

    public JobWhitelistsEuiState(string playerName, HashSet<ProtoId<JobPrototype>> whitelists)
    {
        PlayerName = playerName;
        Whitelists = whitelists;
    }
}

/// <summary>
/// Tries to add or remove a whitelist of a job for a player.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetJobWhitelistedMessage : EuiMessageBase
{
    public ProtoId<JobPrototype> Job;
    public bool Whitelisting;

    public SetJobWhitelistedMessage(ProtoId<JobPrototype> job, bool whitelisting)
    {
        Job = job;
        Whitelisting = whitelisting;
    }
}
