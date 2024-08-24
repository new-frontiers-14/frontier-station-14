/*
 * New Frontiers - This file is licensed under AGPLv3
 * Copyright (c) 2024 New Frontiers Contributors
 * See AGPLv3.txt for details.
 */
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
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Server.Bank;

public sealed partial class BankSystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;

    private void InitializeStationATM()
    {
        SubscribeLocalEvent<StationBankATMComponent, StationBankWithdrawMessage>(OnWithdraw);
        SubscribeLocalEvent<StationBankATMComponent, StationBankDepositMessage>(OnDeposit);
        SubscribeLocalEvent<StationBankATMComponent, BoundUIOpenedEvent>(OnATMUIOpen);
        SubscribeLocalEvent<StationBankATMComponent, EntInsertedIntoContainerMessage>(OnCashSlotChanged);
        SubscribeLocalEvent<StationBankATMComponent, EntRemovedFromContainerMessage>(OnCashSlotChanged);
    }

    private void OnWithdraw(EntityUid uid, StationBankATMComponent component, StationBankWithdrawMessage args)
    {

        if (args.Actor is not { Valid: true } player)
            return;

        // to keep the window stateful
        var station = _station.GetOwningStation(uid);
        // check for a bank account

        GetInsertedCashAmount(component, out var deposit);

        if (!TryComp<StationBankAccountComponent>(station, out var stationBank))
        {
            _log.Info($"station {station} has no bank account");
            ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-no-bank"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        if (!_access.IsAllowed(player, uid))
        {
            _log.Info($"{player} tried to access stationo bank account");
            ConsolePopup(args.Actor, Loc.GetString("station-bank-unauthorized"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank.Balance, false, deposit));
            return;
        }

        if (args.Description == null || args.Reason == null)
        {
            ConsolePopup(args.Actor, Loc.GetString("station-bank-requires-reason"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank.Balance, _access.IsAllowed(player, uid), deposit));
            return;
        }

        // check for sufficient funds
        if (stationBank.Balance < args.Amount || args.Amount < 0)
        {
            ConsolePopup(args.Actor, Loc.GetString("bank-insufficient-funds"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank.Balance, _access.IsAllowed(player, uid), deposit));
            return;
        }

        _cargo.DeductFunds(stationBank, args.Amount);
        ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-withdraw-successful"));
        PlayConfirmSound(uid, component);
        _log.Info($"{args.Actor} withdrew {args.Amount}, '{args.Reason}': {args.Description}");

        _adminLogger.Add(LogType.ATMUsage, LogImpact.Low, $"{ToPrettyString(player):actor} withdrew {args.Amount} from station bank account. '{args.Reason}': {args.Description}");
        //spawn the cash stack of whatever cash type the ATM is configured to.
        var stackPrototype = _prototypeManager.Index<StackPrototype>(component.CashType);
        _stackSystem.Spawn(args.Amount, stackPrototype, uid.ToCoordinates());

        _uiSystem.SetUiState(uid, args.UiKey,
            new StationBankATMMenuInterfaceState(stationBank.Balance, _access.IsAllowed(player, uid), deposit));
    }

    private void OnDeposit(EntityUid uid, StationBankATMComponent component, StationBankDepositMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        // to keep the window stateful
        var station = _station.GetOwningStation(uid);
        // check for a bank account

        // gets the money inside a cashslot of an ATM.
        // Dynamically knows what kind of cash to look for according to BankATMComponent
        GetInsertedCashAmount(component, out var deposit);

        if (!TryComp<StationBankAccountComponent>(station, out var stationBank))
        {
            _log.Info($"station {station} has no bank account");
            ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-no-bank"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        // validating the cash slot was setup correctly in the yaml
        if (component.CashSlot.ContainerSlot is not BaseContainer cashSlot)
        {
            _log.Info($"ATM has no cash slot");
            ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-no-bank"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        if (!_access.IsAllowed(player, uid))
        {
            _log.Info($"{player} tried to access stationo bank account");
            ConsolePopup(args.Actor, Loc.GetString("station-bank-unauthorized"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank.Balance, false, deposit));
            return;
        }

        if (args.Description == null || args.Reason == null)
        {
            ConsolePopup(args.Actor, Loc.GetString("station-bank-requires-reason"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank.Balance, _access.IsAllowed(player, uid), deposit));
            return;
        }

        // validate stack prototypes
        if (!TryComp<StackComponent>(component.CashSlot.ContainerSlot.ContainedEntity, out var stackComponent) ||
            stackComponent.StackTypeId == null)
        {
            _log.Info($"ATM cash slot contains bad stack prototype");
            ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-wrong-cash"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        // and then check them against the ATM's CashType
        if (_prototypeManager.Index<StackPrototype>(component.CashType) != _prototypeManager.Index<StackPrototype>(stackComponent.StackTypeId))
        {
            _log.Info($"{stackComponent.StackTypeId} is not {component.CashType}");
            ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-wrong-cash"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        // try to deposit the inserted cash into a player's bank acount.
        if (args.Amount <= 0)
        {
            _log.Info($"{args.Amount} is invalid");
            ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-transaction-denied"));
            PlayDenySound(uid, component);
            return;
        }

        if (deposit < args.Amount)
        {
            _log.Info($"{args.Amount} is more then {deposit}");
            ConsolePopup(args.Actor, Loc.GetString("bank-insufficient-funds"));
            PlayDenySound(uid, component);
            return;
        }

        _cargo.DeductFunds(stationBank, -args.Amount);
        ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-deposit-successful"));
        PlayConfirmSound(uid, component);
        _log.Info($"{args.Actor} deposited {args.Amount}, '{args.Reason}': {args.Description}");

        _adminLogger.Add(LogType.ATMUsage, LogImpact.Low, $"{ToPrettyString(player):actor} deposited {args.Amount} to station bank account. '{args.Reason}': {args.Description}");

        SetInsertedCashAmount(component, args.Amount, out int leftAmount, out bool empty);

        // yeet and delete the stack in the cash slot after success if its worth 0
        if (empty)
            _containerSystem.CleanContainer(cashSlot);

        _uiSystem.SetUiState(uid, args.UiKey,
            new StationBankATMMenuInterfaceState(stationBank.Balance, _access.IsAllowed(player, uid), leftAmount));
    }

    private void OnCashSlotChanged(EntityUid uid, StationBankATMComponent component, ContainerModifiedMessage args)
    {
        GetInsertedCashAmount(component, out var deposit);
        var station = _station.GetOwningStation(uid);

        if (!TryComp<StationBankAccountComponent>(station, out var bank))
        {
            return;
        }

        if (component.CashSlot.ContainerSlot?.ContainedEntity is not { Valid: true } cash)
        {
            _uiSystem.SetUiState(uid, BankATMMenuUiKey.ATM,
                new StationBankATMMenuInterfaceState(bank.Balance, true, 0));
        }

        _uiSystem.SetUiState(uid, BankATMMenuUiKey.ATM,
            new StationBankATMMenuInterfaceState(bank.Balance, true, deposit));
    }

    private void OnATMUIOpen(EntityUid uid, StationBankATMComponent component, BoundUIOpenedEvent args)
    {
        if (args.Actor is not { Valid : true } player)
            return;

        GetInsertedCashAmount(component, out var deposit);
        var station = _station.GetOwningStation(uid);

        if (!TryComp<StationBankAccountComponent>(station, out var stationBank))
        {
            _log.Info($"{station} has no bank account");
            _uiSystem.SetUiState(uid, BankATMMenuUiKey.ATM,
                new StationBankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        _uiSystem.SetUiState(uid, BankATMMenuUiKey.ATM,
            new StationBankATMMenuInterfaceState(stationBank.Balance, _access.IsAllowed(player, uid), deposit));
    }

    private void GetInsertedCashAmount(StationBankATMComponent component, out int amount)
    {
        amount = 0;
        var cashEntity = component.CashSlot.ContainerSlot?.ContainedEntity;

        // Nothing inserted: amount should be 0.
        if (cashEntity == null)
            return;

        // Invalid item inserted (doubloons, FUC, telecrystals...): amount should be negative (to denote an error)
        if (!TryComp<StackComponent>(cashEntity, out var cashStack) ||
            cashStack.StackTypeId != component.CashType)
        {
            amount = -1;
            return;
        }

        // Valid amount: output the stack's value.
        amount = cashStack.Count;
        return;
    }

    private void SetInsertedCashAmount(StationBankATMComponent component, int amount, out int leftAmount, out bool empty)
    {
        leftAmount = 0;
        empty = false;
        var cashEntity = component.CashSlot.ContainerSlot?.ContainedEntity;

        if (!TryComp<StackComponent>(cashEntity, out var cashStack) ||
            cashStack.StackTypeId != component.CashType)
        {
            return;
        }

        int newAmount = cashStack.Count;
        cashStack.Count = newAmount - amount;
        leftAmount = cashStack.Count;

        if (cashStack.Count <= 0)
            empty = true;

        return;
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
