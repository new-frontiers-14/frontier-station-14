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
        SubscribeLocalEvent<StationRecordKeyStorageComponent, AfterInteractUsingEvent>(OnDeedSwipeHappenedAlternative);
    }

    private void OnDeedSwipeHappened(EntityUid uidDeed, StationRecordKeyStorageComponent _, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target is not { Valid: true } uidDevice || !args.CanReach)
            return;

        // Device found, we're handling this event.
        args.Handled = true;

        HandleDeedSwipe(uidDeed, uidDevice, args);
    }
    private void OnDeedSwipeHappenedAlternative(EntityUid uidDeed, StationRecordKeyStorageComponent _, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target is not { Valid: true } || !args.CanReach)
            return;

        var uidDevice = args.Used;

        // Device found, we're handling this event.
        args.Handled = true;

        HandleDeedSwipe(uidDeed, uidDevice, args);
    }
    private void HandleDeedSwipe(EntityUid uidDeed, EntityUid uidDevice, InteractEvent args)
    {
        if (!TryComp<GridAccessComponent>(uidDevice, out var gridAccessComponent))
            return;//This should never happen? Is there a better way to get oneself own component?

        // If the id card has no registered ship we cant continue.
        if (!TryComp<ShuttleDeedComponent>(uidDeed, out var shuttleDeedComponent))
        {
            _popup.PopupClient(Loc.GetString("grid-access-missing-id-deed"),
                uidDeed, args.User, PopupType.Medium);
            _audio.PlayLocal(gridAccessComponent.ErrorSound, uidDevice, args.User);
            return;
        }

        // Swiping it again removes the authorization on it.
        if (gridAccessComponent.LinkedShuttleUid == shuttleDeedComponent.ShuttleUid)
        {
            _popup.PopupClient(Loc.GetString("grid-access-id-card-removed"),
                uidDeed, args.User, PopupType.Medium);
            _audio.PlayLocal(gridAccessComponent.SwipeSound, uidDevice, args.User);
            gridAccessComponent.LinkedShuttleUid = null;
        }
        else // Transfering or setting a new ID card
        {
            _popup.PopupClient(Loc.GetString("grid-access-id-card-accepted"),
                uidDeed, args.User, PopupType.Medium);
            _audio.PlayLocal(gridAccessComponent.InsertSound, uidDevice, args.User);
            gridAccessComponent.LinkedShuttleUid = shuttleDeedComponent.ShuttleUid;
        }

        Dirty(uidDevice, gridAccessComponent);
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
