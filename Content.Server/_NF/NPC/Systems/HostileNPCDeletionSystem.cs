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
        SubscribeLocalEvent<ActiveNPCComponent, EntParentChangedMessage>(OnActiveNPCParentChanged);
    }

    private void OnActiveNPCParentChanged(EntityUid uid, ActiveNPCComponent comp, EntParentChangedMessage args)
    {
        // If this entity is being destroyed, no need to fiddle with components
        if (Terminating(uid))
            return;

        var gridUid = Transform(uid).GridUid;
        if (TryComp<ProtectedGridComponent>(gridUid, out var protectedGrid))
        {
            if (TryComp<NpcFactionMemberComponent>(uid, out var npcFactionMember)
                && _npcFaction.IsFactionHostile("NanoTrasen", (uid, npcFactionMember)))
            {
                if (protectedGrid.KillHostileMobs)
                {
                    _audio.PlayPredicted(protectedGrid.HostileMobKillSound, Transform(uid).Coordinates, null);
                    _sharedBodySystem.GibBody(uid);
                    Spawn("Ash", Transform(uid).Coordinates);
                    _popup.PopupEntity(Loc.GetString("admin-smite-turned-ash-other", ("name", uid)), uid, PopupType.LargeCaution);
                    EntityManager.QueueDeleteEntity(uid);
                }
            }
        }
    }
}
