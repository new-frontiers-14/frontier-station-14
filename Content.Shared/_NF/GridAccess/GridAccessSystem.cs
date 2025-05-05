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
            if (!TryComp<ShuttleDeedComponent>(comp.Owner, out var shuttleDeedComponent))
            {
                _popup.PopupClient(Loc.GetString("grid-access-missing-id-deed"),
                    uid, args.User, PopupType.Medium);
                _audio.PlayPredicted(comp.ErrorSound, rcdEntityUid, args.User, AudioParams.Default.WithMaxDistance(0.01f));
                return;
            }

            // Swiping it again removes the authorization on it.
            if (gridAccessComponent.LinkedShuttleUid != null)
            {
                _popup.PopupClient(Loc.GetString("grid-access-id-card-removed"),
                    uid, args.User, PopupType.Medium);
                _audio.PlayPredicted(comp.SwipeSound, rcdEntityUid, args.User, AudioParams.Default.WithMaxDistance(0.01f));
                gridAccessComponent.LinkedShuttleUid = null;
            }
            else
            {
                _popup.PopupClient(Loc.GetString("grid-access-id-card-accepted"),
                    uid, args.User, PopupType.Medium);
                _audio.PlayPredicted(comp.InsertSound, rcdEntityUid, args.User, AudioParams.Default.WithMaxDistance(0.01f));
                gridAccessComponent.LinkedShuttleUid = shuttleDeedComponent.ShuttleUid;
            }

            Dirty(gridAccessComponent.Owner, gridAccessComponent);
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
            // var mapGrid = Comp<MapGridComponent>(gridId.Value);
            // var gridUid = mapGrid.Owner; // why was this a thing

            // Frontier - Remove all grid-access device use on outpost. Ignore if ProtectionOverride is true.
            if (!comp.ProtectionOverride && TryComp<ProtectedGridComponent>(gridUid, out var prot) && prot.PreventRCDUse)
            {
                // _popup.PopupClient(Loc.GetString("rcd-component-use-blocked"), used, user);
                popupMessage = "use-blocked";
                return false;
            }

            // Frontier - LinkedShuttleUid requirements to use Shipyard Remote.
            if (comp.LinkedShuttleUid == null)
            {
                // _popup.PopupClient(Loc.GetString("rcd-component-no-id-swiped"), used, user);
                popupMessage = "no-id-swiped";
                return false;
            }
            if (comp.LinkedShuttleUid != gridUid)
            {
                // _popup.PopupClient(Loc.GetString("rcd-component-can-only-build-authorized-ship"), used, user);
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
