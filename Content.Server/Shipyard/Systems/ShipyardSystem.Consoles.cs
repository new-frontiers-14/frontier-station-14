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
using Robust.Shared.Player;
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
using Content.Server.Forensics;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Database;
using Content.Shared.Preferences;
using Content.Shared.Shuttles.Components;
using static Content.Shared.Shipyard.Components.ShuttleDeedComponent;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using System.Text.RegularExpressions;

namespace Content.Server.Shipyard.Systems;

public sealed partial class ShipyardSystem : SharedShipyardSystem
{
    [Dependency] private readonly AccessSystem _accessSystem = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IServerPreferencesManager _prefManager = default!;
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
            return;

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

        if (!GetAvailableShuttles(uid).Contains(vessel.ID))
        {
            PlayDenySound(uid, component);
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(player):player} tried to purchase a vessel that was never available.");
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
                B = 100,
                A = 100
            });
            _shuttle.AddIFFFlag(shuttle.Owner, IFFFlags.IsPlayerShuttle);
        }

        if (TryComp<AccessComponent>(targetId, out var newCap))
        {
            var newAccess = newCap.Tags.ToList();
            newAccess.Add($"Captain");

            if (ShipyardConsoleUiKey.Security == (ShipyardConsoleUiKey) args.UiKey)
            {
                newAccess.Add($"Security");
                newAccess.Add($"Brig");
            }

            _accessSystem.TrySetTags(targetId, newAccess, newCap);
        }

        var deedID = EnsureComp<ShuttleDeedComponent>(targetId);
        AssignShuttleDeedProperties(deedID, shuttle.Owner, name, player);

        var deedShuttle = EnsureComp<ShuttleDeedComponent>(shuttle.Owner);
        AssignShuttleDeedProperties(deedShuttle, shuttle.Owner, name, player);

        var channel = component.ShipyardChannel;

        if (ShipyardConsoleUiKey.Security != (ShipyardConsoleUiKey) args.UiKey)
            _idSystem.TryChangeJobTitle(targetId, $"Captain", idCard, player);
        else
            channel = component.SecurityShipyardChannel;

        // The following block of code is entirely to do with trying to sanely handle moving records from station to station.
        // it is ass.
        // This probably shouldnt be messed with further until station records themselves become more robust
        // and not entirely dependent upon linking ID card entity to station records key lookups
        // its just bad

        var stationList = EntityQueryEnumerator<StationRecordsComponent>();

        if (TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
                && shuttleStation != null
                && keyStorage.Key != null)
        {
            bool recSuccess = false;
            while (stationList.MoveNext(out var stationUid, out var stationRecComp))
            {
                if (!_records.TryGetRecord<GeneralStationRecord>(stationUid, keyStorage.Key.Value, out var record))
                    continue;

                _records.RemoveRecord(stationUid, keyStorage.Key.Value);
                _records.CreateGeneralRecord((EntityUid) shuttleStation, targetId, record.Name, record.Age, record.Species, record.Gender, $"Captain", record.Fingerprint, record.DNA);
                recSuccess = true;
                break;
            }

            if (!recSuccess
                && _prefManager.GetPreferences(args.Session.UserId).SelectedCharacter is HumanoidCharacterProfile profile)
            {
                TryComp<FingerprintComponent>(player, out var fingerprintComponent);
                TryComp<DnaComponent>(player, out var dnaComponent);
                _records.CreateGeneralRecord((EntityUid) shuttleStation, targetId, profile.Name, profile.Age, profile.Species, profile.Gender, $"Captain", fingerprintComponent!.Fingerprint, dnaComponent!.DNA);
            }
        }
        _records.Synchronize(shuttleStation!.Value);
        _records.Synchronize(station);

        //if (ShipyardConsoleUiKey.Security == (ShipyardConsoleUiKey) args.UiKey) Enable in the case we force this on every security ship
        //    EnsureComp<StationEmpImmuneComponent>(shuttle.Owner); Enable in the case we force this on every security ship

        int sellValue = 0;
        if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
            sellValue = (int) _pricing.AppraiseGrid((EntityUid) (deed?.ShuttleUid!));

        if (ShipyardConsoleUiKey.BlackMarket == (ShipyardConsoleUiKey) args.UiKey)
        {
            var tax = (int) (sellValue * 0.30f);
            sellValue -= tax;
            channel = component.BlackMarketShipyardChannel;

            SendPurchaseMessage(uid, player, name, component.SecurityShipyardChannel, true);
        }

        SendPurchaseMessage(uid, player, name, channel, false);

        PlayConfirmSound(uid, component);
        _adminLogger.Add(LogType.ShipYardUsage, LogImpact.Low, $"{ToPrettyString(player):actor} purchased shuttle {ToPrettyString(shuttle.Owner)} for {vessel.Price} credits via {ToPrettyString(component.Owner)}");
        RefreshState(uid, bank.Balance, true, name, sellValue, true, (ShipyardConsoleUiKey) args.UiKey);
    }

    private void TryParseShuttleName(ShuttleDeedComponent deed, string name)
    {
        // The logic behind this is: if a name part fits the requirements, it is the required part. Otherwise it's the name.
        // This may cause problems but ONLY when renaming a ship. It will still display properly regardless of this.
        var nameParts = name.Split(' ');

        var hasSuffix = nameParts.Length > 1 && nameParts.Last().Length < MaxSuffixLength && nameParts.Last().Contains('-');
        deed.ShuttleNameSuffix = hasSuffix ? nameParts.Last() : null;
        deed.ShuttleName = String.Join(" ", nameParts.SkipLast(hasSuffix ? 1 : 0));
    }

    public void OnSellMessage(EntityUid uid, ShipyardConsoleComponent component, ShipyardConsoleSellMessage args)
    {

        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;

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

        var channel = component.ShipyardChannel;

        if (!TrySellShuttle(stationUid, shuttleUid, out var bill))
        {
            ConsolePopup(args.Session, Loc.GetString("shipyard-console-sale-reqs"));
            PlayDenySound(uid, component);
            return;
        }

        RemComp<ShuttleDeedComponent>(targetId);

        if (ShipyardConsoleUiKey.Security == (ShipyardConsoleUiKey) args.UiKey)
            channel = component.SecurityShipyardChannel;

        if (ShipyardConsoleUiKey.BlackMarket == (ShipyardConsoleUiKey) args.UiKey)
        {
            var tax = (int) (bill * 0.30f);
            var query = EntityQueryEnumerator<StationBankAccountComponent>();

            while (query.MoveNext(out _, out var comp))
            {
                _cargo.DeductFunds(comp, -tax);
            }

            bill -= tax;
            channel = component.BlackMarketShipyardChannel;

            SendSellMessage(uid, deed.ShuttleOwner!, GetFullName(deed), component.SecurityShipyardChannel, player, true);
        }

        _bank.TryBankDeposit(player, bill);
        PlayConfirmSound(uid, component);

        SendSellMessage(uid, deed.ShuttleOwner!, GetFullName(deed), channel, player, false);

        _adminLogger.Add(LogType.ShipYardUsage, LogImpact.Low, $"{ToPrettyString(player):actor} sold {shuttleName} for {bill} credits via {ToPrettyString(component.Owner)}");
        RefreshState(uid, bank.Balance, true, null, 0, true, (ShipyardConsoleUiKey) args.UiKey);
    }

    private void OnConsoleUIOpened(EntityUid uid, ShipyardConsoleComponent component, BoundUIOpenedEvent args)
    {
        // kind of cursed. We need to update the UI when an Id is entered, but the UI needs to know the player characters bank account.
        if (!TryComp<ActivatableUIComponent>(uid, out var uiComp) || uiComp.Key == null)
            return;

        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;

        //      mayhaps re-enable this later for HoS/SA
        //        var station = _station.GetOwningStation(uid);

        if (!TryComp<BankAccountComponent>(player, out var bank))
            return;

        var targetId = component.TargetIdSlot.ContainerSlot?.ContainedEntity;

        if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
        {
            if (Deleted(deed!.ShuttleUid))
            {
                RemComp<ShuttleDeedComponent>(targetId!.Value);
                return;
            }
        }

        int sellValue = 0;
        if (deed?.ShuttleUid != null)
            sellValue = (int) _pricing.AppraiseGrid((EntityUid) (deed?.ShuttleUid!));

        if (ShipyardConsoleUiKey.BlackMarket == (ShipyardConsoleUiKey) uiComp.Key)
        {
            var tax = (int) (sellValue * 0.30f);
            sellValue -= tax;
        }

        var fullName = deed != null ? GetFullName(deed) : null;
        RefreshState(uid, bank.Balance, true, fullName, sellValue, targetId.HasValue, (ShipyardConsoleUiKey) args.UiKey);
    }

    private void ConsolePopup(ICommonSession session, string text)
    {
        if (session.AttachedEntity is { Valid : true } player)
            _popup.PopupEntity(text, player);
    }

    private void SendPurchaseMessage(EntityUid uid, EntityUid player, string name, string shipyardChannel, bool secret)
    {
        var channel = _prototypeManager.Index<RadioChannelPrototype>(shipyardChannel);

        if (secret)
        {
            _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-docking-secret"), channel, uid);
            _chat.TrySendInGameICMessage(uid, Loc.GetString("shipyard-console-docking-secret"), InGameICChatType.Speak, true);
        }
        else
        {
            _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-docking", ("owner", player), ("vessel", name)), channel, uid);
            _chat.TrySendInGameICMessage(uid, Loc.GetString("shipyard-console-docking", ("owner", player!), ("vessel", name)), InGameICChatType.Speak, true);
        }
    }

    private void SendSellMessage(EntityUid uid, EntityUid? player, string name, string shipyardChannel, EntityUid seller, bool secret)
    {
        var channel = _prototypeManager.Index<RadioChannelPrototype>(shipyardChannel);

        if (secret)
        {
            _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-leaving-secret"), channel, uid);
            _chat.TrySendInGameICMessage(uid, Loc.GetString("shipyard-console-leaving-secret"), InGameICChatType.Speak, true);
        }
        else
        {
            _radio.SendRadioMessage(uid, Loc.GetString("shipyard-console-leaving", ("owner", player!), ("vessel", name!), ("player", seller)), channel, uid);
            _chat.TrySendInGameICMessage(uid, Loc.GetString("shipyard-console-leaving", ("owner", player!), ("vessel", name!), ("player", seller)), InGameICChatType.Speak, true);
        }
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
            return;

        var shipyardUi = _ui.GetUi(uid, uiComp.Key);
        var uiUser = shipyardUi.SubscribedSessions.FirstOrDefault();

        if (uiUser?.AttachedEntity is not { Valid: true } player)
            return;

        if (!TryComp<BankAccountComponent>(player, out var bank))
            return;

        var targetId = component.TargetIdSlot.ContainerSlot?.ContainedEntity;

        if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
        {
            if (Deleted(deed!.ShuttleUid))
            {
                RemComp<ShuttleDeedComponent>(targetId!.Value);
                return;
            }
        }

        int sellValue = 0;
        if (deed?.ShuttleUid != null)
            sellValue = (int) _pricing.AppraiseGrid((EntityUid) (deed?.ShuttleUid!));

        if (ShipyardConsoleUiKey.BlackMarket == (ShipyardConsoleUiKey) uiComp.Key)
        {
            var tax = (int) (sellValue * 0.30f);
            sellValue -= tax;
        }

        var fullName = deed != null ? GetFullName(deed) : null;
        RefreshState(uid, bank.Balance, true, fullName, sellValue, targetId.HasValue, (ShipyardConsoleUiKey) uiComp.Key);
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

    /// <summary>
    ///   Returns all shuttle prototype IDs the given shipyard console can offer.
    /// </summary>
    public List<string> GetAvailableShuttles(EntityUid uid, ShipyardConsoleUiKey? key = null, ShipyardListingComponent? listing = null)
    {
        var availableShuttles = new List<string>();

        if (key == null && TryComp<UserInterfaceComponent>(uid, out var ui))
        {
            // Try to find a ui key that is an instance of the shipyard console ui key
            foreach (var (k, v) in ui.Interfaces)
            {
                if (k is ShipyardConsoleUiKey shipyardKey)
                {
                    key = shipyardKey;
                    break;
                }
            }
        }

        // Add all prototypes matching the ui key
        if (key != null && key != ShipyardConsoleUiKey.Custom && ShipyardGroupMapping.TryGetValue(key.Value, out var group))
        {
            var protos = _prototypeManager.EnumeratePrototypes<VesselPrototype>();
            foreach (var proto in protos)
            {
                if (proto.Group == group)
                    availableShuttles.Add(proto.ID);
            }
        }

        // Add all prototypes specified in ShipyardListing
        if (listing != null || TryComp(uid, out listing))
        {
            foreach (var shuttle in listing.Shuttles)
            {
                availableShuttles.Add(shuttle);
            }
        }

        return availableShuttles;
    }

    private void RefreshState(EntityUid uid, int balance, bool access, string? shipDeed, int shipSellValue, bool isTargetIdPresent, ShipyardConsoleUiKey uiKey)
    {
        var listing = TryComp<ShipyardListingComponent>(uid, out var comp) ? comp : null;

        var newState = new ShipyardConsoleInterfaceState(
            balance,
            access,
            shipDeed,
            shipSellValue,
            isTargetIdPresent,
            ((byte)uiKey),
            GetAvailableShuttles(uid, uiKey, listing),
            uiKey.ToString());

        _ui.TrySetUiState(uid, uiKey, newState);
    }

    void AssignShuttleDeedProperties(ShuttleDeedComponent deed, EntityUid? shuttleUid, string? shuttleName, EntityUid? shuttleOwner)
    {
        deed.ShuttleUid = shuttleUid;
        TryParseShuttleName(deed, shuttleName!);
        deed.ShuttleOwner = shuttleOwner;
    }

    private void OnInitDeedSpawner(EntityUid uid, StationDeedSpawnerComponent component, MapInitEvent args)
    {
        if (!HasComp<IdCardComponent>(uid)) // Test if the deed on an ID
            return;

        var xform = Transform(uid); // Get the grid the card is on
        if (xform.GridUid == null)
            return;

        if (!TryComp<ShuttleDeedComponent>(xform.GridUid.Value, out var shuttleDeed) || !TryComp<ShuttleComponent>(xform.GridUid.Value, out var shuttle) || !HasComp<TransformComponent>(xform.GridUid.Value) || shuttle == null  || ShipyardMap == null)
            return;

        var shuttleOwner = ToPrettyString(shuttleDeed.ShuttleOwner); // Grab owner name
        var output = Regex.Replace($"{shuttleOwner}", @"\s*\([^()]*\)", ""); // Removes content inside parentheses along with parentheses and a preceding space
        _idSystem.TryChangeFullName(uid, output); // Update the card with owner name

        var deedID = EnsureComp<ShuttleDeedComponent>(uid);
        AssignShuttleDeedProperties(deedID, shuttleDeed.ShuttleUid, shuttleDeed.ShuttleName, shuttleDeed.ShuttleOwner);
    }
}
