using System.Linq;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Systems;
using Content.Server.Research.TechnologyDisk.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research.TechnologyDisk.Components;
using Content.Shared.Station.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Disk
{
    public sealed class ResearchDiskSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ResearchSystem _research = default!;
        [Dependency] private readonly StationSystem _station = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResearchDiskComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ResearchDiskComponent, MapInitEvent>(OnMapInit);
        }

        private void OnAfterInteract(EntityUid uid, ResearchDiskComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            // Frontier: Make disk console the new way to insert points and tech disks.
            // Restrict adding points to disk consoles only.
            if (!TryComp<DiskConsoleComponent>(args.Target, out _))
                return;

            // Frontier: Get the current grid station
            if (args.Target == null)
                return;
            var station = _station.GetOwningStation(args.Target.Value);

            // Frontier: Server is on the grid.
            if (!TryComp<ResearchServerComponent>(station, out var server))
                return;


            _research.ModifyServerPoints(args.Target.Value, component.Points, server);
            _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
            EntityManager.QueueDeleteEntity(uid);
            args.Handled = true;
        }

        private void OnMapInit(EntityUid uid, ResearchDiskComponent component, MapInitEvent args)
        {
            if (!component.UnlockAllTech)
                return;

            component.Points = _prototype.EnumeratePrototypes<TechnologyPrototype>()
                .Sum(tech => tech.Cost);
        }
    }
}
