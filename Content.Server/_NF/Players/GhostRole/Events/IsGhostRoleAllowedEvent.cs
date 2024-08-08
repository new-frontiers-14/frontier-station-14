using Content.Shared.Ghost.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Players.GhostRole.Events;

[ByRefEvent]
public struct IsGhostRoleAllowedEvent(ICommonSession player, ProtoId<GhostRolePrototype> ghostRoleId, bool cancelled = false)
{
    public readonly ICommonSession Player = player;
    public readonly ProtoId<GhostRolePrototype> GhostRoleId = ghostRoleId;
    public bool Cancelled = cancelled;
}
