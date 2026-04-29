using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.StationRecords;

namespace Content.Shared._NF.GridAccess;

public sealed class GridAccessSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [Dependency] private readonly SharedAudioSystem _audio = default!;

    [Dependency] private readonly SharedTransformSystem _sharedTransformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationRecordKeyStorageComponent, AfterInteractEvent>(OnDeedSwipeHappened);
    }

    private void OnDeedSwipeHappened(EntityUid uid, StationRecordKeyStorageComponent _, ref AfterInteractEvent args)
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
            _audio.PlayLocal(gridAccessComponent.ErrorSound, rcdEntityUid, args.User);
            return;
        }

        // Swiping it again removes the authorization on it.
        if (gridAccessComponent.LinkedShuttleUid == shuttleDeedComponent.ShuttleUid)
        {
            _popup.PopupClient(Loc.GetString("grid-access-id-card-removed"),
                uid, args.User, PopupType.Medium);
            _audio.PlayLocal(gridAccessComponent.SwipeSound, rcdEntityUid, args.User);
            gridAccessComponent.LinkedShuttleUid = null;
        }
        else // Transfering or setting a new ID card
        {
            _popup.PopupClient(Loc.GetString("grid-access-id-card-accepted"),
                uid, args.User, PopupType.Medium);
            _audio.PlayLocal(gridAccessComponent.InsertSound, rcdEntityUid, args.User);
            gridAccessComponent.LinkedShuttleUid = shuttleDeedComponent.ShuttleUid;
        }

        Dirty(rcdEntityUid, gridAccessComponent);
    }

    /// <summary>
    /// Gets a tool's authorization for a given GridUid.
    /// Returns an incomplete, non-localized string for popups.
    /// </summary>
    public static bool IsAuthorized(EntityUid? gridUid, GridAccessComponent comp, out string? popupMessage)
    {
        popupMessage = null;

        if (gridUid == null)
        {
            return false;
        }

        // LinkedShuttleUid requirements to use Shipyard devices.
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
}
