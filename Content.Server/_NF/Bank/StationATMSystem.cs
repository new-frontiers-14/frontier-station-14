/*
 * New Frontiers - This file is licensed under AGPLv3
 * Copyright (c) 2024 New Frontiers Contributors
 * See AGPLv3.txt for details.
 */
using System.Linq;
using Content.Shared._NF.Bank;
using Content.Shared._NF.Bank.BUI;
using Content.Shared._NF.Bank.Components;
using Content.Shared._NF.Bank.Events;
using Content.Shared.Access.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Stacks;
using Robust.Shared.Containers;

namespace Content.Server._NF.Bank;

public sealed partial class BankSystem
{
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

        GetInsertedCashAmount(component, out var deposit);

        if (!TryGetBalance(component.Account, out var stationBank))
        {
            _log.Info($"entity {uid} cannot read account {component.Account}. Is the bank service running?");
            ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-no-bank"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        var hasAccess = _access.IsAllowed(player, uid);
        if (!hasAccess)
        {
            _log.Info($"{player} tried to access station bank account");
            ConsolePopup(args.Actor, Loc.GetString("station-bank-unauthorized"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
            return;
        }

        if (args.Description == null || args.Reason == null)
        {
            ConsolePopup(args.Actor, Loc.GetString("station-bank-requires-reason"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
            return;
        }

        // check for sufficient funds
        if (stationBank < args.Amount || args.Amount < 0)
        {
            ConsolePopup(args.Actor, Loc.GetString("bank-insufficient-funds"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
            return;
        }

        var enumVal = ParseLedgerType($"StationWithdrawal{args.Reason}", false);
        if (!TrySectorWithdraw(component.Account, args.Amount, enumVal))
        {
            ConsolePopup(args.Actor, Loc.GetString("bank-withdraw-failed"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
            return;
        }

        ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-withdraw-successful"));
        PlayConfirmSound(uid, component);
        _log.Info($"{args.Actor} withdrew {args.Amount}, '{args.Reason}': {args.Description}");

        _adminLogger.Add(LogType.ATMUsage, LogImpact.Low, $"{ToPrettyString(player):actor} withdrew {args.Amount} from {component.Account} station bank account. '{args.Reason}': {args.Description}");
        //spawn the cash stack of whatever cash type the ATM is configured to.
        var stackPrototype = _prototypeManager.Index(component.CashType);
        var stackUid = _stackSystem.Spawn(args.Amount, stackPrototype, args.Actor.ToCoordinates());
        if (!_hands.TryPickupAnyHand(args.Actor, stackUid))
            _transform.SetLocalRotation(stackUid, Angle.Zero);

        _uiSystem.SetUiState(uid, args.UiKey,
            new StationBankATMMenuInterfaceState(stationBank - args.Amount, hasAccess, deposit));
    }

    private void OnDeposit(EntityUid uid, StationBankATMComponent component, StationBankDepositMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        // gets the money inside a cashslot of an ATM.
        // Dynamically knows what kind of cash to look for according to BankATMComponent
        GetInsertedCashAmount(component, out var deposit);

        if (!TryGetBalance(component.Account, out var stationBank))
        {
            _log.Info($"entity {uid} cannot read account {component.Account}. Is the bank service running?");
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
                new StationBankATMMenuInterfaceState(stationBank, false, deposit));
            return;
        }

        var hasAccess = _access.IsAllowed(player, uid);
        if (!hasAccess)
        {
            _log.Info($"{player} tried to access station bank account");
            ConsolePopup(args.Actor, Loc.GetString("station-bank-unauthorized"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
            return;
        }

        if (args.Description == null || args.Reason == null)
        {
            ConsolePopup(args.Actor, Loc.GetString("station-bank-requires-reason"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
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
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
            return;
        }

        // and then check them against the ATM's CashType
        if (_prototypeManager.Index(component.CashType) != _prototypeManager.Index<StackPrototype>(stackComponent.StackTypeId))
        {
            _log.Info($"{stackComponent.StackTypeId} is not {component.CashType}");
            ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-wrong-cash"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
            return;
        }

        // try to deposit the inserted cash into a player's bank acount.
        if (args.Amount <= 0)
        {
            _log.Info($"{args.Amount} is invalid");
            ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-transaction-denied"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
            return;
        }

        if (deposit < args.Amount)
        {
            _log.Debug($"Deposit: {args.Amount} is more than {deposit}, depositing all inserted cash");
            args.Amount = deposit;
        }

        var enumVal = ParseLedgerType($"StationDeposit{args.Reason}", true);
        if (!TrySectorDeposit(component.Account, args.Amount, enumVal))
        {
            ConsolePopup(args.Actor, Loc.GetString("bank-withdraw-failed"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(uid, args.UiKey,
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
            return;
        }

        ConsolePopup(args.Actor, Loc.GetString("bank-atm-menu-deposit-successful"));
        PlayConfirmSound(uid, component);
        _log.Info($"{args.Actor} deposited {args.Amount}, '{args.Reason}': {args.Description}");

        _adminLogger.Add(LogType.ATMUsage, LogImpact.Low, $"{ToPrettyString(player):actor} deposited {args.Amount} to {component.Account} station bank account. '{args.Reason}': {args.Description}");

        SetInsertedCashAmount(component, args.Amount, out int leftAmount, out bool empty);

        // yeet and delete the stack in the cash slot after success if it's worth 0
        if (empty)
            _containerSystem.CleanContainer(cashSlot);

        _uiSystem.SetUiState(uid, args.UiKey,
            new StationBankATMMenuInterfaceState(stationBank + args.Amount, hasAccess, leftAmount));
    }

    private LedgerEntryType ParseLedgerType(string name, bool isDeposit)
    {
        if (Enum.TryParse(typeof(LedgerEntryType), name, true, out var result))
            return (LedgerEntryType)result;

        // Unknown value, return default enum value.
        return isDeposit ? LedgerEntryType.StationDepositOther : LedgerEntryType.StationWithdrawalOther;
    }

    private void OnCashSlotChanged(EntityUid uid, StationBankATMComponent component, ContainerModifiedMessage args)
    {
        GetInsertedCashAmount(component, out var deposit);

        if (!TryGetBalance(component.Account, out var stationBank))
        {
            _uiSystem.SetUiState(uid, BankATMMenuUiKey.ATM,
                new StationBankATMMenuInterfaceState(stationBank, false, deposit));
            return;
        }

        // Get whether our actor has access or not.
        TryComp(uid, out UserInterfaceComponent? ui);
        var actorSet = _uiSystem.GetActors((uid, ui), BankATMMenuUiKey.ATM);
        // Nobody accessing UI
        if (actorSet.Count() <= 0)
            return;
        var hasAccess = _access.IsAllowed(actorSet.First(), uid);

        if (component.CashSlot.ContainerSlot?.ContainedEntity is not { Valid: true })
        {
            _uiSystem.SetUiState(uid, BankATMMenuUiKey.ATM,
                new StationBankATMMenuInterfaceState(stationBank, hasAccess, 0));
        }

        _uiSystem.SetUiState(uid, BankATMMenuUiKey.ATM,
            new StationBankATMMenuInterfaceState(stationBank, hasAccess, deposit));
    }

    private void OnATMUIOpen(EntityUid uid, StationBankATMComponent component, BoundUIOpenedEvent args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        GetInsertedCashAmount(component, out var deposit);

        if (!TryGetBalance(component.Account, out var stationBank))
        {
            _uiSystem.SetUiState(uid, BankATMMenuUiKey.ATM,
                new StationBankATMMenuInterfaceState(stationBank, false, deposit));
            return;
        }

        _uiSystem.SetUiState(uid, BankATMMenuUiKey.ATM,
            new StationBankATMMenuInterfaceState(stationBank, _access.IsAllowed(player, uid), deposit));
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
        _audio.PlayPvs(_audio.ResolveSound(component.ErrorSound), uid);
    }

    private void PlayConfirmSound(EntityUid uid, StationBankATMComponent component)
    {
        _audio.PlayPvs(_audio.ResolveSound(component.ConfirmSound), uid);
    }
}
