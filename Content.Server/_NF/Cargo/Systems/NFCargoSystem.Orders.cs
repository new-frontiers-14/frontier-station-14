using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._NF.Cargo.Components;
using Content.Shared._NF.Bank.BUI;
using Content.Shared._NF.Bank.Components;
using Content.Shared._NF.Cargo;
using Content.Shared._NF.Cargo.Components;
using Content.Shared._NF.Cargo.BUI;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;
using Robust.Shared.Map;

namespace Content.Server._NF.Cargo.Systems;

public sealed partial class NFCargoSystem
{
    /// <summary>
    /// How much time to wait (in seconds) before processing database orders.
    /// </summary>
    private const int Delay = 10;

    /// <summary>
    /// Keeps track of how much time has elapsed since last balance increase.
    /// </summary>
    private float _timer;


    public void InitializeConsole()
    {
        SubscribeLocalEvent<NFCargoOrderConsoleComponent, CargoConsoleAddOrderMessage>(OnAddOrderMessage);
        SubscribeLocalEvent<NFCargoOrderConsoleComponent, BoundUIOpenedEvent>(OnOrderUIOpened);
        SubscribeLocalEvent<NFCargoOrderConsoleComponent, ComponentInit>(OnInit);
        ResetOrders();
    }

    public void ResetOrders()
    {
        _timer = 0;
    }

    private void OnInit(EntityUid uid, NFCargoOrderConsoleComponent orderConsole, ComponentInit args)
    {
        var station = _station.GetOwningStation(uid);
        UpdateOrderState((uid, orderConsole), station);
    }

    private void UpdateConsole(float frameTime)
    {
        _timer += frameTime;

        // TODO: Doesn't work with serialization and shouldn't just be updating every delay
        // client can just interp this just fine on its own.
        while (_timer > Delay)
        {
            _timer -= Delay;

            var query = EntityQueryEnumerator<NFCargoOrderConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (!_ui.IsUiOpen(uid, NFCargoConsoleUiKey.Orders)) continue;

                var station = _station.GetOwningStation(uid);
                UpdateOrderState((uid, comp), station);
            }
        }
    }

    #region Interface
    private void OnAddOrderMessage(Entity<NFCargoOrderConsoleComponent> ent, ref CargoConsoleAddOrderMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (args.Amount <= 0)
            return;

        if (!_accessReader.IsAllowed(player, ent))
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
            PlayDenySound(ent);
            return;
        }

        if (!HasComp<BankAccountComponent>(player))
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-nf-no-bank-account"));
            PlayDenySound(ent);
            return;
        }

        if (!TryGetOrderDatabase(ent, out var dbUid, out var orderDatabase))
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-station-not-found"));
            PlayDenySound(ent);
            return;
        }

        if (!_proto.TryIndex<CargoProductPrototype>(args.CargoProductId, out var product))
        {
            Log.Error($"Tried to add invalid cargo product {args.CargoProductId} as order!");
            return;
        }

        if (!ent.Comp.AllowedGroups.Contains(product.Group))
            return;

        var data = GetOrderData(EntityManager.GetNetEntity(ent), args, product, GenerateOrderId(orderDatabase));

        var amount = GetOutstandingOrderCount(orderDatabase);
        var capacity = orderDatabase.Capacity;

        // Too many orders, avoid them getting spammed in the UI.
        if (amount >= capacity)
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-too-many"));
            PlayDenySound(ent);
            return;
        }

        // Cap orders so someone can't spam thousands.
        data.OrderQuantity = Math.Min(capacity - amount, data.OrderQuantity);

        var cost = data.Price * data.OrderQuantity;

        // Not enough balance
        if (!_bank.TryBankWithdraw(player, cost))
        {
            ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
            PlayDenySound(ent);
            return;
        }

        // Give a stipend to station accounts for vendor purchases
        foreach (var (account, taxCoeff) in ent.Comp.TaxAccounts)
        {
            if (!float.IsFinite(taxCoeff) || taxCoeff <= 0.0f)
                continue;
            var tax = (int)Math.Floor(cost * taxCoeff);
            _bank.TrySectorDeposit(account, tax, LedgerEntryType.CargoTax);
        }

        AddOrder(dbUid.Value, data, orderDatabase);

        // Log order addition
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(player):user} placed an order [orderId:{data.OrderId}, quantity:{data.OrderQuantity}, product:{data.ProductId}, purchaser:{data.Purchaser}, notes:{data.Notes}]");

        _audio.PlayPvs(ent.Comp.ConfirmSound, ent);
    }

    private void OnOrderUIOpened(Entity<NFCargoOrderConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        var station = _station.GetOwningStation(ent);
        UpdateOrderState(ent, station);
    }

    #endregion

    private void UpdateOrderState(Entity<NFCargoOrderConsoleComponent> ent, EntityUid? station)
    {
        if (!TryComp(ent, out TransformComponent? xform) || xform.GridUid is not { } stationGrid)
            return;

        var uiUsers = _ui.GetActors((ent, null), NFCargoConsoleUiKey.Orders);
        foreach (var user in uiUsers)
        {
            if (!TryComp(user, out MetaDataComponent? meta))
                continue;

            var balance = 0;
            if (TryComp<BankAccountComponent>(user, out var playerBank))
                balance = playerBank.Balance;

            if (station == null || !TryGetOrderDatabase(station.Value, out var _, out var orderDatabase))
                continue;

            // We only want to see orders made on the same computer, so filter them out
            var filteredOrders = orderDatabase.Orders
                .Where(order => order.Computer == EntityManager.GetNetEntity(ent)).ToList();

            var state = new NFCargoConsoleInterfaceState(
                meta.EntityName,
                GetOutstandingOrderCount(orderDatabase),
                orderDatabase.Capacity,
                balance,
                filteredOrders);

            _ui.SetUiState(ent.Owner, NFCargoConsoleUiKey.Orders, state);
        }
    }

    private void ConsolePopup(EntityUid actor, string text)
    {
        _popup.PopupCursor(text, actor);
    }

    private void PlayDenySound(Entity<NFCargoOrderConsoleComponent> ent)
    {
        if (_timing.CurTime >= ent.Comp.NextDenySoundTime)
        {
            ent.Comp.NextDenySoundTime = _timing.CurTime + ent.Comp.DenySoundDelay;
            _audio.PlayPvs(_audio.ResolveSound(ent.Comp.ErrorSound), ent);
        }
    }

    private static NFCargoOrderData GetOrderData(NetEntity consoleUid, CargoConsoleAddOrderMessage args, CargoProductPrototype cargoProduct, int id)
    {
        return new NFCargoOrderData(id, cargoProduct.Product, cargoProduct.Name, cargoProduct.Cost, args.Amount, args.Requester, args.Reason, consoleUid);
    }

    public static int GetOutstandingOrderCount(NFStationCargoOrderDatabaseComponent component)
    {
        var amount = 0;

        foreach (var order in component.Orders)
            amount += order.OrderQuantity - order.NumDispatched;

        return amount;
    }

    /// <summary>
    /// Updates all of the cargo-related consoles for a particular station.
    /// This should be called whenever orders change.
    /// </summary>
    private void UpdateOrders(EntityUid dbUid)
    {
        // Order added so all consoles need updating.
        var orderQuery = AllEntityQuery<NFCargoOrderConsoleComponent>();

        while (orderQuery.MoveNext(out var uid, out var comp))
        {
            var station = _station.GetOwningStation(uid);
            if (station != dbUid)
                continue;

            UpdateOrderState((uid, comp), station);
        }
    }

    private void AddOrder(EntityUid dbUid, NFCargoOrderData data, NFStationCargoOrderDatabaseComponent component)
    {
        component.Orders.Add(data);
        UpdateOrders(dbUid);
    }

    private static int GenerateOrderId(NFStationCargoOrderDatabaseComponent orderDB)
    {
        // We need an arbitrary unique ID to identify orders, since they may
        // want to be cancelled later.
        return ++orderDB.NumOrdersCreated;
    }

    public void RemoveOrder(EntityUid dbUid, int index, NFStationCargoOrderDatabaseComponent orderDB)
    {
        var sequenceIdx = orderDB.Orders.FindIndex(order => order.OrderId == index);
        if (sequenceIdx != -1)
        {
            orderDB.Orders.RemoveAt(sequenceIdx);
        }
        UpdateOrders(dbUid);
    }

    public void ClearOrders(NFStationCargoOrderDatabaseComponent component)
    {
        if (component.Orders.Count == 0)
            return;

        component.Orders.Clear();
    }

    private static bool PopFrontOrder(List<NetEntity> consoleUidList, NFStationCargoOrderDatabaseComponent orderDB, [NotNullWhen(true)] out NFCargoOrderData? orderOut)
    {
        var orderIdx = orderDB.Orders.FindIndex(order => consoleUidList.Any(consoleUid => consoleUid == order.Computer));
        if (orderIdx == -1)
        {
            orderOut = null;
            return false;
        }

        orderOut = orderDB.Orders[orderIdx];
        orderOut.NumDispatched++;

        if (orderOut.NumDispatched >= orderOut.OrderQuantity)
        {
            // Order is complete. Remove from the queue.
            orderDB.Orders.RemoveAt(orderIdx);
        }
        return true;
    }

    /// <summary>
    /// Tries to fulfill the next outstanding order.
    /// </summary>
    private bool FulfillNextOrder(List<NetEntity> consoleUidList, NFStationCargoOrderDatabaseComponent orderDB, EntityCoordinates spawn, string? paperProto)
    {
        if (!PopFrontOrder(consoleUidList, orderDB, out var order))
            return false;

        return FulfillOrder(order, spawn, paperProto);
    }

    /// <summary>
    /// Fulfills the specified cargo order and spawns paper attached to it.
    /// </summary>
    private bool FulfillOrder(NFCargoOrderData order, EntityCoordinates spawn, string? paperProto)
    {
        // Create the item itself
        var item = Spawn(order.ProductId, spawn);

        // Ensure the item doesn't start anchored
        _transform.Unanchor(item, Transform(item));

        // Create a sheet of paper to write the order details on
        var printed = EntityManager.SpawnEntity(paperProto, spawn);
        if (TryComp<PaperComponent>(printed, out var paper))
        {
            // fill in the order data
            var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId));
            _meta.SetEntityName(printed, val);

            _paper.SetContent((printed, paper), Loc.GetString(
                    "cargo-console-nf-paper-print-text",
                    ("orderNumber", order.OrderId),
                    ("itemName", MetaData(item).EntityName),
                    ("orderIndex", order.NumDispatched),
                    ("orderQuantity", order.OrderQuantity),
                    ("purchaser", order.Purchaser),
                    ("notes", order.Notes)));

            // attempt to attach the label to the item
            if (TryComp<PaperLabelComponent>(item, out var label))
            {
                _slots.TryInsert(item, label.LabelSlot, printed, null);
            }
        }

        return true;

    }

    private bool TryGetOrderDatabase(EntityUid uid, [NotNullWhen(true)] out EntityUid? dbUid, [NotNullWhen(true)] out NFStationCargoOrderDatabaseComponent? dbComp)
    {
        dbUid = _station.GetOwningStation(uid);
        return TryComp(dbUid, out dbComp);
    }
}
