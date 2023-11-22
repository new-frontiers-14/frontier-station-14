using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.Bank;
using Content.Shared.Bank.BUI;
using Content.Shared.Bank.Components;
using Content.Shared.Bank.Events;
using Content.Shared.Coordinates;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Components;
using Content.Shared.Database;

namespace Content.Server.Bank;

public sealed partial class BankSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private void InitializeATM()
    {
        SubscribeLocalEvent<BankATMComponent, BankWithdrawMessage>(OnWithdraw);
        SubscribeLocalEvent<BankATMComponent, BankDepositMessage>(OnDeposit);
        SubscribeLocalEvent<BankATMComponent, BoundUIOpenedEvent>(OnATMUIOpen);
        SubscribeLocalEvent<BankATMComponent, EntInsertedIntoContainerMessage>(OnCashSlotChanged);
        SubscribeLocalEvent<BankATMComponent, EntRemovedFromContainerMessage>(OnCashSlotChanged);
    }

    private void OnWithdraw(EntityUid uid, BankATMComponent component, BankWithdrawMessage args)
    {

        if (args.Session.AttachedEntity is not { Valid : true } player)
            return;

        // to keep the window stateful
        GetInsertedCashAmount(component, out var deposit);
        if (!_uiSystem.TryGetUi(uid, args.UiKey, out var bui))
        {
            return;
        }

            // check for a bank account
        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            _log.Info($"{player} has no bank account");
            ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-no-bank"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        // check for sufficient funds
        if (bank.Balance < args.Amount)
        {
            ConsolePopup(args.Session, Loc.GetString("bank-insufficient-funds"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(bank.Balance, true, deposit));
            return;
        }

        // try to actually withdraw from the bank. Validation happens on the banking system but we still indicate error.
        if (!TryBankWithdraw(player, args.Amount))
        {
            ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-transaction-denied"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(bank.Balance, true, deposit));
            return;
        }

        ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-withdraw-successful"));
        PlayConfirmSound(uid, component);
        _adminLogger.Add(LogType.ATMUsage, LogImpact.Low, $"{ToPrettyString(player):actor} withdrew {args.Amount} from {ToPrettyString(component.Owner)}");

        //spawn the cash stack of whatever cash type the ATM is configured to.
        var stackPrototype = _prototypeManager.Index<StackPrototype>(component.CashType);
        _stackSystem.Spawn(args.Amount, stackPrototype, uid.ToCoordinates());

        _uiSystem.SetUiState(bui,
            new BankATMMenuInterfaceState(bank.Balance, true, deposit));
    }

    private void OnDeposit(EntityUid uid, BankATMComponent component, BankDepositMessage args)
    {
        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;

        // gets the money inside a cashslot of an ATM.
        // Dynamically knows what kind of cash to look for according to BankATMComponent
        GetInsertedCashAmount(component, out var deposit);

        var bui = _uiSystem.GetUi(component.Owner, args.UiKey);

        // make sure the user actually has a bank
        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            _log.Info($"{player} has no bank account");
            ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-no-bank"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        // validating the cash slot was setup correctly in the yaml
        if (component.CashSlot.ContainerSlot is not BaseContainer cashSlot)
        {
            _log.Info($"ATM has no cash slot");
            ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-no-bank"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        // validate stack prototypes
        if (!TryComp<StackComponent>(component.CashSlot.ContainerSlot.ContainedEntity, out var stackComponent) ||
            stackComponent.StackTypeId == null)
        {
            _log.Info($"ATM cash slot contains bad stack prototype");
            ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-wrong-cash"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        // and then check them against the ATM's CashType
        if (_prototypeManager.Index<StackPrototype>(component.CashType) != _prototypeManager.Index<StackPrototype>(stackComponent.StackTypeId))
        {
            _log.Info($"{stackComponent.StackTypeId} is not {component.CashType}");
            ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-wrong-cash"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        if (BankATMMenuUiKey.BlackMarket == (BankATMMenuUiKey) args.UiKey)
        {
            var tax = (int) (deposit * 0.30f);
            var query = EntityQueryEnumerator<StationBankAccountComponent>();

            while (query.MoveNext(out _, out var comp))
            {
                _cargo.DeductFunds(comp, -tax);
            }

            deposit -= tax;
        }

        // try to deposit the inserted cash into a player's bank acount. Validation happens on the banking system but we still indicate error.
        if (!TryBankDeposit(player, deposit))
        {
            ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-transaction-denied"));
            PlayDenySound(uid, component);
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(bank.Balance, true, deposit));
            return;
        }

        ConsolePopup(args.Session, Loc.GetString("bank-atm-menu-deposit-successful"));
        PlayConfirmSound(uid, component);
        _adminLogger.Add(LogType.ATMUsage, LogImpact.Low, $"{ToPrettyString(player):actor} deposited {deposit} into {ToPrettyString(component.Owner)}");

        // yeet and delete the stack in the cash slot after success
        _containerSystem.CleanContainer(cashSlot);
        _uiSystem.SetUiState(bui,
            new BankATMMenuInterfaceState(bank.Balance, true, 0));
        return;
    }

    private void OnCashSlotChanged(EntityUid uid, BankATMComponent component, ContainerModifiedMessage args)
    {
        var bankUi = _uiSystem.GetUiOrNull(uid, BankATMMenuUiKey.ATM) ?? _uiSystem.GetUiOrNull(uid, BankATMMenuUiKey.BlackMarket);

        var uiUser = bankUi!.SubscribedSessions.FirstOrDefault();
        GetInsertedCashAmount(component, out var deposit);

        if (uiUser?.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            return;
        }

        if (component.CashSlot.ContainerSlot?.ContainedEntity is not { Valid : true } cash)
        {
            _uiSystem.SetUiState(bankUi,
                new BankATMMenuInterfaceState(bank.Balance, true, 0));
        }

        _uiSystem.SetUiState(bankUi,
            new BankATMMenuInterfaceState(bank.Balance, true, deposit));
    }

    private void OnATMUIOpen(EntityUid uid, BankATMComponent component, BoundUIOpenedEvent args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;

        GetInsertedCashAmount(component, out var deposit);
        var bui = _uiSystem.GetUi(component.Owner, args.UiKey);

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            _log.Info($"{player} has no bank account");
            _uiSystem.SetUiState(bui,
                new BankATMMenuInterfaceState(0, false, deposit));
            return;
        }

        _uiSystem.SetUiState(bui,
            new BankATMMenuInterfaceState(bank.Balance, true, deposit));
    }

    private void GetInsertedCashAmount(BankATMComponent component, out int amount)
    {
        amount = 0;
        var cashEntity = component.CashSlot.ContainerSlot?.ContainedEntity;

        if (!TryComp<StackComponent>(cashEntity, out var cashStack) ||
            cashStack.StackTypeId != component.CashType)
        {
            return;
        }

        amount = cashStack.Count;
        return;
    }

    private void PlayDenySound(EntityUid uid, BankATMComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ErrorSound), uid);
    }

    private void PlayConfirmSound(EntityUid uid, BankATMComponent component)
    {
        _audio.PlayPvs(_audio.GetSound(component.ConfirmSound), uid);
    }

    private void ConsolePopup(ICommonSession session, string text)
    {
        if (session.AttachedEntity is { Valid: true } player)
            _popup.PopupEntity(text, player);
    }
}
