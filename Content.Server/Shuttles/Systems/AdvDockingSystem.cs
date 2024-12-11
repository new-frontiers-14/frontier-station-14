using System.Numerics;
using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Doors.Systems;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Shuttles.Components;
using Content.Shared.Temperature;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Localizations;
using Content.Shared.Power;
using Content.Server.Construction; // Frontier
using Content.Server.DeviceLinking.Events; // Frontier
namespace Content.Server.Shuttles.Systems
{

    public sealed class AdvDockingSystem : EntitySystem
    {
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<AdvDockingComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<AdvDockingComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<ShuttleComponent, TileChangedEvent>(OnShuttleTileChange);
        }


        private void OnShuttleTileChange(EntityUid uid, ShuttleComponent component, ref TileChangedEvent args)
        {
            // If the old tile was space but the new one isn't then disable all adjacent thrusters
            if (args.NewTile.IsSpace(_tileDefManager) || !args.OldTile.IsSpace(_tileDefManager))
                return;

            var tilePos = args.NewTile.GridIndices;
            var grid = Comp<MapGridComponent>(uid);
            var xformQuery = GetEntityQuery<TransformComponent>();
            var dockQuery = GetEntityQuery<AdvDockingComponent>();

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x != 0 && y != 0)
                        continue;

                    var checkPos = tilePos + new Vector2i(x, y);
                    var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(uid, grid, checkPos);

                    while (enumerator.MoveNext(out var ent))
                    {
                        if (!dockQuery.TryGetComponent(ent.Value, out var dock))
                            continue;

                        // Work out if the dock is facing this direction
                        var xform = xformQuery.GetComponent(ent.Value);
                        var direction = xform.LocalRotation.ToWorldVec();

                        if (new Vector2i((int)direction.X, (int)direction.Y) != new Vector2i(x, y))
                            continue;

                        DisableAirtightness(ent.Value, dock, xform.GridUid);
                    }
                }
            }
        }

        private void OnPowerChange(EntityUid uid, AdvDockingComponent component, ref PowerChangedEvent args)
        {
            if (args.Powered && CanEnable(uid, component))
            {
                EnableAirtightness(uid, component);
            }
            else
            {
                DisableAirtightness(uid, component);
            }
        }

        private void OnAnchorChange(EntityUid uid, AdvDockingComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored && CanEnable(uid, component))
            {
                EnableAirtightness(uid, component);
            }
            else
            {
                DisableAirtightness(uid, component);
            }
        }


        /// <summary>
        /// Tries to enable the seals and turn it on. If it's already enabled it does nothing.
        /// </summary>
        public void EnableAirtightness(EntityUid uid, AdvDockingComponent component, TransformComponent? xform = null)
        {
            if (component.IsOn ||
                !Resolve(uid, ref xform))
            {
                return;
            }

            component.IsOn = true;
            if (TryComp(uid, out DoorComponent? door))
                door.ChangeAirtight = false;

            if (!EntityManager.TryGetComponent(xform.GridUid, out ShuttleComponent? shuttleComponent))
                return;

        }


        public void DisableAirtightness(EntityUid uid, AdvDockingComponent component, TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref xform)) return;
            DisableAirtightness(uid, component, xform.GridUid, xform);
        }


        /// <summary>
        /// Tries to disable the seals
        /// </summary>
        public void DisableAirtightness(EntityUid uid, AdvDockingComponent component, EntityUid? gridId, TransformComponent? xform = null)
        {
            if (!component.IsOn ||
                !Resolve(uid, ref xform))
            {
                return;
            }

            component.IsOn = false;
            if (TryComp(uid, out DoorComponent? door))
                door.ChangeAirtight = true;

            if (!EntityManager.TryGetComponent(gridId, out ShuttleComponent? shuttleComponent))
                return;
        }

        public bool CanEnable(EntityUid uid, AdvDockingComponent component)
        {
            if (!component.Enabled)
                return false;

            if (component.LifeStage > ComponentLifeStage.Running)
                return false;

            var xform = Transform(uid);

            if (!xform.Anchored || !this.IsPowered(uid, EntityManager))
            {
                return false;
            }

            return DockExposed(xform);
        }

        private bool DockExposed(TransformComponent xform)
        {
            if (xform.GridUid == null)
                return true;

            var (x, y) = xform.LocalPosition + xform.LocalRotation.Opposite().ToWorldVec();
            var mapGrid = Comp<MapGridComponent>(xform.GridUid.Value);
            var tile = _mapSystem.GetTileRef(xform.GridUid.Value, mapGrid, new Vector2i((int)Math.Floor(x), (int)Math.Floor(y)));

            return tile.Tile.IsSpace();
        }

    }

}
