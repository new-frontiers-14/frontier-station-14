using Content.Shared.Bank;
using Content.Shared.Bank.Components;
using Content.Shared.Bank.Events;
using Content.Shared.Coordinates;
using Content.Shared.Stacks;
using Content.Server.Station.Systems;
using Content.Server.Cargo.Systems;
using Content.Server.Cargo.Components;
using Content.Shared.Bank.BUI;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Robust.Server.GameObjects;

namespace Content.Server.Bank;

public sealed partial class BankSystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;

    private void InitializeStationATM()
    {
        SubscribeLocalEvent<StationBankATMComponent, StationBankWithdrawMessage>(OnWithdraw);
        SubscribeLocalEvent<StationBankATMComponent, BoundUIOpenedEvent>(OnATMUIOpen);
    }

    private void OnWithdraw(EntityUid uid, StationBankATMComponent component, StationBankWithdrawMessage args)
    {

        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;

        // to keep the window stateful
        var bui = _uiSystem.GetUi(component.Owner, BankATMMenuUiKey.ATM);
        var station = _station.GetOwningStation(uid);
        // check for a bank account


        if (!TryComp<StationBankAccountComponent>(station, out var stationBank))
        {
            _log.Info($"station {station} has no bank account");
            ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-no-bank"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new StationBankATMMenuInterfaceState(0, false));
            return;
        }

        if (!_access.IsAllowed(player, uid))
        {
            _log.Info($"{player} tried to access stationo bank account");
            ConsolePopup(args.Session, Loc.GetString("station-bank-unauthorized"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new StationBankATMMenuInterfaceState(stationBank.Balance, false));
            return;
        }

        if (args.Description == null || args.Reason == null)
        {
            ConsolePopup(args.Session, Loc.GetString("station-bank-requires-reason"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new StationBankATMMenuInterfaceState(stationBank.Balance, _access.IsAllowed(player, uid)));
            return;
        }

        // check for sufficient funds
        if (stationBank.Balance < args.Amount || args.Amount < 0)
        {
            ConsolePopup(args.Session, Loc.GetString("bank-insufficient-funds"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new StationBankATMMenuInterfaceState(stationBank.Balance, _access.IsAllowed(player, uid)));
            return;
        }

        _cargo.DeductFunds(stationBank, args.Amount);
        ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-withdraw-successful"));
        PlayConfirmSound(uid, component);
        _log.Info($"{args.Session.UserId} {args.Session.Name} withdrew {args.Amount}, '{args.Reason}': {args.Description}");

        _adminLogger.Add(LogType.ATMUsage, LogImpact.Low, $"{ToPrettyString(player):actor} withdrew {args.Amount} from station bank account. '{args.Reason}': {args.Description}");
        //spawn the cash stack of whatever cash type the ATM is configured to.
        var stackPrototype = _prototypeManager.Index<StackPrototype>(component.CashType);
        _stackSystem.Spawn(args.Amount, stackPrototype, uid.ToCoordinates());

        _uiSystem.SetUiState(bui,
            new StationBankATMMenuInterfaceState(stationBank.Balance, _access.IsAllowed(player, uid)));
    }


    private void OnATMUIOpen(EntityUid uid, StationBankATMComponent component, BoundUIOpenedEvent args)
    {
        if (args.Session.AttachedEntity is not { Valid : true } player)
            return;

        var bui = _uiSystem.GetUi(component.Owner, BankATMMenuUiKey.ATM);
        var station = _station.GetOwningStation(uid);
        if (!TryComp<StationBankAccountComponent>(station, out var stationBank))
        {
            _log.Info($"{station} has no bank account");
            _uiSystem.SetUiState(bui,
                new StationBankATMMenuInterfaceState(0, false));
            return;
        }

        _uiSystem.SetUiState(bui,
            new StationBankATMMenuInterfaceState(stationBank.Balance, _access.IsAllowed(player, uid)));
    }

    private void PlayDenySound(EntityUid uid, StationBankATMComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ErrorSound), uid);
    }

    private void PlayConfirmSound(EntityUid uid, StationBankATMComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ConfirmSound), uid);
    }
}
