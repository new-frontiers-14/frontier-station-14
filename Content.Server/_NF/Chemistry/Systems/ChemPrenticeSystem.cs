using Content.Server._NF.Chemistry.Components;
using Content.Shared._NF.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server._NF.Chemistry.Systems
{

    /// <summary>
    /// Contains all the server-side logic for ChemPrentices.
    /// <seealso cref="ChemPrenticeComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemPrenticeSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
        [Dependency] private readonly ItemSlotsSystem _slots = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemPrenticeComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemPrenticeComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemPrenticeComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemPrenticeComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemPrenticeComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemPrenticeComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<ChemPrenticeComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
        }

        private void SubscribeUpdateUiState<T>(Entity<ChemPrenticeComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void UpdateUiState(Entity<ChemPrenticeComponent> ent)
        {
            var (owner, chemPrentice) = ent;
            if (!_solution.TryGetSolution(owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
            {
                return;
            }
            var inputContainer = _slots.GetItemOrNull(owner, SharedChemMaster.InputSlotName);
            _appearance.SetData(owner, ChemMasterVisualState.BeakerInserted, inputContainer.HasValue);

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;
            var bufferMaxVolume = bufferSolution.MaxVolume;

            var state = new ChemPrenticeBoundUserInterfaceState(
                chemPrentice.Mode, BuildInputContainerInfo(inputContainer),
                bufferReagents, bufferCurrentVolume, bufferMaxVolume);

            _ui.SetUiState(owner, ChemPrenticeUiKey.Key, state);
        }

        private void OnSetModeMessage(Entity<ChemPrenticeComponent> ent, ref ChemMasterSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(ChemMasterMode), message.ChemMasterMode))
                return;

            ent.Comp.Mode = message.ChemMasterMode;
            UpdateUiState(ent);
            ClickSound(ent);
        }

        private void OnReagentButtonMessage(Entity<ChemPrenticeComponent> ent, ref ChemMasterReagentAmountButtonMessage message)
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

        private void TransferReagents(Entity<ChemPrenticeComponent> ent, ReagentId id, FixedPoint2 amount, bool fromBuffer)
        {
            var container = _slots.GetItemOrNull(ent, SharedChemMaster.InputSlotName);
            if (container is null ||
                !_solution.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
                !_solution.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out var bufferSolutionComponent, out var bufferSolution))
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
                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id), bufferSolution.AvailableVolume);
                _solution.RemoveReagent(containerSoln.Value, id, amount);
                _solution.TryAddReagent(bufferSolutionComponent.Value, id, amount, out var _);
                //bufferSolution.AddReagent(id, amount);
            }

            UpdateUiState(ent);
        }

        private void DiscardReagents(Entity<ChemPrenticeComponent> ent, ReagentId id, FixedPoint2 amount, bool fromBuffer)
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

            UpdateUiState(ent);
        }

        private void ClickSound(Entity<ChemPrenticeComponent> ent)
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

        private static ContainerInfo BuildContainerInfo(string name, Solution solution)
        {
            return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
            {
                Reagents = solution.Contents
            };
        }
    }
}
