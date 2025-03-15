using Content.Server.Station.Systems;
using Content.Shared._NF.BindToGrid;
using Content.Shared.Examine;

namespace Content.Server._NF.BindToGrid;

public sealed class BindToGridSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BindToGridComponent, ExaminedEvent>(OnBoundItemExamined);
    }

    private void OnBoundItemExamined(EntityUid uid, BindToGridComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || (component.BoundGrid == null))
            return;

        var stationName = Loc.GetString("bound-to-grid-unknown-station");
        if (!Deleted(GetEntity(component.BoundGrid)))
        {
            stationName = Name(GetEntity(component.BoundGrid));
        }

        args.PushMarkup(Loc.GetString("bound-to-grid-examine-text", ("shipname", stationName)));
    }
}
