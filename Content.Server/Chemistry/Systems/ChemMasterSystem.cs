using Content.Server.Chemistry.Components;
using Content.Server.Labels;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Chemistry.Systems
{

    /// <summary>
    /// Contains all the server-side logic for ChemMasters.
    /// <seealso cref="ChemMasterComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMasterSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!; //Frontier
        [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
        [Dependency] private readonly ItemSlotsSystem _slots = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly StorageSystem _storage = default!;
        [Dependency] private readonly LabelSystem _label = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        [ValidatePrototypeId<EntityPrototype>]
        private const string PillPrototypeId = "Pill";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterOutputToBottleMessage>(OnOutputToBottleMessage);
        }

        private void SubscribeUpdateUiState<T>(Entity<ChemMasterComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void UpdateUiState(Entity<ChemMasterComponent> ent, bool updateLabel = false)
        {
            var (owner, chemMaster) = ent;
            if (!_solution.TryGetSolution(owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;
            var inputContainer = _slots.GetItemOrNull(owner, SharedChemMaster.InputSlotName);
            _appearance.SetData(owner, ChemMasterVisualState.BeakerInserted, inputContainer.HasValue); //Frontier
            var outputContainer = _slots.GetItemOrNull(owner, SharedChemMaster.OutputSlotName);

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;

            var state = new ChemMasterBoundUserInterfaceState(
                chemMaster.Mode, BuildInputContainerInfo(inputContainer), BuildOutputContainerInfo(outputContainer),
                bufferReagents, bufferCurrentVolume, chemMaster.PillType, chemMaster.PillDosageLimit, updateLabel);

            _ui.SetUiState(owner, ChemMasterUiKey.Key, state);
        }

        private void OnSetModeMessage(Entity<ChemMasterComponent> ent, ref ChemMasterSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(ChemMasterMode), message.ChemMasterMode))
                return;

            ent.Comp.Mode = message.ChemMasterMode;
            UpdateUiState(ent);
            ClickSound(ent);
        }

        private void OnSetPillTypeMessage(Entity<ChemMasterComponent> ent, ref ChemMasterSetPillTypeMessage message)
        {
            // Ensure valid pill type. There are 20 pills selectable, 0-19.
            if (message.PillType > SharedChemMaster.PillTypes - 1)
                return;

            ent.Comp.PillType = message.PillType;
            UpdateUiState(ent);
            ClickSound(ent);
        }

        private void OnReagentButtonMessage(Entity<ChemMasterComponent> ent, ref ChemMasterReagentAmountButtonMessage message)
        {
            // Ensure the amount corresponds to one of the reagent amount buttons.
            if (!Enum.IsDefined(typeof(ChemMasterReagentAmount), message.Amount))
                return;

            switch (ent.Comp.Mode)
            {
                case ChemMasterMode.Transfer:
                    TransferReagents(ent, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                    break;
                case ChemMasterMode.Discard:
                    DiscardReagents(ent, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                    break;
                default:
                    // Invalid mode.
                    return;
            }

            ClickSound(ent);
        }

        private void TransferReagents(Entity<ChemMasterComponent> ent, ReagentId id, FixedPoint2 amount, bool fromBuffer)
        {
            var container = _slots.GetItemOrNull(ent, SharedChemMaster.InputSlotName);
            if (container is null ||
                !_solution.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
                !_solution.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
            {
                return;
            }

            if (fromBuffer) // Buffer to container
            {
                amount = FixedPoint2.Min(amount, containerSolution.AvailableVolume);
                amount = bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
                _solution.TryAddReagent(containerSoln.Value, id, amount, out var _);
            }
            else // Container to buffer
            {
                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id));
                _solution.RemoveReagent(containerSoln.Value, id, amount);
                bufferSolution.AddReagent(id, amount);
            }

            UpdateUiState(ent, updateLabel: true);
        }

        private void DiscardReagents(Entity<ChemMasterComponent> ent, ReagentId id, FixedPoint2 amount, bool fromBuffer)
        {
            if (fromBuffer)
            {
                if (_solution.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                    bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
                else
                    return;
            }
            else
            {
                var container = _slots.GetItemOrNull(ent, SharedChemMaster.InputSlotName);
                if (container is not null &&
                    _solution.TryGetFitsInDispenser(container.Value, out var containerSolution, out _))
                {
                    _solution.RemoveReagent(containerSolution.Value, id, amount);
                }
                else
                    return;
            }

            UpdateUiState(ent, updateLabel: fromBuffer);
        }

        private void OnCreatePillsMessage(Entity<ChemMasterComponent> ent, ref ChemMasterCreatePillsMessage message)
        {
            var user = message.Actor;
            var maybeContainer = _slots.GetItemOrNull(ent, SharedChemMaster.OutputSlotName);
            if (maybeContainer is not { Valid: true } container
                || !TryComp(container, out StorageComponent? storage))
            {
                return; // output can't fit pills
            }

            // Ensure the number is valid.
            if (message.Number == 0 || !_storage.HasSpace((container, storage)))
                return;

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > ent.Comp.PillDosageLimit)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            var needed = message.Dosage * message.Number;
            if (!WithdrawFromBuffer(ent, needed, user, out var withdrawal))
                return;

            _label.Label(container, message.Label);

            for (var i = 0; i < message.Number; i++)
            {
                var item = Spawn(PillPrototypeId, Transform(container).Coordinates);
                _storage.Insert(container, item, out _, user: user, storage);
                _label.Label(item, message.Label);

                _solution.EnsureSolutionEntity(item, SharedChemMaster.PillSolutionName,out var itemSolution ,message.Dosage);
                if (!itemSolution.HasValue)
                    return;

                _solution.TryAddSolution(itemSolution.Value, withdrawal.SplitSolution(message.Dosage));

                var pill = EnsureComp<PillComponent>(item);
                pill.PillType = ent.Comp.PillType;
                Dirty(item, pill);

                // Log pill creation by a user
                _adminLogger.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");
            }

            UpdateUiState(ent);
            ClickSound(ent);
        }

        private void OnOutputToBottleMessage(Entity<ChemMasterComponent> ent, ref ChemMasterOutputToBottleMessage message)
        {
            var user = message.Actor;
            var maybeContainer = _slots.GetItemOrNull(ent, SharedChemMaster.OutputSlotName);
            if (maybeContainer is not { Valid: true } container
                || !_solution.TryGetSolution(container, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
            {
                return; // output can't fit reagents
            }

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > solution.AvailableVolume)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            if (!WithdrawFromBuffer(ent, message.Dosage, user, out var withdrawal))
                return;

            _label.Label(container, message.Label);
            _solution.TryAddSolution(soln.Value, withdrawal);

            // Log bottle creation by a user
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(user):user} bottled {ToPrettyString(container):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");

            UpdateUiState(ent);
            ClickSound(ent);
        }

        private bool WithdrawFromBuffer(
            Entity<ChemMasterComponent> ent,
            FixedPoint2 neededVolume, EntityUid? user,
            [NotNullWhen(returnValue: true)] out Solution? outputSolution)
        {
            outputSolution = null;

            if (!_solution.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out _, out var solution))
            {
                return false;
            }

            if (solution.Volume == 0)
            {
                if (user.HasValue)
                    _popup.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user.Value);
                return false;
            }

            // ReSharper disable once InvertIf
            if (neededVolume > solution.Volume)
            {
                if (user.HasValue)
                    _popup.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user.Value);
                return false;
            }

            outputSolution = solution.SplitSolution(neededVolume);
            return true;
        }

        private void ClickSound(Entity<ChemMasterComponent> ent)
        {
            _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2f));
        }

        private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solution.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
            {
                return null;
            }

            return BuildContainerInfo(Name(container.Value), solution);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            var name = Name(container.Value);
            {
                if (_solution.TryGetSolution(
                        container.Value, SharedChemMaster.BottleSolutionName, out _, out var solution))
                {
                    return BuildContainerInfo(name, solution);
                }
            }

            if (!TryComp(container, out StorageComponent? storage))
                return null;

            var pills = storage.Container.ContainedEntities.Select((Func<EntityUid, (string, FixedPoint2 quantity)>) (pill =>
            {
                _solution.TryGetSolution(pill, SharedChemMaster.PillSolutionName, out _, out var solution);
                var quantity = solution?.Volume ?? FixedPoint2.Zero;
                return (Name(pill), quantity);
            })).ToList();

            return new ContainerInfo(name, _storage.GetCumulativeItemAreas((container.Value, storage)), storage.Grid.GetArea())
            {
                Entities = pills
            };
        }

        private static ContainerInfo BuildContainerInfo(string name, Solution solution)
        {
            return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
            {
                Reagents = solution.Contents
            };
        }
    }
}
