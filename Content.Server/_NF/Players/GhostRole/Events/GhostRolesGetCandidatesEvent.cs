using Content.Shared.Ghost.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Players.GhostRole.Events;

[ByRefEvent]
public readonly record struct GhostRolesGetCandidatesEvent(NetUserId Player, List<ProtoId<GhostRolePrototype>> GhostRoles);
