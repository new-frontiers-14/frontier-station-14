using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Labels.Components;
using Content.Shared.Chemistry.Components.SolutionManager; // Frontier
using Content.Shared.Chemistry.Components; // Frontier
using Content.Shared.Chemistry.Reagent; // Frontier
using Content.Server.Labels; // Frontier
using Content.Shared.Verbs; // Frontier
using Content.Shared.Examine; // Frontier
using Content.Server.Construction; // Frontier

namespace Content.Server.Chemistry.EntitySystems
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers.
    /// <seealso cref="ReagentDispenserComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ReagentDispenserSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SolutionTransferSystem _solutionTransferSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly OpenableSystem _openable = default!;
        [Dependency] private readonly LabelSystem _label = default!; // Frontier
        [Dependency] private readonly SharedContainerSystem _containers = default!; // Frontier

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, EntInsertedIntoContainerMessage>(OnEntInserted); // Frontier: SubscribeUpdateUiState < OnEntInserted
            SubscribeLocalEvent<ReagentDispenserComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ReagentDispenserComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternateVerb); // Frontier
            SubscribeLocalEvent<ReagentDispenserComponent, ExaminedEvent>(OnExamined); // Frontier
            SubscribeLocalEvent<ReagentDispenserComponent, RefreshPartsEvent>(OnRefreshParts); // Frontier
            SubscribeLocalEvent<ReagentDispenserComponent, UpgradeExamineEvent>(OnUpgradeExamine); // Frontier

            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);

            SubscribeLocalEvent<ReagentDispenserComponent, MapInitEvent>(OnMapInit, before: new []{typeof(ItemSlotsSystem)});
        }

        private void SubscribeUpdateUiState<T>(Entity<ReagentDispenserComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        // Frontier: auto-label on insert
        private void OnEntInserted(Entity<ReagentDispenserComponent> ent, ref EntInsertedIntoContainerMessage ev)
        {
            if (ent.Comp.AutoLabel && _solutionContainerSystem.TryGetDrainableSolution(ev.Entity, out _, out var sol))
            {
                ReagentId? reagentId = sol.GetPrimaryReagentId();
                if (reagentId != null && _prototypeManager.TryIndex<ReagentPrototype>(reagentId.Value.Prototype, out var reagent))
                {
                    var reagentQuantity = sol.GetReagentQuantity(reagentId.Value);
                    var totalQuantity = sol.Volume;
                    if (reagentQuantity == totalQuantity)
                        _label.Label(ev.Entity, reagent.LocalizedName);
                    else
                        _label.Label(ev.Entity, Loc.GetString("reagent-dispenser-component-impure-auto-label", ("reagent", reagent.LocalizedName), ("purity", 100.0f * reagentQuantity / totalQuantity)));
                }
            }

            UpdateUiState(ent);
        }

        private void OnAlternateVerb(Entity<ReagentDispenserComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
        {
            if (!ent.Comp.CanAutoLabel)
                return;

            args.Verbs.Add(new AlternativeVerb()
            {
                Act = () =>
                {
                    SetAutoLabel(ent, !ent.Comp.AutoLabel);
                },
                Text = ent.Comp.AutoLabel ?
                Loc.GetString("reagent-dispenser-component-set-auto-label-off-verb")
                : Loc.GetString("reagent-dispenser-component-set-auto-label-on-verb"),
                Priority = -1, //Not important, low priority.
            });
        }

        private void SetAutoLabel(Entity<ReagentDispenserComponent> ent, bool autoLabel)
        {
            if (!ent.Comp.CanAutoLabel)
                return;

            ent.Comp.AutoLabel = autoLabel;
        }

        private void OnExamined(Entity<ReagentDispenserComponent> ent, ref ExaminedEvent args)
        {
            if (!args.IsInDetailsRange || !ent.Comp.CanAutoLabel)
                return;

            if (ent.Comp.AutoLabel)
                args.PushMarkup(Loc.GetString("reagent-dispenser-component-examine-auto-label-on"));
            else
                args.PushMarkup(Loc.GetString("reagent-dispenser-component-examine-auto-label-off"));
        }
        // End Frontier

        private void UpdateUiState(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);

            var inventory = GetInventory(reagentDispenser);

            var state = new ReagentDispenserBoundUserInterfaceState(outputContainerInfo, GetNetEntity(outputContainer), inventory, reagentDispenser.Comp.DispenseAmount);
            _userInterfaceSystem.SetUiState(reagentDispenser.Owner, ReagentDispenserUiKey.Key, state);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var solution))
            {
                return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
                {
                    Reagents = solution.Contents
                };
            }

            return null;
        }

        private List<ReagentInventoryItem> GetInventory(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            var inventory = new List<ReagentInventoryItem>();

            for (var i = 0; i < reagentDispenser.Comp.NumSlots; i++)
            {
                var storageSlotId = ReagentDispenserComponent.BaseStorageSlotId + i;
                var storedContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser.Owner, storageSlotId);

                // Set label from manually-applied label, or metadata if unavailable
                string reagentLabel;
                if (TryComp<LabelComponent>(storedContainer, out var label) && !string.IsNullOrEmpty(label.CurrentLabel))
                    reagentLabel = label.CurrentLabel;
                else if (storedContainer != null)
                    reagentLabel = Name(storedContainer.Value);
                else
                    continue;

                // Get volume remaining and color of solution
                FixedPoint2 quantity = 0f;
                var reagentColor = Color.White;
                if (storedContainer != null && _solutionContainerSystem.TryGetDrainableSolution(storedContainer.Value, out _, out var sol))
                {
                    quantity = sol.Volume;
                    reagentColor = sol.GetColor(_prototypeManager);
                }

                inventory.Add(new ReagentInventoryItem(storageSlotId, reagentLabel, quantity, reagentColor));
            }

            return inventory;
        }

        private void OnSetDispenseAmountMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserSetDispenseAmountMessage message)
        {
            reagentDispenser.Comp.DispenseAmount = message.ReagentDispenserDispenseAmount;
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnDispenseReagentMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserDispenseReagentMessage message)
        {
            // Ensure that the reagent is something this reagent dispenser can dispense.
            var storedContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, message.SlotId);
            if (storedContainer == null)
                return;

            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true } || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            if (_solutionContainerSystem.TryGetDrainableSolution(storedContainer.Value, out var src, out _) &&
                _solutionContainerSystem.TryGetRefillableSolution(outputContainer.Value, out var dst, out _))
            {
                // force open container, if applicable, to avoid confusing people on why it doesn't dispense
                _openable.SetOpen(storedContainer.Value, true);
                _solutionTransferSystem.Transfer(reagentDispenser,
                        storedContainer.Value, src.Value,
                        outputContainer.Value, dst.Value,
                        (int)reagentDispenser.Comp.DispenseAmount);
            }

            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnClearContainerSolutionMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserClearContainerSolutionMessage message)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            if (outputContainer is not { Valid: true } || !_solutionContainerSystem.TryGetFitsInDispenser(outputContainer.Value, out var solution, out _))
                return;

            _solutionContainerSystem.RemoveAllSolution(solution.Value);
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void ClickSound(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            _audioSystem.PlayPvs(reagentDispenser.Comp.ClickSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
        }

        /// <summary>
        /// Automatically generate storage slots for all NumSlots, and fill them with their initial chemicals.
        /// The actual spawning of entities happens in ItemSlotsSystem's MapInit.
        /// </summary>
        private void OnMapInit(EntityUid uid, ReagentDispenserComponent component, MapInitEvent args)
        {
            // Frontier: set auto-labeller
            component.AutoLabel = component.CanAutoLabel;

            /* // Frontier: no need to change slots, already done through RefreshParts
            // Get list of pre-loaded containers
            List<string> preLoad = new List<string>();
            if (component.PackPrototypeId is not null
                && _prototypeManager.TryIndex(component.PackPrototypeId, out ReagentDispenserInventoryPrototype? packPrototype))
            {
                preLoad.AddRange(packPrototype.Inventory);
            }

            // Populate storage slots with base storage slot whitelist
            for (var i = 0; i < component.NumSlots; i++)
            {
                var storageSlotId = ReagentDispenserComponent.BaseStorageSlotId + i;
                ItemSlot storageComponent = new();
                storageComponent.Whitelist = component.StorageWhitelist;
                storageComponent.Swap = false;
                storageComponent.EjectOnBreak = true;

                // Check corresponding index in pre-loaded container (if exists) and set starting item
                if (i < preLoad.Count)
                    storageComponent.StartingItem = preLoad[i];

                component.StorageSlotIds.Add(storageSlotId);
                component.StorageSlots.Add(storageComponent);
                component.StorageSlots[i].Name = "Storage Slot " + (i+1);
                _itemSlotsSystem.AddItemSlot(uid, component.StorageSlotIds[i], component.StorageSlots[i]);
            }
            */ // End Frontier: no need to change slots, already done through RefreshParts

            _itemSlotsSystem.AddItemSlot(uid, SharedReagentDispenser.OutputSlotName, component.BeakerSlot);

            // Frontier: spawn slot contents
            if (component.PackPrototypeId is not null
                && _prototypeManager.TryIndex(component.PackPrototypeId, out ReagentDispenserInventoryPrototype? packPrototype))
            {
                for (var i = 0; i < packPrototype.Inventory.Count && i < component.StorageSlots.Count; i++)
                {
                    if (component.StorageSlots[i].ContainerSlot == null)
                        continue;
                    var item = Spawn(packPrototype.Inventory[i], Transform(uid).Coordinates);
                    if (!_containers.Insert(item, component.StorageSlots[i].ContainerSlot!)) // ContainerSystem.Insert is silent.
                        QueueDel(item);
                }
            }
            // End Frontier
        }

        // Frontier: upgradable parts
        private void OnRefreshParts(EntityUid uid, ReagentDispenserComponent component, RefreshPartsEvent args)
        {
            if (!args.PartRatings.TryGetValue(component.SlotUpgradeMachinePart, out float partRating))
                partRating = 1.0f;

            component.NumSlots = component.BaseNumStorageSlots + (int)(component.ExtraSlotsPerTier * (partRating - 1.0f));
            // Not enough?
            for (int i = component.StorageSlots.Count; i < component.NumSlots; i++)
            {
                var storageSlotId = ReagentDispenserComponent.BaseStorageSlotId + i;

                ItemSlot storageComponent = new();
                storageComponent.Whitelist = component.StorageWhitelist;
                storageComponent.Swap = false;
                storageComponent.EjectOnBreak = true;

                component.StorageSlotIds.Add(storageSlotId);
                component.StorageSlots.Add(storageComponent);
                component.StorageSlots[i].Name = "Storage Slot " + (i+1);
                _itemSlotsSystem.AddItemSlot(uid, component.StorageSlotIds[i], component.StorageSlots[i]);
            }
            // Too many?
            for (int i = component.StorageSlots.Count - 1; i >= component.NumSlots; i--)
            {
                _itemSlotsSystem.RemoveItemSlot(uid, component.StorageSlots[i]);
                component.StorageSlotIds.RemoveAt(i);
                component.StorageSlots.RemoveAt(i);
            }
        }

        private void OnUpgradeExamine(EntityUid uid, ReagentDispenserComponent component, UpgradeExamineEvent args)
        {
            args.AddNumberUpgrade("reagent-dispenser-component-examine-extra-slots", component.NumSlots - component.BaseNumStorageSlots);
        }
        // End Frontier
    }
}
