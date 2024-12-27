using System.Numerics;
using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Doors.Systems;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
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
using Content.Shared.Localizations;
using Content.Shared.Power;
namespace Content.Server.Shuttles.Systems
{

    public sealed class AdvDoorSealSystem : EntitySystem
    {
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly AirtightSystem _airtightSystem = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<AdvDoorSealComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<AdvDoorSealComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<AdvDoorSealComponent, UpdateSpacedEvent>(OnTileChange);
            SubscribeLocalEvent<AdvDoorSealComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, AdvDoorSealComponent component, ComponentInit args)
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && component.OriginalLoad == 0) { component.OriginalLoad = apcPower.Load; }

            if (CanEnable(uid, component))
            {
                EnableAirtightness(uid, component);
            }
            else
            {
                DisableAirtightness(uid, component);
            }
        }

        private void OnTileChange(EntityUid uid, AdvDoorSealComponent component, ref UpdateSpacedEvent args)
        {
            if (CanEnable(uid, component))
            {
                EnableAirtightness(uid, component);
            }
            else
            {
                DisableAirtightness(uid, component);
            }
        }

        private void OnPowerChange(EntityUid uid, AdvDoorSealComponent component, ref PowerChangedEvent args)
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

        private void OnAnchorChange(EntityUid uid, AdvDoorSealComponent component, ref AnchorStateChangedEvent args)
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
        public void EnableAirtightness(EntityUid uid, AdvDoorSealComponent component, TransformComponent? xform = null)
        {
            if (component.IsOn ||
                !Resolve(uid, ref xform))
            {
                return;
            }

            component.IsOn = true;
            if (TryComp(uid, out DoorComponent? door))
                door.ChangeAirtight = false;

            if (TryComp(uid, out AirtightComponent? airtight))
                _airtightSystem.SetAirblocked((uid, airtight), true);
        }


        public void DisableAirtightness(EntityUid uid, AdvDoorSealComponent component, TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref xform)) return;
            DisableAirtightness(uid, component, xform.GridUid, xform);
        }


        /// <summary>
        /// Tries to disable the seals
        /// </summary>
        public void DisableAirtightness(EntityUid uid, AdvDoorSealComponent component, EntityUid? gridId, TransformComponent? xform = null)
        {
            if (!component.IsOn ||
                !Resolve(uid, ref xform))
            {
                return;
            }

            component.IsOn = false;
            if (TryComp(uid, out DoorComponent? door))
            {
                door.ChangeAirtight = true;
                if (door.State == DoorState.Open && TryComp(uid, out AirtightComponent? airtight)) //If the door's open, goodbye air
                {
                    _airtightSystem.SetAirblocked((uid, airtight), false);
                }
            }

        }

        public bool CanEnable(EntityUid uid, AdvDoorSealComponent component)
        {
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

            var (x, y) = xform.LocalPosition + xform.LocalRotation.ToWorldVec();
            var mapGrid = Comp<MapGridComponent>(xform.GridUid.Value);
            var tile = _mapSystem.GetTileRef(xform.GridUid.Value, mapGrid, new Vector2i((int)Math.Floor(x), (int)Math.Floor(y)));

            return tile.Tile.IsSpace();
        }

    }

}
