using System.Linq;
using Content.Server.Cargo.Components;
using Content.Shared._NF.ShuttleRecords;
using Content.Shared._NF.ShuttleRecords.Components;
using Content.Shared._NF.ShuttleRecords.Events;
using Content.Shared.Access.Components;
using Content.Shared.Database;
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

        if (!TryGetShuttleRecordsDataComponent(out var dataComponent))
            return;

        var newState = new ShuttleRecordsConsoleInterfaceState(
            records: dataComponent.ShuttleRecordsList,
            transactionCost: component.TransactionPrice
        );

        _ui.SetUiState(consoleUid, ShuttleRecordsUiKey.Default, newState);
    }

    private void OnCopyDeedMessage(EntityUid uid, ShuttleRecordsConsoleComponent component, CopyDeedMessage args)
    {
        if (!TryGetShuttleRecordsDataComponent(out var dataComponent))
            return;

        // Check if id card is present.
        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid: true } targetId)
        {
            _popup.PopupEntity(Loc.GetString("shuttle-records-no-idcard"), args.Actor);
            _audioSystem.PlayPredicted(component.ErrorSound, uid, null, AudioParams.Default.WithMaxDistance(5f));
            return;
        }

        // Check for & get station bank account.
        var station = _station.GetOwningStation(uid);
        if (!TryComp<StationBankAccountComponent>(station, out var stationBank))
        {
            _popup.PopupEntity(Loc.GetString("shuttle-records-no-bank-account"), args.Actor);
            _audioSystem.PlayPredicted(component.ErrorSound, uid, null, AudioParams.Default.WithMaxDistance(5f));
            return;
        }

        // Ensure that after the deduction math there is more than 0 left in the account.
        var balanceAfterTransaction = stationBank.Balance - component.TransactionPrice;
        if (balanceAfterTransaction < 0)
        {
            _popup.PopupEntity(Loc.GetString("shuttle-records-insufficient-funds"), args.Actor);
            _audioSystem.PlayPredicted(component.ErrorSound, uid, null, AudioParams.Default.WithMaxDistance(5f));
            return;
        }

        // Check if the shuttle record exists.
        var record = dataComponent.ShuttleRecordsList.Select(record => record).FirstOrDefault(record => record.EntityUid == args.ShuttleNetEntity);
        if (record == null)
        {
            _popup.PopupEntity(Loc.GetString("shuttle-records-no-record-found"), args.Actor);
            _audioSystem.PlayPredicted(component.ErrorSound, uid, null, AudioParams.Default.WithMaxDistance(5f));
            return;
        }

        // Check if the actor has access to the shuttle records console.
        if (!_access.IsAllowed(args.Actor, uid))
        {
            _popup.PopupEntity(Loc.GetString("shuttle-records-no-access"), args.Actor);
            _audioSystem.PlayPredicted(component.ErrorSound, uid, null, AudioParams.Default.WithMaxDistance(5f));
            return;
        }

        AssignShuttleDeedProperties(record, targetId);

        // Now we can finally deduct funds since everything went well.
        stationBank.Balance = balanceAfterTransaction;

        // Add to admin logs.
        var shuttleName = record.Name + " " + record.Suffix;
        _adminLogger.Add(
            LogType.ShuttleRecordsUsage,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} used {component.TransactionPrice} from station bank account to copy shuttle deed {shuttleName}.");
        _audioSystem.PlayPredicted(component.ConfirmSound, uid, null, AudioParams.Default.WithMaxDistance(5f));
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
