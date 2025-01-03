using Content.Server.Chemistry.Components;
using Content.Server.Labels;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared._NF.Chemistry;
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

namespace Content.Server._NF.Chemistry.EntitySystems
{

    /// <summary>
    /// Contains all the server-side logic for ChemPrentices.
    /// <seealso cref="ChemPrenticeComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemPrenticeSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

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

        private void UpdateUiState(Entity<ChemPrenticeComponent> ent, bool updateLabel = false)
        {
            var (owner, ChemPrentice) = ent;
            if (!_solutionContainerSystem.TryGetSolution(owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;
            var inputContainer = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.InputSlotName);
            var outputContainer = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.OutputSlotName);

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;
            var bufferMaxVolume = bufferSolution.MaxVolume;

            var state = new ChemPrenticeBoundUserInterfaceState(
                ChemPrentice.Mode, BuildInputContainerInfo(inputContainer),
                bufferReagents, bufferCurrentVolume, bufferMaxVolume);

            _userInterfaceSystem.SetUiState(owner, ChemPrenticeUiKey.Key, state);
        }

        private void OnSetModeMessage(Entity<ChemPrenticeComponent> ChemPrentice, ref ChemMasterSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(ChemMasterMode), message.ChemMasterMode))
                return;

            ChemPrentice.Comp.Mode = message.ChemMasterMode;
            UpdateUiState(ChemPrentice);
            ClickSound(ChemPrentice);
        }

        private void OnReagentButtonMessage(Entity<ChemPrenticeComponent> ChemPrentice, ref ChemMasterReagentAmountButtonMessage message)
        {
            // Ensure the amount corresponds to one of the reagent amount buttons.
            if (!Enum.IsDefined(typeof(ChemMasterReagentAmount), message.Amount))
                return;

            switch (ChemPrentice.Comp.Mode)
            {
                case ChemMasterMode.Transfer:
                    TransferReagents(ChemPrentice, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                    break;
                case ChemMasterMode.Discard:
                    DiscardReagents(ChemPrentice, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                    break;
                default:
                    // Invalid mode.
                    return;
            }

            ClickSound(ChemPrentice);
        }

        private void TransferReagents(Entity<ChemPrenticeComponent> ChemPrentice, ReagentId id, FixedPoint2 amount, bool fromBuffer)
        {
            var container = _itemSlotsSystem.GetItemOrNull(ChemPrentice, SharedChemMaster.InputSlotName);
            if (container is null ||
                !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
                !_solutionContainerSystem.TryGetSolution(ChemPrentice.Owner, SharedChemMaster.BufferSolutionName, out var bufferSolutionComponent, out var bufferSolution))
            {
                return;
            }

            if (fromBuffer) // Buffer to container
            {
                amount = FixedPoint2.Min(amount, containerSolution.AvailableVolume);
                amount = bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
                _solutionContainerSystem.TryAddReagent(containerSoln.Value, id, amount, out var _);
            }
            else // Container to buffer
            {
                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id), bufferSolution.AvailableVolume);
                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);
                _solutionContainerSystem.TryAddReagent(bufferSolutionComponent.Value, id, amount, out var _);
                //bufferSolution.AddReagent(id, amount);
            }

            UpdateUiState(ChemPrentice, updateLabel: true);
        }

        private void DiscardReagents(Entity<ChemPrenticeComponent> ChemPrentice, ReagentId id, FixedPoint2 amount, bool fromBuffer)
        {
            if (fromBuffer)
            {
                if (_solutionContainerSystem.TryGetSolution(ChemPrentice.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                    bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
                else
                    return;
            }
            else
            {
                var container = _itemSlotsSystem.GetItemOrNull(ChemPrentice, SharedChemMaster.InputSlotName);
                if (container is not null &&
                    _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution, out _))
                {
                    _solutionContainerSystem.RemoveReagent(containerSolution.Value, id, amount);
                }
                else
                    return;
            }

            UpdateUiState(ChemPrentice, updateLabel: fromBuffer);
        }

        private void ClickSound(Entity<ChemPrenticeComponent> ChemPrentice)
        {
            _audioSystem.PlayPvs(ChemPrentice.Comp.ClickSound, ChemPrentice, AudioParams.Default.WithVolume(-2f));
        }

        private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
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
