using System.Linq;
using Content.Server.Bank;
using System.Numerics;
using Content.Server.Cargo.Systems;
using Content.Server.Emp;
using Content.Server.Cargo.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Bank.Components;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Server.VendingMachines
{
    public sealed class VendingMachineSystem : SharedVendingMachineSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly BankSystem _bankSystem = default!;
        [Dependency] private readonly CargoSystem _cargo = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _action = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("vending");
            SubscribeLocalEvent<VendingMachineComponent, MapInitEvent>(OnComponentMapInit);
            SubscribeLocalEvent<VendingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<VendingMachineComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<VendingMachineComponent, GotEmaggedEvent>(OnEmagged);
            SubscribeLocalEvent<VendingMachineComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<VendingMachineComponent, PriceCalculationEvent>(OnVendingPrice);
            SubscribeLocalEvent<VendingMachineComponent, EmpPulseEvent>(OnEmpPulse);

            SubscribeLocalEvent<VendingMachineComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
            SubscribeLocalEvent<VendingMachineComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
            SubscribeLocalEvent<VendingMachineComponent, VendingMachineEjectMessage>(OnInventoryEjectMessage);

            SubscribeLocalEvent<VendingMachineComponent, VendingMachineSelfDispenseEvent>(OnSelfDispense);

            SubscribeLocalEvent<VendingMachineComponent, RestockDoAfterEvent>(OnDoAfter);

            SubscribeLocalEvent<VendingMachineRestockComponent, PriceCalculationEvent>(OnPriceCalculation);

            SubscribeLocalEvent<VendingMachineComponent, WeldableChangedEvent>(OnWeldChanged); // Frontier - Allow repair
        }

        private void OnComponentMapInit(EntityUid uid, VendingMachineComponent component, MapInitEvent args)
        {
            _action.AddAction(uid, ref component.ActionEntity, component.Action, uid);
            Dirty(uid, component);
        }

        private void OnVendingPrice(EntityUid uid, VendingMachineComponent component, ref PriceCalculationEvent args)
        {
            var price = 0.0;

            foreach (var entry in component.Inventory.Values)
            {
                if (!PrototypeManager.TryIndex<EntityPrototype>(entry.ID, out var proto))
                {
                    _sawmill.Error($"Unable to find entity prototype {entry.ID} on {ToPrettyString(uid)} vending.");
                    continue;
                }

                price += entry.Amount; //* _pricing.GetEstimatedPrice(proto); Removed this to make machine price without the items worth.
            }

            //args.Price += price; Removed this to also make the machine price without the amount of items worth.
        }

        protected override void OnComponentInit(EntityUid uid, VendingMachineComponent component, ComponentInit args)
        {
            base.OnComponentInit(uid, component, args);

            if (HasComp<ApcPowerReceiverComponent>(uid))
            {
                TryUpdateVisualState(uid, component);
            }
        }

        private void OnActivatableUIOpenAttempt(EntityUid uid, VendingMachineComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (component.Broken)
                args.Cancel();
        }

        private void OnBoundUIOpened(EntityUid uid, VendingMachineComponent component, BoundUIOpenedEvent args)
        {
            if (args.Session.AttachedEntity is not { Valid: true } player)
                return;

            var balance = 0;

            if (TryComp<BankAccountComponent>(player, out var bank))
                balance = bank.Balance;

            UpdateVendingMachineInterfaceState(uid, component, balance);
        }

        private void UpdateVendingMachineInterfaceState(EntityUid uid, VendingMachineComponent component, int balance)
        {
            var state = new VendingMachineInterfaceState(GetAllInventory(uid, component), balance);

            _userInterfaceSystem.TrySetUiState(uid, VendingMachineUiKey.Key, state);
        }

        private void OnInventoryEjectMessage(EntityUid uid, VendingMachineComponent component, VendingMachineEjectMessage args)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            if (args.Session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                return;

            if (component.Ejecting)
                return;

            AuthorizedVend(uid, entity, args.Type, args.ID, component);
        }

        private void OnPowerChanged(EntityUid uid, VendingMachineComponent component, ref PowerChangedEvent args)
        {
            TryUpdateVisualState(uid, component);
        }

        private void OnBreak(EntityUid uid, VendingMachineComponent vendComponent, BreakageEventArgs eventArgs)
        {
            vendComponent.Broken = true;
            TryUpdateVisualState(uid, vendComponent);
            EnsureComp<WeldableComponent>(uid);
        }

        private void OnWeldChanged(EntityUid uid, VendingMachineComponent component, WeldableChangedEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out DamageableComponent? damageable) || damageable.TotalDamage == 0)
                return;

            if (component.Broken)
            {
                component.Broken = false;
                TryUpdateVisualState(uid, component);
                EntityManager.RemoveComponent<WeldableComponent>(uid);
                // Repair all damage
                _damageableSystem.SetAllDamage(uid, damageable, 0);
            }
        }

        private void OnEmagged(EntityUid uid, VendingMachineComponent component, ref GotEmaggedEvent args)
        {
            // only emag if there are emag-only items
            args.Handled = component.EmaggedInventory.Count > 0;
        }

        private void OnDamage(EntityUid uid, VendingMachineComponent component, DamageChangedEvent args)
        {
            if (component.Broken || component.DispenseOnHitCoolingDown ||
                component.DispenseOnHitChance == null || args.DamageDelta == null)
                return;

            if (args.DamageIncreased && args.DamageDelta.Total >= component.DispenseOnHitThreshold &&
                _random.Prob(component.DispenseOnHitChance.Value))
            {
                if (component.DispenseOnHitCooldown > 0f)
                    component.DispenseOnHitCoolingDown = true;
                EjectRandom(uid, throwItem: true, forceEject: true, component);
            }
        }

        private void OnSelfDispense(EntityUid uid, VendingMachineComponent component, VendingMachineSelfDispenseEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            EjectRandom(uid, throwItem: true, forceEject: false, component);
        }

        private void OnDoAfter(EntityUid uid, VendingMachineComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Used == null)
                return;

            if (!TryComp<VendingMachineRestockComponent>(args.Args.Used, out var restockComponent))
            {
                _sawmill.Error($"{ToPrettyString(args.Args.User)} tried to restock {ToPrettyString(uid)} with {ToPrettyString(args.Args.Used.Value)} which did not have a VendingMachineRestockComponent.");
                return;
            }

            TryRestockInventory(uid, component);

            Popup.PopupEntity(Loc.GetString("vending-machine-restock-done", ("this", args.Args.Used), ("user", args.Args.User), ("target", uid)), args.Args.User, PopupType.Medium);

            Audio.PlayPvs(restockComponent.SoundRestockDone, uid, AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));

            Del(args.Args.Used.Value);

            args.Handled = true;
        }

        /// <summary>
        /// Sets the <see cref="VendingMachineComponent.CanShoot"/> property of the vending machine.
        /// </summary>
        public void SetShooting(EntityUid uid, bool canShoot, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.CanShoot = canShoot;
        }

        public void Deny(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (vendComponent.Denying)
                return;

            vendComponent.Denying = true;
            Audio.PlayPvs(vendComponent.SoundDeny, uid, AudioParams.Default.WithVolume(-2f));
            TryUpdateVisualState(uid, vendComponent);
        }

        /// <summary>
        /// Checks if the user is authorized to use this vending machine
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="sender">Entity trying to use the vending machine</param>
        /// <param name="vendComponent"></param>
        public bool IsAuthorized(EntityUid uid, EntityUid sender, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return false;

            if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
                return true;

            if (_accessReader.IsAllowed(sender, uid, accessReader) || HasComp<EmaggedComponent>(uid))
                return true;

            Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-denied"), uid);
            Deny(uid, vendComponent);
            return false;
        }

        /// <summary>
        /// Tries to eject the provided item. Will do nothing if the vending machine is incapable of ejecting, already ejecting
        /// or the item doesn't exist in its inventory.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="type">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="throwItem">Whether the item should be thrown in a random direction after ejection</param>
        /// <param name="vendComponent"></param>
        public bool TryEjectVendorItem(EntityUid uid, InventoryType type, string itemId, bool throwItem, int balance, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return false;

            if (vendComponent.Ejecting || vendComponent.Broken || !this.IsPowered(uid, EntityManager))
            {
                return false;
            }

            var entry = GetEntry(uid, itemId, type, vendComponent);

            if (entry == null)
            {
                Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid);
                Deny(uid, vendComponent);
                return false;
            }

            if (entry.Amount <= 0)
            {
                Popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-out-of-stock"), uid);
                Deny(uid, vendComponent);
                return false;
            }

            if (string.IsNullOrEmpty(entry.ID))
                return false;

            if (!TryComp<TransformComponent>(vendComponent.Owner, out var transformComp))
                return false;

            // Start Ejecting, and prevent users from ordering while anim playing
            vendComponent.Ejecting = true;
            vendComponent.NextItemToEject = entry.ID;
            vendComponent.ThrowNextItem = throwItem;
            entry.Amount--;
            UpdateVendingMachineInterfaceState(uid, vendComponent, balance);
            TryUpdateVisualState(uid, vendComponent);
            Audio.PlayPvs(vendComponent.SoundVend, uid);
            return true;
        }

        /// <summary>
        /// Checks whether the user is authorized to use the vending machine, then ejects the provided item if true
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="sender">Entity that is trying to use the vending machine</param>
        /// <param name="type">The type of inventory the item is from</param>
        /// <param name="itemId">The prototype ID of the item</param>
        /// <param name="component"></param>
        public void AuthorizedVend(EntityUid uid, EntityUid sender, InventoryType type, string itemId, VendingMachineComponent component)
        {
            if (!TryComp<BankAccountComponent>(sender, out var bank))
                return;

            if (!_prototypeManager.TryIndex<EntityPrototype>(itemId, out var proto))
                return;

            var price = _pricing.GetEstimatedPrice(proto);
            // Somewhere deep in the code of pricing, a hardcoded 20 dollar value exists for anything without
            // a staticprice component for some god forsaken reason, and I cant find it or think of another way to
            // get an accurate price from a prototype with no staticprice comp.
            // this will undoubtably lead to vending machine exploits if I cant find wtf pricing system is doing.
            // also stacks, food, solutions, are handled poorly too f
            if (price == 0)
                price = 20;

            if (TryComp<MarketModifierComponent>(component.Owner, out var modifier))
                price *= modifier.Mod;

            var totalPrice = (int) price;

            // This block exists to allow the VendPrice flag to set a vending machine item price.
            var priceVend = _pricing.GetEstimatedVendPrice(proto);
            if (priceVend != null && totalPrice <= (int) priceVend)
                totalPrice = (int) priceVend;
            // This block exists to allow the VendPrice flag to set a vending machine item price.

            if (totalPrice > bank.Balance)
            {
                _popupSystem.PopupEntity(Loc.GetString("bank-insufficient-funds"), uid);
                Deny(uid, component);
                return;
            }

            if (IsAuthorized(uid, sender, component))
            {
                if (_bankSystem.TryBankWithdraw(sender, totalPrice))
                {
                    if (TryEjectVendorItem(uid, type, itemId, component.CanShoot, bank.Balance, component))
                    {
                        if (TryComp<StationBankAccountComponent>(_station.GetOwningStation(uid), out var stationBank))
                        {
                            _cargo.DeductFunds(stationBank, (int) -(Math.Floor(totalPrice * 0.65f)));
                        }
                        UpdateVendingMachineInterfaceState(uid, component, bank.Balance);
                    }
                }
            }
        }

        /// <summary>
        /// Tries to update the visuals of the component based on its current state.
        /// </summary>
        public void TryUpdateVisualState(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var finalState = VendingMachineVisualState.Normal;
            if (vendComponent.Broken)
            {
                finalState = VendingMachineVisualState.Broken;
            }
            else if (vendComponent.Ejecting)
            {
                finalState = VendingMachineVisualState.Eject;
            }
            else if (vendComponent.Denying)
            {
                finalState = VendingMachineVisualState.Deny;
            }
            else if (!this.IsPowered(uid, EntityManager))
            {
                finalState = VendingMachineVisualState.Off;
            }

            _appearanceSystem.SetData(uid, VendingMachineVisuals.VisualState, finalState);
        }

        /// <summary>
        /// Ejects a random item from the available stock. Will do nothing if the vending machine is empty.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="throwItem">Whether to throw the item in a random direction after dispensing it.</param>
        /// <param name="forceEject">Whether to skip the regular ejection checks and immediately dispense the item without animation.</param>
        /// <param name="vendComponent"></param>
        public void EjectRandom(EntityUid uid, bool throwItem, bool forceEject = false, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            if (!this.IsPowered(uid, EntityManager))
                return;

            if (vendComponent.Ejecting)
                return;

            if (vendComponent.EjectRandomCounter <= 0)
            {
                _audioSystem.PlayPvs(_audioSystem.GetSound(vendComponent.SoundDeny), uid);
                _popupSystem.PopupEntity(Loc.GetString("vending-machine-component-try-eject-access-abused"), uid, PopupType.MediumCaution);
                return;
            }

            var availableItems = GetAvailableInventory(uid, vendComponent);
            if (availableItems.Count <= 0)
                return;
            var item = _random.Pick(availableItems);

            if (forceEject)
            {
                vendComponent.NextItemToEject = item.ID;
                vendComponent.ThrowNextItem = throwItem;
                var entry = GetEntry(uid, item.ID, item.Type, vendComponent);
                if (entry != null)
                    entry.Amount--;
                EjectItem(uid, vendComponent, forceEject);
            }
            else
                TryEjectVendorItem(uid, item.Type, item.ID, throwItem, 0, vendComponent);
            vendComponent.EjectRandomCounter -= 1;
        }

        public void AddCharges(EntityUid uid, int change, VendingMachineComponent? comp = null)
        {
            if (!Resolve(uid, ref comp, false))
                return;

            var old = comp.EjectRandomCounter;
            comp.EjectRandomCounter = Math.Clamp(comp.EjectRandomCounter + change, 0, comp.EjectRandomMax);
            if (comp.EjectRandomCounter != old)
                Dirty(comp);
        }

        private void EjectItem(EntityUid uid, VendingMachineComponent? vendComponent = null, bool forceEject = false)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            // No need to update the visual state because we never changed it during a forced eject
            if (!forceEject)
                TryUpdateVisualState(uid, vendComponent);

            if (string.IsNullOrEmpty(vendComponent.NextItemToEject))
            {
                vendComponent.ThrowNextItem = false;
                return;
            }

            var ent = Spawn(vendComponent.NextItemToEject, Transform(uid).Coordinates);
            if (vendComponent.ThrowNextItem)
            {
                var range = vendComponent.NonLimitedEjectRange;
                var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                _throwingSystem.TryThrow(ent, direction, vendComponent.NonLimitedEjectForce);
            }

            vendComponent.NextItemToEject = null;
            vendComponent.ThrowNextItem = false;
        }

        private VendingMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, InventoryType type, VendingMachineComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return null;

            if (type == InventoryType.Emagged && HasComp<EmaggedComponent>(uid))
                return component.EmaggedInventory.GetValueOrDefault(entryId);

            if (type == InventoryType.Contraband && component.Contraband)
                return component.ContrabandInventory.GetValueOrDefault(entryId);

            return component.Inventory.GetValueOrDefault(entryId);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<VendingMachineComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.Ejecting)
                {
                    comp.EjectAccumulator += frameTime;
                    if (comp.EjectAccumulator >= comp.EjectDelay)
                    {
                        comp.EjectAccumulator = 0f;
                        comp.Ejecting = false;

                        EjectItem(uid, comp);
                    }
                }

                if (comp.Denying)
                {
                    comp.DenyAccumulator += frameTime;
                    if (comp.DenyAccumulator >= comp.DenyDelay)
                    {
                        comp.DenyAccumulator = 0f;
                        comp.Denying = false;

                        TryUpdateVisualState(uid, comp);
                    }
                }

                if (comp.DispenseOnHitCoolingDown)
                {
                    comp.DispenseOnHitAccumulator += frameTime;
                    if (comp.DispenseOnHitAccumulator >= comp.DispenseOnHitCooldown)
                    {
                        comp.DispenseOnHitAccumulator = 0f;
                        comp.DispenseOnHitCoolingDown = false;
                    }
                }

                // Added block for charges
                if (comp.EjectRandomCounter == comp.EjectRandomMax || _timing.CurTime < comp.NextChargeTime)
                    continue;

                AddCharges(uid, 1, comp);
                comp.NextChargeTime = _timing.CurTime + comp.RechargeDuration;
                // Added block for charges
            }
            var disabled = EntityQueryEnumerator<EmpDisabledComponent, VendingMachineComponent>();
            while (disabled.MoveNext(out var uid, out _, out var comp))
            {
                if (comp.NextEmpEject < _timing.CurTime)
                {
                    EjectRandom(uid, true, false, comp);
                    comp.NextEmpEject += TimeSpan.FromSeconds(5 * comp.EjectDelay);
                }
            }
        }

        public void TryRestockInventory(EntityUid uid, VendingMachineComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            RestockInventoryFromPrototype(uid, vendComponent);

            UpdateVendingMachineInterfaceState(uid, vendComponent, 0);
            TryUpdateVisualState(uid, vendComponent);
        }

        private void OnPriceCalculation(EntityUid uid, VendingMachineRestockComponent component, ref PriceCalculationEvent args)
        {
            List<double> priceSets = new();

            // Find the most expensive inventory and use that as the highest price.
            foreach (var vendingInventory in component.CanRestock)
            {
                double total = 0;

                if (PrototypeManager.TryIndex(vendingInventory, out VendingMachineInventoryPrototype? inventoryPrototype))
                {
                    foreach (var (item, amount) in inventoryPrototype.StartingInventory)
                    {
                        if (PrototypeManager.TryIndex(item, out EntityPrototype? entity))
                            total += _pricing.GetEstimatedPrice(entity) * amount;
                    }
                }

                priceSets.Add(total);
            }

            // This area of the code make it so the cargoblacklist gets ignored, this change was to resolve it.
            //args.Price += priceSets.Max();
            args.Price = 0;
        }

        private void OnEmpPulse(EntityUid uid, VendingMachineComponent component, ref EmpPulseEvent args)
        {
            if (!component.Broken && this.IsPowered(uid, EntityManager))
            {
                args.Affected = true;
                args.Disabled = true;
                component.NextEmpEject = _timing.CurTime;
            }
        }
    }
}
