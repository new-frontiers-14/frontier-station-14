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
        [Dependency] private readonly LabelSystem _labelSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        [ValidatePrototypeId<EntityPrototype>]
        private const string PillPrototypeId = "Pill";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemPrenticeComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemPrenticeComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemPrenticeComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemPrenticeComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemPrenticeComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ChemPrenticeComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<ChemPrenticeComponent, ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
            SubscribeLocalEvent<ChemPrenticeComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
            SubscribeLocalEvent<ChemPrenticeComponent, ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
            SubscribeLocalEvent<ChemPrenticeComponent, ChemMasterOutputToBottleMessage>(OnOutputToBottleMessage);
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

            var state = new ChemPrenticeBoundUserInterfaceState(
                ChemPrentice.Mode, BuildInputContainerInfo(inputContainer), BuildOutputContainerInfo(outputContainer),
                bufferReagents, bufferCurrentVolume, ChemPrentice.PillType, ChemPrentice.PillDosageLimit, updateLabel);

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

        private void OnSetPillTypeMessage(Entity<ChemPrenticeComponent> ChemPrentice, ref ChemMasterSetPillTypeMessage message)
        {
            // Ensure valid pill type. There are 20 pills selectable, 0-19.
            if (message.PillType > SharedChemMaster.PillTypes - 1)
                return;

            ChemPrentice.Comp.PillType = message.PillType;
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
                !_solutionContainerSystem.TryGetSolution(ChemPrentice.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
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
                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id));
                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);
                bufferSolution.AddReagent(id, amount);
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

        private void OnCreatePillsMessage(Entity<ChemPrenticeComponent> ChemPrentice, ref ChemMasterCreatePillsMessage message)
        {
            var user = message.Actor;
            var maybeContainer = _itemSlotsSystem.GetItemOrNull(ChemPrentice, SharedChemMaster.OutputSlotName);
            if (maybeContainer is not { Valid: true } container
                || !TryComp(container, out StorageComponent? storage))
            {
                return; // output can't fit pills
            }

            // Ensure the number is valid.
            if (message.Number == 0 || !_storageSystem.HasSpace((container, storage)))
                return;

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > ChemPrentice.Comp.PillDosageLimit)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            var needed = message.Dosage * message.Number;
            if (!WithdrawFromBuffer(ChemPrentice, needed, user, out var withdrawal))
                return;

            _labelSystem.Label(container, message.Label);

            for (var i = 0; i < message.Number; i++)
            {
                var item = Spawn(PillPrototypeId, Transform(container).Coordinates);
                _storageSystem.Insert(container, item, out _, user: user, storage);
                _labelSystem.Label(item, message.Label);

                _solutionContainerSystem.EnsureSolutionEntity(item, SharedChemMaster.PillSolutionName, out var itemSolution, message.Dosage);
                if (!itemSolution.HasValue)
                    return;

                _solutionContainerSystem.TryAddSolution(itemSolution.Value, withdrawal.SplitSolution(message.Dosage));

                var pill = EnsureComp<PillComponent>(item);
                pill.PillType = ChemPrentice.Comp.PillType;
                Dirty(item, pill);

                // Log pill creation by a user
                _adminLogger.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");
            }

            UpdateUiState(ChemPrentice);
            ClickSound(ChemPrentice);
        }

        private void OnOutputToBottleMessage(Entity<ChemPrenticeComponent> ChemPrentice, ref ChemMasterOutputToBottleMessage message)
        {
            var user = message.Actor;
            var maybeContainer = _itemSlotsSystem.GetItemOrNull(ChemPrentice, SharedChemMaster.OutputSlotName);
            if (maybeContainer is not { Valid: true } container
                || !_solutionContainerSystem.TryGetSolution(container, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
            {
                return; // output can't fit reagents
            }

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > solution.AvailableVolume)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            if (!WithdrawFromBuffer(ChemPrentice, message.Dosage, user, out var withdrawal))
                return;

            _labelSystem.Label(container, message.Label);
            _solutionContainerSystem.TryAddSolution(soln.Value, withdrawal);

            // Log bottle creation by a user
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(user):user} bottled {ToPrettyString(container):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");

            UpdateUiState(ChemPrentice);
            ClickSound(ChemPrentice);
        }

        private bool WithdrawFromBuffer(
            Entity<ChemPrenticeComponent> ChemPrentice,
            FixedPoint2 neededVolume, EntityUid? user,
            [NotNullWhen(returnValue: true)] out Solution? outputSolution)
        {
            outputSolution = null;

            if (!_solutionContainerSystem.TryGetSolution(ChemPrentice.Owner, SharedChemMaster.BufferSolutionName, out _, out var solution))
            {
                return false;
            }

            if (solution.Volume == 0)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user.Value);
                return false;
            }

            // ReSharper disable once InvertIf
            if (neededVolume > solution.Volume)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user.Value);
                return false;
            }

            outputSolution = solution.SplitSolution(neededVolume);
            return true;
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

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            var name = Name(container.Value);
            {
                if (_solutionContainerSystem.TryGetSolution(
                        container.Value, SharedChemMaster.BottleSolutionName, out _, out var solution))
                {
                    return BuildContainerInfo(name, solution);
                }
            }

            if (!TryComp(container, out StorageComponent? storage))
                return null;

            var pills = storage.Container.ContainedEntities.Select(pill =>
            {
                _solutionContainerSystem.TryGetSolution(pill, SharedChemMaster.PillSolutionName, out _, out var solution);
                var quantity = solution?.Volume ?? FixedPoint2.Zero;
                return (Name(pill), quantity);
            }).ToList();

            return new ContainerInfo(name, _storageSystem.GetCumulativeItemAreas((container.Value, storage)), storage.Grid.GetArea())
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
