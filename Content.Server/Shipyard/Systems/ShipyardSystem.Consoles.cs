using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Bank;
using Content.Shared.Bank.Components;
using Content.Shared.Shipyard.Events;
using Content.Shared.Shipyard.BUI;
using Content.Shared.Shipyard.Prototypes;
using Content.Shared.Shipyard.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Shipyard;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Maps;
using Content.Server.UserInterface;
using Content.Shared.StationRecords;
using Content.Server.Chat.Systems;
using Content.Server.Mind;
using Content.Server.StationRecords.Systems;
using Content.Shared.Database;
using Content.Server.Station.Components;

namespace Content.Server.Shipyard.Systems;

public sealed partial class ShipyardSystem : SharedShipyardSystem
{
    [Dependency] private readonly AccessSystem _accessSystem = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly IdCardSystem _idSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public void InitializeConsole()
    {

    }

    private void OnPurchaseMessage(EntityUid uid, ShipyardConsoleComponent component, ShipyardConsolePurchaseMessage args)
    {
        if (args.Session.AttachedEntity is not { Valid : true } player)
        {
            return;
        }

        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid : true } targetId)
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<IdCardComponent>(targetId, out var idCard))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(uid, component);
            return;
        }

        if (HasComp<ShuttleDeedComponent>(targetId))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-already-deeded"));
            PlayDenySound(uid, component);
            return;
        }

        if (TryComp<AccessReaderComponent>(uid, out var accessReaderComponent) && !_access.IsAllowed(player, uid, accessReaderComponent))
        {
            ConsolePopup(args.Session, Loc.GetString("comms-console-permission-denied"));
            PlayDenySound(uid, component);
            return;
        }

        if (!_prototypeManager.TryIndex<VesselPrototype>(args.Vessel, out var vessel))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-invalid-vessel", ("vessel", args.Vessel)));
            PlayDenySound(uid, component);
            return;
        }

        var name = vessel.Name;
        if (vessel.Price <= 0)
            return;

        if (_station.GetOwningStation(uid) is not { Valid : true } station)
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-invalid-station"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-bank"));
            PlayDenySound(uid, component);
            return;
        }

        if (bank.Balance <= vessel.Price)
        {
            ConsolePopup(args.Session, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
            PlayDenySound(uid, component);
            return;
        }

        if (!_bank.TryBankWithdraw(player, vessel.Price))
        {
            ConsolePopup(args.Session, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryPurchaseShuttle((EntityUid) station, vessel.ShuttlePath.ToString(), out var shuttle))
        {
            PlayDenySound(uid, component);
            return;
        }
        EntityUid? shuttleStation = null;
        // setting up any stations if we have a matching game map prototype to allow late joins directly onto the vessel
        if (_prototypeManager.TryIndex<GameMapPrototype>(vessel.ID, out var stationProto))
        {
            List<EntityUid> gridUids = new()
            {
                shuttle.Owner
            };
            shuttleStation = _station.InitializeNewStation(stationProto.Stations[vessel.ID], gridUids);
            var metaData = MetaData((EntityUid) shuttleStation);
            name = metaData.EntityName;
            _shuttle.SetIFFColor(shuttle.Owner, new Color
            {
                R = 10,
                G = 50,
                B = 175,
                A = 155
            });
        }

        if (TryComp<AccessComponent>(targetId, out var newCap))
        {
            //later we will make a custom pilot job, for now they get the captain treatment
            var newAccess = newCap.Tags.ToList();
            newAccess.Add($"Captain");

            if (ShipyardConsoleUiKey.Security == (ShipyardConsoleUiKey) args.UiKey)
            {
                newAccess.Add($"Security");
                newAccess.Add($"Brig");
            }

            _accessSystem.TrySetTags(targetId, newAccess, newCap);
        }

        var newDeed = EnsureComp<ShuttleDeedComponent>(targetId);
        var channel = _prototypeManager.Index<RadioChannelPrototype>(component.ShipyardChannel);
        newDeed.ShuttleUid = shuttle.Owner;
        newDeed.ShuttleName = name;

        if (ShipyardConsoleUiKey.Security != (ShipyardConsoleUiKey) args.UiKey)
            _idSystem.TryChangeJobTitle(targetId, $"Captain", idCard, player);

        _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-docking", ("vessel", name)), channel, uid);
        _chat.TrySendInGameICMessage(uid, Loc.GetString("shipyard-console-docking", ("vessel", name)), InGameICChatType.Speak, true);

        if (TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
                && shuttleStation !=null
                && keyStorage.Key != null
                && _records.TryGetRecord<GeneralStationRecord>(station, keyStorage.Key.Value, out var record))
        {
            _records.RemoveRecord(station, keyStorage.Key.Value);
            _records.CreateGeneralRecord((EntityUid) shuttleStation, targetId, record.Name, record.Age, record.Species, record.Gender, $"Captain", record.Fingerprint, record.DNA);
            _records.Synchronize((EntityUid) shuttleStation);
            _records.Synchronize(station);
        }

        //if (ShipyardConsoleUiKey.Security == (ShipyardConsoleUiKey) args.UiKey) Enable in the case we force this on every security ship
        //    EnsureComp<StationEmpImmuneComponent>(shuttle.Owner); Enable in the case we force this on every security ship

		int sellValue = 0;
        if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
            sellValue = (int) _pricing.AppraiseGrid((EntityUid) (deed?.ShuttleUid!));

        if (ShipyardConsoleUiKey.BlackMarket == (ShipyardConsoleUiKey) args.UiKey)
        {
            var tax = (int) (sellValue * 0.30f);
            sellValue -= tax;
        }

        PlayConfirmSound(uid, component);
        _adminLogger.Add(LogType.ShipYardUsage, LogImpact.Low, $"{ToPrettyString(player):actor} purchased shuttle {ToPrettyString(shuttle.Owner)} for {vessel.Price} credits via {ToPrettyString(component.Owner)}");
        RefreshState(uid, bank.Balance, true, name, sellValue, true, (ShipyardConsoleUiKey) args.UiKey);
    }

    public void OnSellMessage(EntityUid uid, ShipyardConsoleComponent component, ShipyardConsoleSellMessage args)
    {

        if (args.Session.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid: true } targetId)
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<IdCardComponent>(targetId, out var idCard))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<ShuttleDeedComponent>(targetId, out var deed) || deed.ShuttleUid is not { Valid : true } shuttleUid)
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-deed"));
            PlayDenySound(uid, component);
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-no-bank"));
            PlayDenySound(uid, component);
            return;
        }

        if (_station.GetOwningStation(uid) is not { Valid : true } stationUid)
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-invalid-station"));
            PlayDenySound(uid, component);
            return;
        }

        if (_station.GetOwningStation(shuttleUid) is { Valid : true } shuttleStation
            && TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            && keyStorage.Key != null
            && keyStorage.Key.Value.OriginStation == shuttleStation
            && _records.TryGetRecord<GeneralStationRecord>(shuttleStation, keyStorage.Key.Value, out var record))
        {
            _records.RemoveRecord(shuttleStation, keyStorage.Key.Value);
            _records.CreateGeneralRecord(stationUid, targetId, record.Name, record.Age, record.Species, record.Gender, $"Passenger", record.Fingerprint, record.DNA);
            _records.Synchronize(stationUid);
        }

        var shuttleName = ToPrettyString(shuttleUid); // Grab the name before it gets 1984'd

        if (!TrySellShuttle(stationUid, shuttleUid, out var bill))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-sale-reqs"));
            PlayDenySound(uid, component);
            return;
        }

        RemComp<ShuttleDeedComponent>(targetId);

        if (ShipyardConsoleUiKey.BlackMarket == (ShipyardConsoleUiKey) args.UiKey)
        {
            var tax = (int) (bill * 0.30f);
            var query = EntityQueryEnumerator<StationBankAccountComponent>();

            while (query.MoveNext(out _, out var comp))
            {
                _cargo.DeductFunds(comp, -tax);
            }

            bill -= tax;
        }

        _bank.TryBankDeposit(player, bill);
        PlayConfirmSound(uid, component);
        _adminLogger.Add(LogType.ShipYardUsage, LogImpact.Low, $"{ToPrettyString(player):actor} sold {shuttleName} for {bill} credits via {ToPrettyString(component.Owner)}");
        RefreshState(uid, bank.Balance, true, null, 0, true, (ShipyardConsoleUiKey) args.UiKey);
    }

    private void OnConsoleUIOpened(EntityUid uid, ShipyardConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (args.Session.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        //      mayhaps re-enable this later for HoS/SA
        //        var station = _station.GetOwningStation(uid);

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            return;
        }

        var targetId = component.TargetIdSlot.ContainerSlot?.ContainedEntity;
        int sellValue = 0;
        if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
            sellValue = (int) _pricing.AppraiseGrid((EntityUid) (deed?.ShuttleUid!));

        if (ShipyardConsoleUiKey.BlackMarket == (ShipyardConsoleUiKey) args.UiKey)
        {
            var tax = (int) (sellValue * 0.30f);
            sellValue -= tax;
        }

        RefreshState(uid, bank.Balance, true, deed?.ShuttleName, sellValue, targetId.HasValue, (ShipyardConsoleUiKey) args.UiKey);
    }

    private void ConsolePopup(ICommonSession session, string text)
    {
        if (session.AttachedEntity is { Valid : true } player)
            _popup.PopupEntity(text, player);
    }

    private void PlayDenySound(EntityUid uid, ShipyardConsoleComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ErrorSound), uid);
    }

    private void PlayConfirmSound(EntityUid uid, ShipyardConsoleComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ConfirmSound), uid);
    }

    private void OnItemSlotChanged(EntityUid uid, ShipyardConsoleComponent component, ContainerModifiedMessage args)
    {
        // kind of cursed. We need to update the UI when an Id is entered, but the UI needs to know the player characters bank account.
        if (!TryComp<ActivatableUIComponent>(uid, out var uiComp) || uiComp.Key == null)
        {
            return;
        }
        var shipyardUi = _ui.GetUi(uid, uiComp.Key);
        var uiUser = shipyardUi.SubscribedSessions.FirstOrDefault();

        if (uiUser?.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            return;
        }

        var targetId = component.TargetIdSlot.ContainerSlot?.ContainedEntity;
        int sellValue = 0;
        if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
            sellValue = (int) _pricing.AppraiseGrid((EntityUid) (deed?.ShuttleUid!));

        if (ShipyardConsoleUiKey.BlackMarket == (ShipyardConsoleUiKey) uiComp.Key)
        {
            var tax = (int) (sellValue * 0.30f);
            sellValue -= tax;
        }

        RefreshState(uid, bank.Balance, true, deed?.ShuttleName, sellValue, targetId.HasValue, (ShipyardConsoleUiKey) uiComp.Key);
    }

    public bool FoundOrganics(EntityUid uid, EntityQuery<MobStateComponent> mobQuery, EntityQuery<TransformComponent> xformQuery)
    {
        var xform = xformQuery.GetComponent(uid);
        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            if (mobQuery.TryGetComponent(child.Value, out var mobState)
                && !_mobState.IsDead(child.Value, mobState)
                && _mind.TryGetMind(child.Value, out var mind, out var mindComp)
                && !_mind.IsCharacterDeadIc(mindComp)
                || FoundOrganics(child.Value, mobQuery, xformQuery))
                return true;
        }

        return false;
    }

    private void RefreshState(EntityUid uid, int balance, bool access, string? shipDeed, int shipSellValue, bool isTargetIdPresent, ShipyardConsoleUiKey uiKey)
    {
        var newState = new ShipyardConsoleInterfaceState(
            balance,
            access,
            shipDeed,
            shipSellValue,
            isTargetIdPresent,
            ((byte)uiKey));

        _ui.TrySetUiState(uid, uiKey, newState);
    }
}
