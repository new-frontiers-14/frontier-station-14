using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Components;
using Content.Shared.Tiles;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map; // Frontier
using System.Diagnostics.CodeAnalysis; // Frontier

namespace Content.Shared._NF.GridAccess
{
    public sealed class GridAccessSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        [Dependency] private readonly SharedAudioSystem _audio = default!;

        [Dependency] private readonly SharedTransformSystem _sharedTransformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<IdCardComponent, AfterInteractEvent>(OnIdCardSwipeHappened); // Frontier
        }

        private void OnIdCardSwipeHappened(EntityUid uid, IdCardComponent comp, ref AfterInteractEvent args)
        {
            if (args.Handled)
                return;

            if (args.Target is not { Valid: true } target || !args.CanReach)
                return;

            var rcdEntityUid = target;

            // Is this id card interacting with a grid-access device? If not, ignore it.
            if (!TryComp<GridAccessComponent>(rcdEntityUid, out var gridAccessComponent))
                return;

            // Device found, we're handling this event.
            args.Handled = true;

            // If the id card has no registered ship we cant continue.
            if (!TryComp<ShuttleDeedComponent>(uid, out var shuttleDeedComponent))
            {
                _popup.PopupClient(Loc.GetString("grid-access-missing-id-deed"),
                    uid, args.User, PopupType.Medium);
                _audio.PlayLocal(comp.ErrorSound, rcdEntityUid, args.User);
                return;
            }

            // Swiping it again removes the authorization on it.
            if (gridAccessComponent.LinkedShuttleUid == shuttleDeedComponent.ShuttleUid)
            {
                _popup.PopupClient(Loc.GetString("grid-access-id-card-removed"),
                    uid, args.User, PopupType.Medium);
                _audio.PlayLocal(comp.ErrorSound, rcdEntityUid, args.User);
                gridAccessComponent.LinkedShuttleUid = null;
            }
            else // Transfering or setting a new ID card
            {
                _popup.PopupClient(Loc.GetString("grid-access-id-card-accepted"),
                    uid, args.User, PopupType.Medium);
                _audio.PlayLocal(comp.ErrorSound, rcdEntityUid, args.User);
                gridAccessComponent.LinkedShuttleUid = shuttleDeedComponent.ShuttleUid;
            }

            Dirty(rcdEntityUid, gridAccessComponent);
        }

        /**
        * Frontier - Adds shipyard remote limitations.
        */
        public bool IsAuthorized(EntityUid? gridUid, GridAccessComponent comp, EntityUid used, EntityUid user, out string? popupMessage)
        {
            popupMessage = null;

            if (gridUid == null)
            {
                return true;
            }

            // Frontier - LinkedShuttleUid requirements to use Shipyard devices.
            if (comp.LinkedShuttleUid == null)
            {
                popupMessage = "no-id-swiped";
                return false;
            }
            if (comp.LinkedShuttleUid != gridUid)
            {
                popupMessage = "unauthorized-ship";
                return false;
            }

            return true;
        }

        public bool TryGetGridUid(EntityCoordinates location, [NotNullWhen(true)] out EntityUid? mapGridUid)
        {
            mapGridUid = _sharedTransformSystem.GetGrid(location);

            if (mapGridUid == null)
            {
                location = location.AlignWithClosestGridTile(1.75f, EntityManager);
                mapGridUid = _sharedTransformSystem.GetGrid(location); // location.GetGridUid(EntityManager);

                // Check if we got a grid ID the second time round
                if (mapGridUid == null)
                    return false;
            }

            return true;
        }
    }
}
