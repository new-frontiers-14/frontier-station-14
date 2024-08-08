using Content.Shared.Ghost.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Players.GhostRole.Events;

[ByRefEvent]
public readonly record struct GetDisallowedGhostRolesEvent(ICommonSession Player, HashSet<ProtoId<GhostRolePrototype>> GhostRoles);
