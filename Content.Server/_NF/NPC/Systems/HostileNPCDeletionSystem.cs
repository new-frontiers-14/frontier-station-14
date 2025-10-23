using Content.Shared.Body.Systems;
using Content.Shared.NPC;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Tiles;
using Robust.Shared.Audio.Systems;

namespace Content.Server._NF.NPC.Systems;

/// <summary>
///     Destroys enemy NPCs on protected grids.
/// </summary>
public sealed partial class HostileNPCDeletionSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedBodySystem _sharedBodySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActiveNPCComponent, ComponentStartup>(OnActiveNPCStartup);
        SubscribeLocalEvent<ActiveNPCComponent, EntParentChangedMessage>(OnActiveNPCParentChanged);
    }

    private void OnActiveNPCStartup(EntityUid uid, ActiveNPCComponent comp, ComponentStartup args)
    {
        DestroyEntityIfHostileOnProtectedGrid(uid);
    }

    private void OnActiveNPCParentChanged(EntityUid uid, ActiveNPCComponent comp, EntParentChangedMessage args)
    {
        DestroyEntityIfHostileOnProtectedGrid(uid);
    }

    private void DestroyEntityIfHostileOnProtectedGrid(EntityUid uid)
    {
        // If this entity is being destroyed, no need to fiddle with components
        if (Terminating(uid))
            return;

        var xform = Transform(uid);
        if (TryComp<ProtectedGridComponent>(xform.GridUid, out var protectedGrid))
        {
            if (protectedGrid.KillHostileMobs
                && TryComp<NpcFactionMemberComponent>(uid, out var npcFactionMember)
                && _npcFaction.IsFactionHostile("NanoTrasen", (uid, npcFactionMember)))
            {
                _audio.PlayPredicted(protectedGrid.HostileMobKillSound, xform.Coordinates, null);
                _sharedBodySystem.GibBody(uid);
                Spawn("Ash", xform.Coordinates);
                _popup.PopupCoordinates(Loc.GetString("admin-smite-turned-ash-other", ("name", uid)), xform.Coordinates, PopupType.LargeCaution);
                QueueDel(uid);
            }
        }
    }
}
