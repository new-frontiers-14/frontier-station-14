using System.Linq;
using Content.Shared._NF.ShuttleRecords;
using Content.Shared._NF.ShuttleRecords.Components;
using Content.Shared._NF.ShuttleRecords.Events;
using Content.Shared.Access.Components;
using Content.Shared.Shipyard.Components;
using Robust.Shared.Audio;

namespace Content.Server._NF.ShuttleRecords;

public sealed partial class ShuttleRecordsSystem
{
    public void InitializeShuttleRecords()
    {
        SubscribeLocalEvent<ShuttleRecordsConsoleComponent, BoundUIOpenedEvent>(OnConsoleUiOpened);
        SubscribeLocalEvent<ShuttleRecordsConsoleComponent, CopyDeedMessage>(OnCopyDeedMessage);
    }

    private void OnConsoleUiOpened(EntityUid uid, ShuttleRecordsConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (args.Actor is not { Valid: true })
            return;

        RefreshState(uid, component);
    }


    private void RefreshState(EntityUid consoleUid, ShuttleRecordsConsoleComponent? component)
    {
        if (!Resolve(consoleUid, ref component))
            return;

        // Ensures that when this console is no longer attached to a grid and is powered somehow, it won't work.
        if (Transform(consoleUid).GridUid == null)
            return;

        if (!TryGetShuttleRecordsDataComponent(consoleUid, out var dataComponent))
            return;

        var newState = new ShuttleRecordsConsoleInterfaceState(
            records: dataComponent.ShuttleRecordsList
        );

        _ui.SetUiState(consoleUid, ShuttleRecordsUiKey.Default, newState);
    }

    private void OnCopyDeedMessage(EntityUid uid, ShuttleRecordsConsoleComponent component, CopyDeedMessage args)
    {
        if (!TryGetShuttleRecordsDataComponent(uid, out var dataComponent))
            return;

        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid: true } targetId)
        {
            _popup.PopupEntity(Loc.GetString("shuttle-records-no-idcard"), args.Actor);
            _audioSystem.PlayPredicted(component.ErrorSound, uid, null, AudioParams.Default.WithMaxDistance(5f));
            return;
        }

        var record = dataComponent.ShuttleRecordsList.Select(record => record).FirstOrDefault(record => record.EntityUid == args.ShuttleNetEntity);
        if (record != null)
        {
            AssignShuttleDeedProperties(record, targetId);
            _audioSystem.PlayPredicted(component.ConfirmSound, uid, null, AudioParams.Default.WithMaxDistance(5f));
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("shuttle-records-no-record-found"), args.Actor);
            _audioSystem.PlayPredicted(component.ErrorSound, uid, null, AudioParams.Default.WithMaxDistance(5f));
        }
    }

    private void AssignShuttleDeedProperties(ShuttleRecord shuttleRecord, EntityUid targetId)
    {
        // Ensure that this is in fact a id card.
        if (!_entityManager.TryGetComponent<IdCardComponent>(targetId, out _))
            return;

        _entityManager.EnsureComponent<ShuttleDeedComponent>(targetId, out var deed);

        var shuttleEntity = _entityManager.GetEntity(shuttleRecord.EntityUid);

        // Copy over the variables from the shuttle record to the deed.
        deed.ShuttleUid = shuttleEntity;
        deed.ShuttleOwner = shuttleRecord.OwnerName;
        deed.ShuttleName = shuttleRecord.Name;
        deed.ShuttleNameSuffix = shuttleRecord.Suffix;
    }
}
