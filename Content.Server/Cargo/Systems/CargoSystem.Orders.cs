using System.Diagnostics.CodeAnalysis;
using Content.Server.Access.Systems;
using Content.Server.Bank;
using Content.Server.Cargo.Components;
using Content.Server.Labels.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Bank.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Server.Paper;
using Content.Shared.Access.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Cargo.Systems
{
    public sealed partial class CargoSystem
    {
        /// <summary>
        /// How much time to wait (in seconds) before increasing bank accounts balance.
        /// </summary>
        private const int Delay = 10;

        /// <summary>
        /// Keeps track of how much time has elapsed since last balance increase.
        /// </summary>
        private float _timer;

        [Dependency] private readonly BankSystem _bankSystem = default!;

        private void InitializeConsole()
        {
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleAddOrderMessage>(OnAddOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleRemoveOrderMessage>(OnRemoveOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleApproveOrderMessage>(OnApproveOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, BoundUIOpenedEvent>(OnOrderUIOpened);
            SubscribeLocalEvent<CargoOrderConsoleComponent, ComponentInit>(OnInit);
            Reset();
        }

        private void OnInit(EntityUid uid, CargoOrderConsoleComponent orderConsole, ComponentInit args)
        {
            var station = _station.GetOwningStation(uid);
            UpdateOrderState(orderConsole, station);
        }

        private void Reset()
        {
            _timer = 0;
        }

        private void UpdateConsole(float frameTime)
        {
            _timer += frameTime;

            // TODO: Doesn't work with serialization and shouldn't just be updating every delay
            // client can just interp this just fine on its own.
            while (_timer > Delay)
            {
                _timer -= Delay;

                foreach (var account in EntityQuery<StationBankAccountComponent>())
                {
                    account.Balance += account.IncreasePerSecond * Delay;
                }

                var query = EntityQueryEnumerator<CargoOrderConsoleComponent>();
                while (query.MoveNext(out var uid, out var comp))
                {
                    if (!_uiSystem.IsUiOpen(uid, CargoConsoleUiKey.Orders)) continue;

                    var station = _station.GetOwningStation(uid);
                    UpdateOrderState(comp, station);
                }
            }
        }

        #region Interface

        private void OnApproveOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleApproveOrderMessage args)
        {
            if (args.Session.AttachedEntity is not { Valid: true } player)
                return;

            if (!_accessReaderSystem.IsAllowed(player, uid))
            {
                ConsolePopup(args.Session, Loc.GetString("cargo-console-order-not-allowed"));
                PlayDenySound(uid, component);
                return;
            }

            if (!TryComp<BankAccountComponent>(player, out var bankAccount)) return;

            // No station to deduct from.
            if (!TryGetOrderDatabase(uid, out var dbUid, out var orderDatabase, component) ||  bankAccount == null)
            {
                ConsolePopup(args.Session, Loc.GetString("cargo-console-station-not-found"));
                PlayDenySound(uid, component);
                return;
            }

            // Find our order again. It might have been dispatched or approved already
            var order = orderDatabase.Orders.Find(order => args.OrderId == order.OrderId && !order.Approved);
            if (order == null)
            {
                return;
            }

            // Invalid order
            if (!_protoMan.HasIndex<EntityPrototype>(order.ProductId))
            {
                ConsolePopup(args.Session, Loc.GetString("cargo-console-invalid-product"));
                PlayDenySound(uid, component);
                return;
            }

            var amount = GetOutstandingOrderCount(orderDatabase);
            var capacity = orderDatabase.Capacity;

            // Too many orders, avoid them getting spammed in the UI.
            if (amount >= capacity)
            {
                ConsolePopup(args.Session, Loc.GetString("cargo-console-too-many"));
                PlayDenySound(uid, component);
                return;
            }

            // Cap orders so someone can't spam thousands.
            var cappedAmount = Math.Min(capacity - amount, order.OrderQuantity);

            if (cappedAmount != order.OrderQuantity)
            {
                order.OrderQuantity = cappedAmount;
                ConsolePopup(args.Session, Loc.GetString("cargo-console-snip-snip"));
                PlayDenySound(uid, component);
            }

            var cost = order.Price * order.OrderQuantity;

            // Not enough balance
            if (cost > bankAccount.Balance)
            {
                ConsolePopup(args.Session, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
                PlayDenySound(uid, component);
                return;
            }

            _idCardSystem.TryFindIdCard(player, out var idCard);
            order.SetApproverData(idCard?.FullName, idCard?.JobTitle);
            _audio.PlayPvs(_audio.GetSound(component.ConfirmSound), uid);

            // Log order approval
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(player):user} approved order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, reason:{order.Reason}] with balance at {bankAccount.Balance}");
            if (TryComp<StationBankAccountComponent>(_station.GetOwningStation(uid), out var stationBank))
            {
                DeductFunds(stationBank, (int) -(Math.Floor(cost * 0.65f)));
            }
            _bankSystem.TryBankWithdraw(player, cost);

            UpdateOrders(orderDatabase);
        }

        private void OnRemoveOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleRemoveOrderMessage args)
        {
            if (!TryGetOrderDatabase(uid, out var dbUid, out var orderDatabase, component))
                return;

            RemoveOrder(dbUid!.Value, args.OrderId, orderDatabase);
        }

        private void OnAddOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleAddOrderMessage args)
        {
            if (args.Session.AttachedEntity is not { Valid: true } player)
                return;

            if (args.Amount <= 0)
                return;

            var bank = GetBankAccount(uid, component);

            if (!HasComp<BankAccountComponent>(player) && bank == null) return;
            if (!TryGetOrderDatabase(uid, out var dbUid, out var orderDatabase, component))
                return;

            if (!_protoMan.TryIndex<CargoProductPrototype>(args.CargoProductId, out var product))
            {
                Log.Error($"Tried to add invalid cargo product {args.CargoProductId} as order!");
                return;
            }

            var data = GetOrderData(args, product, GenerateOrderId(orderDatabase));

            if (!TryAddOrder(orderDatabase.Owner, data, orderDatabase))
            {
                PlayDenySound(uid, component);
                return;
            }

            // Log order addition
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(player):user} added order [orderId:{data.OrderId}, quantity:{data.OrderQuantity}, product:{data.ProductId}, requester:{data.Requester}, reason:{data.Reason}]");

        }

        private void OnOrderUIOpened(EntityUid uid, CargoOrderConsoleComponent component, BoundUIOpenedEvent args)
        {
            var station = _station.GetOwningStation(uid);
            UpdateOrderState(component, station);
        }

        #endregion

        private void UpdateOrderState(CargoOrderConsoleComponent component, EntityUid? station)
        {
            if (!_uiSystem.TryGetUi(component.Owner, CargoConsoleUiKey.Orders, out var bui))
            {
                return;
            }

            var uiUser = bui.SubscribedSessions.FirstOrDefault();
            var balance = 0;

            if (uiUser?.AttachedEntity is not { Valid: true } player)
            {
                return;
            }

            if (Transform(component.Owner).GridUid is EntityUid stationGrid && TryComp<BankAccountComponent>(player, out var playerBank))
            {
                station = stationGrid;
                balance = playerBank.Balance;
            }
            else if (TryComp<StationBankAccountComponent>(station, out var stationBank))
            {
                balance = stationBank.Balance;
            }

            if (station == null || !TryGetOrderDatabase(station.Value, out var _, out var orderDatabase, component)) return;

            var state = new CargoConsoleInterfaceState(
                MetaData(player).EntityName,
                GetOutstandingOrderCount(orderDatabase),
                orderDatabase.Capacity,
                balance,
                orderDatabase.Orders);

            _uiSystem.SetUiState(bui, state);
        }

        private void ConsolePopup(ICommonSession session, string text)
        {
            _popup.PopupCursor(text, session);
        }

        private void PlayDenySound(EntityUid uid, CargoOrderConsoleComponent component)
        {
            _audio.PlayPvs(_audio.GetSound(component.ErrorSound), uid);
        }

        private static CargoOrderData GetOrderData(CargoConsoleAddOrderMessage args, CargoProductPrototype cargoProduct, int id)
        {
            return new CargoOrderData(id, cargoProduct.Product, cargoProduct.PointCost, args.Amount, args.Requester, args.Reason);
        }

        public static int GetOutstandingOrderCount(StationCargoOrderDatabaseComponent component)
        {
            var amount = 0;

            foreach (var order in component.Orders)
            {
                if (!order.Approved)
                    continue;
                amount += order.OrderQuantity - order.NumDispatched;
            }

            return amount;
        }

        /// <summary>
        /// Updates all of the cargo-related consoles for a particular station.
        /// This should be called whenever orders change.
        /// </summary>
        private void UpdateOrders(StationCargoOrderDatabaseComponent component)
        {
            // Order added so all consoles need updating.
            var orderQuery = AllEntityQuery<CargoOrderConsoleComponent>();

            while (orderQuery.MoveNext(out var uid, out var comp))
            {
                var station = Transform(uid).GridUid;

                if (_station.GetOwningStation(uid) is EntityUid stationComp)
                {
                    station = stationComp;
                }

                if (station != component.Owner)
                    continue;

                UpdateOrderState(comp, station);
            }

            var consoleQuery = AllEntityQuery<CargoShuttleConsoleComponent>();
            while (consoleQuery.MoveNext(out var uid, out var comp))
            {
                var station = Transform(uid).GridUid;

                if (_station.GetOwningStation(uid) is EntityUid stationComp)
                {
                    station = stationComp;
                }

                if (station != component.Owner)
                    continue;

                UpdateShuttleState(uid, station);
            }
        }

        public bool AddAndApproveOrder(
            EntityUid dbUid,
            string spawnId,
            int cost,
            int qty,
            string sender,
            string description,
            string dest,
            StationCargoOrderDatabaseComponent component
        )
        {
            DebugTools.Assert(_protoMan.HasIndex<EntityPrototype>(spawnId));
            // Make an order
            var id = GenerateOrderId(component);
            var order = new CargoOrderData(id, spawnId, cost, qty, sender, description);

            // Approve it now
            order.SetApproverData(dest, sender);

            // Log order addition
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"AddAndApproveOrder {description} added order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, reason:{order.Reason}]");

            // Add it to the list
            return TryAddOrder(dbUid, order, component);
        }

        private bool TryAddOrder(EntityUid dbUid, CargoOrderData data, StationCargoOrderDatabaseComponent component)
        {
            component.Orders.Add(data);
            UpdateOrders(component);
            return true;
        }

        private static int GenerateOrderId(StationCargoOrderDatabaseComponent orderDB)
        {
            // We need an arbitrary unique ID to identify orders, since they may
            // want to be cancelled later.
            return ++orderDB.NumOrdersCreated;
        }

        public void RemoveOrder(EntityUid dbUid, int index, StationCargoOrderDatabaseComponent orderDB)
        {
            var sequenceIdx = orderDB.Orders.FindIndex(order => order.OrderId == index);
            if (sequenceIdx != -1)
            {
                orderDB.Orders.RemoveAt(sequenceIdx);
            }
            UpdateOrders(orderDB);
        }

        public void ClearOrders(StationCargoOrderDatabaseComponent component)
        {
            if (component.Orders.Count == 0) return;

            component.Orders.Clear();
            Dirty(component);
        }

        private static bool PopFrontOrder(StationCargoOrderDatabaseComponent orderDB, [NotNullWhen(true)] out CargoOrderData? orderOut)
        {
            var orderIdx = orderDB.Orders.FindIndex(order => order.Approved);
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

        private bool FulfillOrder(StationCargoOrderDatabaseComponent orderDB, EntityCoordinates whereToPutIt,
                string? paperPrototypeToPrint)
        {
            if (PopFrontOrder(orderDB, out var order))
            {
                // Create the item itself
                var item = Spawn(order.ProductId, whereToPutIt);

                // Create a sheet of paper to write the order details on
                var printed = EntityManager.SpawnEntity(paperPrototypeToPrint, whereToPutIt);
                if (TryComp<PaperComponent>(printed, out var paper))
                {
                    // fill in the order data
                    var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId));
                    _metaSystem.SetEntityName(printed, val);

                    _paperSystem.SetContent(printed, Loc.GetString(
                                "cargo-console-paper-print-text",
                                ("orderNumber", order.OrderId),
                                ("itemName", MetaData(item).EntityName),
                                ("requester", order.Requester),
                                ("reason", order.Reason),
                                ("approver", order.Approver ?? string.Empty)),
                            paper);

                    // attempt to attach the label to the item
                    if (TryComp<PaperLabelComponent>(item, out var label))
                    {
                        _slots.TryInsert(item, label.LabelSlot, printed, null);
                    }
                }

                return true;
            }

            return false;
        }

        public void DeductFunds(StationBankAccountComponent component, int amount)
        {
            component.Balance = Math.Max(0, component.Balance - amount);
            Dirty(component);
        }

        #region Station

        private StationBankAccountComponent? GetBankAccount(EntityUid uid, CargoOrderConsoleComponent _)
        {
            var station = _station.GetOwningStation(uid);

            TryComp<StationBankAccountComponent>(station, out var bankComponent);
            return bankComponent;
        }

        private bool TryGetOrderDatabase(EntityUid uid, [MaybeNullWhen(false)] out EntityUid? dbUid, [MaybeNullWhen(false)] out StationCargoOrderDatabaseComponent dbComp, CargoOrderConsoleComponent _)
        {
            dbUid = _station.GetOwningStation(uid);
            return TryComp(dbUid, out dbComp);
        }

        #endregion
    }
}
