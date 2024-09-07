using Content.Server.Access.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Bank;
using Content.Server.Shipyard.Components;
using Content.Shared._NF.GameRule;
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
using Robust.Shared.Prototypes;
using Content.Shared.Radio;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Maps;
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
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Access;
using Content.Shared.Tiles;

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
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public void InitializeConsole()
    {

    }

    private void OnPurchaseMessage(EntityUid uid, ShipyardConsoleComponent component, ShipyardConsolePurchaseMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid: true } targetId)
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        TryComp<IdCardComponent>(targetId, out var idCard);
        TryComp<ShipyardVoucherComponent>(targetId, out var voucher);
        if (idCard is null && voucher is null)
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        if (HasComp<ShuttleDeedComponent>(targetId))
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-already-deeded"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        if (TryComp<AccessReaderComponent>(uid, out var accessReaderComponent) && !_access.IsAllowed(player, uid, accessReaderComponent))
        {
            ConsolePopup(args.Actor, Loc.GetString("comms-console-permission-denied"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        if (!_prototypeManager.TryIndex<VesselPrototype>(args.Vessel, out var vessel))
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-invalid-vessel", ("vessel", args.Vessel)));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        if (!GetAvailableShuttles(uid, targetId: targetId).available.Contains(vessel.ID))
        {
            PlayDenySound(args.Actor, uid, component);
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(player):player} tried to purchase a vessel that was never available.");
            return;
        }

        var name = vessel.Name;
        if (vessel.Price <= 0)
            return;

        if (_station.GetOwningStation(uid) is not { Valid: true } station)
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-invalid-station"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-no-bank"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        // Keep track of whether or not a voucher was used.
        // TODO: voucher purchase should be done in a separate function.
        bool voucherUsed = false;
        if (voucher is not null)
        {
            if (voucher!.RedemptionsLeft <= 0)
            {
                ConsolePopup(args.Actor, Loc.GetString("shipyard-console-no-voucher-redemptions"));
                PlayDenySound(args.Actor, uid, component);
                return;
            }
            else if (voucher!.ConsoleType != (ShipyardConsoleUiKey)args.UiKey)
            {
                ConsolePopup(args.Actor, Loc.GetString("shipyard-console-invalid-voucher-type"));
                PlayDenySound(args.Actor, uid, component);
                return;
            }
            voucher.RedemptionsLeft--;
            voucherUsed = true;
        }
        else
        {
            if (bank.Balance <= vessel.Price)
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
                PlayDenySound(args.Actor, uid, component);
                return;
            }

            if (!_bank.TryBankWithdraw(player, vessel.Price))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", vessel.Price)));
                PlayDenySound(args.Actor, uid, component);
                return;
            }
        }


        if (!TryPurchaseShuttle((EntityUid) station, vessel.ShuttlePath.ToString(), out var shuttle))
        {
            PlayDenySound(args.Actor, uid, component);
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
            newAccess.AddRange(component.NewAccessLevels);
            _accessSystem.TrySetTags(targetId, newAccess, newCap);
        }

        var deedID = EnsureComp<ShuttleDeedComponent>(targetId);

        var shuttleOwner = Name(args.Actor).Trim();
        AssignShuttleDeedProperties(deedID, shuttle.Owner, name, shuttleOwner);

        var deedShuttle = EnsureComp<ShuttleDeedComponent>(shuttle.Owner);
        AssignShuttleDeedProperties(deedShuttle, shuttle.Owner, name, shuttleOwner);

        if (!voucherUsed)
        {
            if (!string.IsNullOrEmpty(component.NewJobTitle))
                _idSystem.TryChangeJobTitle(targetId, component.NewJobTitle, idCard, player);
        }

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
                if (!_records.TryGetRecord<GeneralStationRecord>(keyStorage.Key.Value, out var record))
                    continue;

                //_records.RemoveRecord(keyStorage.Key.Value);
                _records.AddRecordEntry((EntityUid) shuttleStation, record);
                recSuccess = true;
                break;
            }

            if (!recSuccess &&
                _mind.TryGetMind(args.Actor, out var mindUid, out var mindComp)
                && _prefManager.GetPreferences(_mind.GetSession(mindComp)!.UserId).SelectedCharacter is HumanoidCharacterProfile profile)
            {
                TryComp<FingerprintComponent>(player, out var fingerprintComponent);
                TryComp<DnaComponent>(player, out var dnaComponent);
                TryComp<StationRecordsComponent>(shuttleStation, out var stationRec);
                _records.CreateGeneralRecord((EntityUid) shuttleStation, targetId, profile.Name, profile.Age, profile.Species, profile.Gender, $"Captain", fingerprintComponent!.Fingerprint, dnaComponent!.DNA, profile, stationRec!);
            }
        }
        _records.Synchronize(shuttleStation!.Value);
        _records.Synchronize(station);

        // Shuttle setup: add protected grid status if needed.
        if (vessel.GridProtection != GridProtectionFlags.None)
        {
            var prot = EnsureComp<ProtectedGridComponent>(shuttle.Owner);
            if (vessel.GridProtection.HasFlag(GridProtectionFlags.FloorRemoval))
                prot.PreventFloorRemoval = true;
            if (vessel.GridProtection.HasFlag(GridProtectionFlags.FloorPlacement))
                prot.PreventFloorPlacement = true;
            if (vessel.GridProtection.HasFlag(GridProtectionFlags.RcdUse))
                prot.PreventRCDUse = true;
            if (vessel.GridProtection.HasFlag(GridProtectionFlags.EmpEvents))
                prot.PreventEmpEvents = true;
            if (vessel.GridProtection.HasFlag(GridProtectionFlags.Explosions))
                prot.PreventExplosions = true;
            if (vessel.GridProtection.HasFlag(GridProtectionFlags.ArtifactTriggers))
                prot.PreventArtifactTriggers = true;
        }

        int sellValue = 0;
        if (!voucherUsed)
        {
            if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
                sellValue = (int) _pricing.AppraiseGrid((EntityUid) (deed?.ShuttleUid!));

            sellValue -= CalculateSalesTax(component, sellValue);
        }

        SendPurchaseMessage(uid, player, name, component.ShipyardChannel, secret: false);
        if (component.SecretShipyardChannel is { } secretChannel)
            SendPurchaseMessage(uid, player, name, secretChannel, secret: true);

        PlayConfirmSound(args.Actor, uid, component);
        if (voucherUsed)
            _adminLogger.Add(LogType.ShipYardUsage, LogImpact.Low, $"{ToPrettyString(player):actor} purchased shuttle {ToPrettyString(shuttle.Owner)} with a voucher via {ToPrettyString(component.Owner)}");
        else
            _adminLogger.Add(LogType.ShipYardUsage, LogImpact.Low, $"{ToPrettyString(player):actor} purchased shuttle {ToPrettyString(shuttle.Owner)} for {vessel.Price} credits via {ToPrettyString(component.Owner)}");

        RefreshState(uid, bank.Balance, true, name, sellValue, targetId, (ShipyardConsoleUiKey) args.UiKey, voucherUsed);
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

        if (args.Actor is not { Valid: true } player)
            return;

        if (component.TargetIdSlot.ContainerSlot?.ContainedEntity is not { Valid: true } targetId)
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        TryComp<IdCardComponent>(targetId, out var idCard);
        TryComp<ShipyardVoucherComponent>(targetId, out var voucher);
        if (idCard is null && voucher is null)
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-no-idcard"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        bool voucherUsed = voucher is not null;

        if (!TryComp<ShuttleDeedComponent>(targetId, out var deed) || deed.ShuttleUid is not { Valid: true } shuttleUid)
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-no-deed"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-no-bank"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        if (_station.GetOwningStation(uid) is not { Valid: true } stationUid)
        {
            ConsolePopup(args.Actor, Loc.GetString("shipyard-console-invalid-station"));
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        if (_station.GetOwningStation(shuttleUid) is { Valid: true } shuttleStation
            && TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            && keyStorage.Key != null
            && keyStorage.Key.Value.OriginStation == shuttleStation
            && _records.TryGetRecord<GeneralStationRecord>(keyStorage.Key.Value, out var record))
        {
            //_records.RemoveRecord(keyStorage.Key.Value);
            _records.AddRecordEntry(stationUid, record);
            _records.Synchronize(stationUid);
        }

        var shuttleName = ToPrettyString(shuttleUid); // Grab the name before it gets 1984'd

        var saleResult = TrySellShuttle(stationUid, shuttleUid, out var bill);
        if (saleResult.Error != ShipyardSaleError.Success)
        {
            switch (saleResult.Error)
            {
                case ShipyardSaleError.Undocked:
                    ConsolePopup(args.Actor, Loc.GetString("shipyard-console-sale-not-docked"));
                    break;
                case ShipyardSaleError.OrganicsAboard:
                    ConsolePopup(args.Actor, Loc.GetString("shipyard-console-sale-organic-aboard", ("name", saleResult.OrganicName ?? "Somebody")));
                    break;
                case ShipyardSaleError.InvalidShip:
                    ConsolePopup(args.Actor, Loc.GetString("shipyard-console-sale-invalid-ship"));
                    break;
                default:
                    ConsolePopup(args.Actor, Loc.GetString("shipyard-console-sale-unknown-reason", ("reason", saleResult.Error.ToString())));
                    break;
            }
            PlayDenySound(args.Actor, uid, component);
            return;
        }

        RemComp<ShuttleDeedComponent>(targetId);

        if (!voucherUsed)
        {
            var tax = CalculateSalesTax(component, bill);
            if (tax != 0)
            {
                var query = EntityQueryEnumerator<StationBankAccountComponent>();

                while (query.MoveNext(out _, out var comp))
                {
                    _cargo.DeductFunds(comp, -tax);
                }

                bill -= tax;
            }

            _bank.TryBankDeposit(player, bill);
            PlayConfirmSound(args.Actor, uid, component);
        }

        var name = GetFullName(deed);
        SendSellMessage(uid, deed.ShuttleOwner!, name, component.ShipyardChannel, player, secret: false);
        if (component.SecretShipyardChannel is { } secretChannel)
            SendSellMessage(uid, deed.ShuttleOwner!, name, secretChannel, player, secret: true);

        EntityUid? refreshId = targetId;

        if (voucherUsed)
        {
            _adminLogger.Add(LogType.ShipYardUsage, LogImpact.Low, $"{ToPrettyString(player):actor} sold {shuttleName} (purchased with voucher) via {ToPrettyString(component.Owner)}");

            // No uses on the voucher left, destroy it.
            if (voucher!.RedemptionsLeft <= 0 && voucher!.DestroyOnEmpty)
            {
                _entityManager.DeleteEntity(targetId);
                refreshId = null;
            }
        }
        else
            _adminLogger.Add(LogType.ShipYardUsage, LogImpact.Low, $"{ToPrettyString(player):actor} sold {shuttleName} for {bill} credits via {ToPrettyString(component.Owner)}");

        RefreshState(uid, bank.Balance, true, null, 0, refreshId, (ShipyardConsoleUiKey) args.UiKey, voucherUsed);
    }

    private void OnConsoleUIOpened(EntityUid uid, ShipyardConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!component.Initialized)
            return;

        // kind of cursed. We need to update the UI when an Id is entered, but the UI needs to know the player characters bank account.
        if (!TryComp<ActivatableUIComponent>(uid, out var uiComp) || uiComp.Key == null)
            return;

        if (args.Actor is not { Valid: true } player)
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

        var voucherUsed = HasComp<ShipyardVoucherComponent>(targetId);

        int sellValue = 0;
        if (deed?.ShuttleUid != null)
            sellValue = (int) _pricing.AppraiseGrid((EntityUid) (deed?.ShuttleUid!));

        sellValue -= CalculateSalesTax(component, sellValue);

        var fullName = deed != null ? GetFullName(deed) : null;
        RefreshState(uid, bank.Balance, true, fullName, sellValue, targetId, (ShipyardConsoleUiKey) args.UiKey, voucherUsed);
    }

    private void ConsolePopup(EntityUid uid, string text)
    {
        _popup.PopupEntity(text, uid);
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

    private void SendSellMessage(EntityUid uid, string? player, string name, string shipyardChannel, EntityUid seller, bool secret)
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

    private void PlayDenySound(EntityUid playerUid, EntityUid consoleUid, ShipyardConsoleComponent component)
    {
        _audio.PlayEntity(component.ErrorSound, playerUid, consoleUid);
    }

    private void PlayConfirmSound(EntityUid playerUid, EntityUid consoleUid, ShipyardConsoleComponent component)
    {
        _audio.PlayEntity(component.ConfirmSound, playerUid, consoleUid);
    }

    private void OnItemSlotChanged(EntityUid uid, ShipyardConsoleComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.TargetIdSlot.ID)
            return;

        // kind of cursed. We need to update the UI when an Id is entered, but the UI needs to know the player characters bank account.
        if (!TryComp<ActivatableUIComponent>(uid, out var uiComp) || uiComp.Key == null)
            return;

        var uiUsers = _ui.GetActors(uid, uiComp.Key);

        foreach (var user in uiUsers)
        {
            if (user is not { Valid: true } player)
                continue;

            if (!TryComp<BankAccountComponent>(player, out var bank))
                continue;

            var targetId = component.TargetIdSlot.ContainerSlot?.ContainedEntity;

            if (TryComp<ShuttleDeedComponent>(targetId, out var deed))
            {
                if (Deleted(deed!.ShuttleUid))
                {
                    RemComp<ShuttleDeedComponent>(targetId!.Value);
                    continue;
                }
            }

            var voucherUsed = HasComp<ShipyardVoucherComponent>(targetId);

            int sellValue = 0;
            if (deed?.ShuttleUid != null)
                sellValue = (int) _pricing.AppraiseGrid((EntityUid) (deed?.ShuttleUid!));

            sellValue -= CalculateSalesTax(component, sellValue);

            var fullName = deed != null ? GetFullName(deed) : null;
            RefreshState(uid,
                bank.Balance,
                true,
                fullName,
                sellValue,
                targetId,
                (ShipyardConsoleUiKey) uiComp.Key,
                voucherUsed);

        }
    }

    /// <summary>
    /// Looks for a living, sapient being aboard a particular entity.
    /// </summary>
    /// <param name="uid">The entity to search (e.g. a shuttle, a station)</param>
    /// <param name="mobQuery">A query to get the MobState from an entity</param>
    /// <param name="xformQuery">A query to get the transform component of an entity</param>
    /// <returns>The name of the sapient being if one was found, null otherwise.</returns>
    public string? FoundOrganics(EntityUid uid, EntityQuery<MobStateComponent> mobQuery, EntityQuery<TransformComponent> xformQuery)
    {
        var xform = xformQuery.GetComponent(uid);
        var childEnumerator = xform.ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            if (mobQuery.TryGetComponent(child, out var mobState)
                && !_mobState.IsDead(child, mobState)
                && _mind.TryGetMind(child, out var mind, out var mindComp)
                && !_mind.IsCharacterDeadIc(mindComp))
                return mindComp.CharacterName;
            else
            {
                var charName = FoundOrganics(child, mobQuery, xformQuery);
                if (charName != null)
                    return charName;
            }
        }

        return null;
    }

    private struct IDShipAccesses
    {
        public IReadOnlyCollection<ProtoId<AccessLevelPrototype>> Tags;
        public IReadOnlyCollection<ProtoId<AccessGroupPrototype>> Groups;
    }

    /// <summary>
    ///   Returns all shuttle prototype IDs the given shipyard console can offer.
    /// </summary>
    public (List<string> available, List<string> unavailable) GetAvailableShuttles(EntityUid uid, ShipyardConsoleUiKey? key = null,
        ShipyardListingComponent? listing = null, EntityUid? targetId = null)
    {
        var available = new List<string>();
        var unavailable = new List<string>();

        if (key == null && TryComp<UserInterfaceComponent>(uid, out var ui))
        {
            // Try to find a ui key that is an instance of the shipyard console ui key
            foreach (var (k, v) in ui.Actors)
            {
                if (k is ShipyardConsoleUiKey shipyardKey)
                {
                    key = shipyardKey;
                    break;
                }
            }
        }

        // No listing provided, try to get the current one from the console being used as a default.
        if (listing is null)
            TryComp(uid, out listing);

        // Construct access set from input type (voucher or ID card)
        IDShipAccesses accesses;
        bool initialHasAccess = true;
        if (TryComp<ShipyardVoucherComponent>(targetId, out var voucher))
        {
            if (voucher.ConsoleType == key)
            {
                accesses.Tags = voucher.Access;
                accesses.Groups = voucher.AccessGroups;
            }
            else
            {
                accesses.Tags = new HashSet<ProtoId<AccessLevelPrototype>>();
                accesses.Groups = new HashSet<ProtoId<AccessGroupPrototype>>();
                initialHasAccess = false;
            }
        }
        else if (TryComp<AccessComponent>(targetId, out var accessComponent))
        {
            accesses.Tags = accessComponent.Tags;
            accesses.Groups = accessComponent.Groups;
        }
        else
        {
            accesses.Tags = new HashSet<ProtoId<AccessLevelPrototype>>();
            accesses.Groups = new HashSet<ProtoId<AccessGroupPrototype>>();
        }

        foreach (var vessel in _prototypeManager.EnumeratePrototypes<VesselPrototype>())
        {
            bool hasAccess = initialHasAccess;
            // If the vessel needs access to be bought, check the user's access.
            if (!string.IsNullOrEmpty(vessel.Access))
            {
                hasAccess = false;
                // Check tags
                if (accesses.Tags.Contains(vessel.Access))
                    hasAccess = true;

                // Check each group if we haven't found access already.
                if (!hasAccess)
                {
                    foreach (var groupId in accesses.Groups)
                    {
                        var groupProto = _prototypeManager.Index(groupId);
                        if (groupProto?.Tags.Contains(vessel.Access) ?? false)
                        {
                            hasAccess = true;
                            break;
                        }
                    }
                }
            }

            // Check that the listing contains the shuttle or that the shuttle is in the group that the console is looking for
            if (listing?.Shuttles.Contains(vessel.ID) ?? false ||
                key != null && key != ShipyardConsoleUiKey.Custom &&
                vessel.Group == key)
            {
                if (hasAccess)
                    available.Add(vessel.ID);
                else
                    unavailable.Add(vessel.ID);
            }
        }

        return (available, unavailable);
    }

    private void RefreshState(EntityUid uid, int balance, bool access, string? shipDeed, int shipSellValue, EntityUid? targetId, ShipyardConsoleUiKey uiKey, bool freeListings)
    {
        var newState = new ShipyardConsoleInterfaceState(
            balance,
            access,
            shipDeed,
            shipSellValue,
            targetId.HasValue,
            ((byte)uiKey),
            GetAvailableShuttles(uid, uiKey, targetId: targetId),
            uiKey.ToString(),
            freeListings);

        _ui.SetUiState(uid, uiKey, newState);
    }

    void AssignShuttleDeedProperties(ShuttleDeedComponent deed, EntityUid? shuttleUid, string? shuttleName, string? shuttleOwner)
    {
        deed.ShuttleUid = shuttleUid;
        TryParseShuttleName(deed, shuttleName!);
        deed.ShuttleOwner = shuttleOwner;
    }

    private int CalculateSalesTax(ShipyardConsoleComponent component, int sellValue)
    {
        if (float.IsFinite(component.SalesTax) && component.SalesTax != 0f)
        {
            return (int) (sellValue * component.SalesTax);
        }
        return 0;
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

        var output = Regex.Replace($"{shuttleDeed.ShuttleOwner}", @"\s*\([^()]*\)", ""); // Removes content inside parentheses along with parentheses and a preceding space
        _idSystem.TryChangeFullName(uid, output); // Update the card with owner name

        var deedID = EnsureComp<ShuttleDeedComponent>(uid);
        AssignShuttleDeedProperties(deedID, shuttleDeed.ShuttleUid, shuttleDeed.ShuttleName, shuttleDeed.ShuttleOwner);
    }
}
