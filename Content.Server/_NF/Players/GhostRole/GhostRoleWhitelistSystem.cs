using System.Collections.Immutable;
using Content.Server.Players.JobWhitelist;
using Content.Server._NF.Players.GhostRole.Events;
using Content.Shared.CCVar;
using Content.Shared.Ghost.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._NF.Players.GhostRole;

public sealed class GhostRoleWhitelistSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly JobWhitelistManager _manager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private ImmutableArray<ProtoId<GhostRolePrototype>> _whitelistedGhostRoles = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<GhostRolesGetCandidatesEvent>(OnStationGhostRolesGetCandidates);
        SubscribeLocalEvent<IsGhostRoleAllowedEvent>(OnIsGhostRoleAllowed);
        SubscribeLocalEvent<GetDisallowedGhostRolesEvent>(OnGetDisallowedGhostRoles);

        CacheJobs();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<GhostRolePrototype>())
            CacheJobs();
    }

    private void OnStationGhostRolesGetCandidates(ref GhostRolesGetCandidatesEvent ev)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return;

        for (var i = ev.GhostRoles.Count - 1; i >= 0; i--)
        {
            var ghostRoleId = ev.GhostRoles[i];
            if (_player.TryGetSessionById(ev.Player, out var player) &&
                !_manager.IsAllowed(player, ghostRoleId))
            {
                ev.GhostRoles.RemoveSwap(i);
            }
        }
    }

    private void OnIsGhostRoleAllowed(ref IsGhostRoleAllowedEvent ev)
    {
        if (!_manager.IsAllowed(ev.Player, ev.GhostRoleId))
            ev.Cancelled = true;
    }

    private void OnGetDisallowedGhostRoles(ref GetDisallowedGhostRolesEvent ev)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return;

        foreach (var ghostRole in _whitelistedGhostRoles)
        {
            if (!_manager.IsAllowed(ev.Player, ghostRole))
                ev.GhostRoles.Add(ghostRole);
        }
    }

    private void CacheJobs()
    {
        var builder = ImmutableArray.CreateBuilder<ProtoId<GhostRolePrototype>>();
        foreach (var ghostRole in _prototypes.EnumeratePrototypes<GhostRolePrototype>())
        {
            if (ghostRole.Whitelisted)
                builder.Add(ghostRole.ID);
        }

        _whitelistedGhostRoles = builder.ToImmutable();
    }
}
