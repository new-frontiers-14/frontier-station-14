using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DV.Administration;

[Serializable, NetSerializable]
public sealed class JobWhitelistsEuiState : EuiStateBase
{
    public string PlayerName;
    public HashSet<ProtoId<JobPrototype>> Whitelists;
    public HashSet<ProtoId<GhostRolePrototype>> GhostRoleWhitelists;
    public bool GlobalWhitelist;

    public JobWhitelistsEuiState(string playerName, HashSet<ProtoId<JobPrototype>> whitelists, HashSet<ProtoId<GhostRolePrototype>> ghostRoleWhitelists, bool globalWhitelist)
    {
        PlayerName = playerName;
        Whitelists = whitelists;
        GhostRoleWhitelists = ghostRoleWhitelists;
        GlobalWhitelist = globalWhitelist;
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

/// <summary>
/// Frontier: tries to add or remove a whitelist of a ghost role for a player.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetGhostRoleWhitelistedMessage : EuiMessageBase
{
    public ProtoId<GhostRolePrototype> Role;
    public bool Whitelisting;

    public SetGhostRoleWhitelistedMessage(ProtoId<GhostRolePrototype> role, bool whitelisting)
    {
        Role = role;
        Whitelisting = whitelisting;
    }
}

/// <summary>
/// Frontier: tries to add or remove a global whitelist for a player.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetGlobalWhitelistMessage : EuiMessageBase
{
    public bool Whitelisting;

    public SetGlobalWhitelistMessage(bool whitelisting)
    {
        Whitelisting = whitelisting;
    }
}
