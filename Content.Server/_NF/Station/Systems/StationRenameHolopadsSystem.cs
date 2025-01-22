using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Holopad;
using Content.Shared.Labels.Components;
using Content.Shared.NameModifier.EntitySystems;

namespace Content.Server._NF.Station.Systems;

public sealed class StationRenameHolopadsSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!; // TODO: use LabelSystem directly instead of this.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRenameHolopadsComponent, StationPostInitEvent>(OnPostInit);
    }

    private void OnPostInit(EntityUid uid, StationRenameHolopadsComponent component, ref StationPostInitEvent args)
    {
        SyncHolopadsNames(uid);
    }

    private void SyncHolopadsNames(EntityUid stationUid)
    {
        // update all holopads that belong to this station grid
        var query = EntityQueryEnumerator<HolopadComponent>();
        while (query.MoveNext(out var uid, out var pad))
        {
            if (!pad.UseStationName)
                continue;

            var padStationUid = _stationSystem.GetOwningStation(uid);
            if (padStationUid != stationUid)
                continue;

            SyncHolopad((uid, pad), padStationUid);
        }
    }

    public void SyncHolopad(Entity<HolopadComponent> holopad, EntityUid? padStationUid = null)
    {
        if (!holopad.Comp.UseStationName)
            return;

        padStationUid ??= _stationSystem.GetOwningStation(holopad);
        if (padStationUid == null)
        {
            RemComp<LabelComponent>(holopad); // No idea where we are, any name is probably inaccurate.
            return;
        }

        var padName = "";

        if (!string.IsNullOrEmpty(holopad.Comp.StationNamePrefix))
        {
            padName += holopad.Comp.StationNamePrefix + " ";
        }

        padName += Name(padStationUid.Value);

        if (!string.IsNullOrEmpty(holopad.Comp.StationNameSuffix))
        {
            padName += " " + holopad.Comp.StationNameSuffix;
        }

        // FIXME: this should use NameModifiers, LabelSystem.Label(), and work regardless of PreventLabelTag.
        var padLabel = EnsureComp<LabelComponent>(holopad);
        padLabel.CurrentLabel = padName;
        _nameMod.RefreshNameModifiers(holopad.Owner);
        Dirty(holopad, padLabel);
    }
}
