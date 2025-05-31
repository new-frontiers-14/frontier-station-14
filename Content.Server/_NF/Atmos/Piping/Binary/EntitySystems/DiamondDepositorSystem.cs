using Content.Server._NF.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Construction;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Stack;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._NF.Atmos.Piping.Binary.EntitySystems;

[UsedImplicitly]
public sealed class DiamondDepositorSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DiamondDepositorComponent, AtmosDeviceEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<DiamondDepositorComponent, AtmosDeviceUpdateEvent>(OnUpdate);
        SubscribeLocalEvent<DiamondDepositorComponent, AtmosDeviceDisabledEvent>(OnDisabled);
        SubscribeLocalEvent<DiamondDepositorComponent, EntInsertedIntoContainerMessage>(OnSeedInserted);
        SubscribeLocalEvent<DiamondDepositorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnEnabled(EntityUid uid, DiamondDepositorComponent comp, ref AtmosDeviceEnabledEvent args)
    {
        UpdateAppearance(uid, comp);
    }

    private void OnExamined(Entity<DiamondDepositorComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;
        if (!EntityManager.GetComponent<TransformComponent>(ent).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
            return;

        if (!_nodeContainer.TryGetNode(ent.Owner, comp.InletName, out PipeNode? inlet))
            return;

        using (args.PushGroup(nameof(DiamondDepositorComponent)))
        {
            if (comp.Reacting)
            {
                args.PushMarkup(Loc.GetString("diamond-depositor-reacting"));
            }
            else if (!comp.ConsumedSeed)
            {
                args.PushMarkup(Loc.GetString("diamond-depositor-no-seed"));
            }
            else
            {
                if (inlet.Air.Temperature < comp.TargetTemp - comp.MaxTempError)
                {
                    args.PushMarkup(Loc.GetString("diamond-depositor-low-pressure"));
                }

                if (inlet.Air.Temperature > comp.TargetTemp + comp.MaxTempError)
                {
                    args.PushMarkup(Loc.GetString("diamond-depositor-low-temperature"));
                }
            }
        }
    }

    private void OnUpdate(Entity<DiamondDepositorComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        var comp = ent.Comp;

        if (!comp.ConsumedSeed
            || !_nodeContainer.TryGetNodes(ent.Owner, comp.InletName, comp.OutletName, out PipeNode? inlet, out PipeNode? outlet))
        {
            _ambientSoundSystem.SetAmbience(ent, false);
            return;
        }

        // The gas recycler is a passive device, so it permits gas flow even if nothing is being reacted.
        var error = Math.Abs(inlet.Air.Temperature - comp.TargetTemp);
        comp.Reacting = error < comp.MaxTempError;
        var removed = inlet.Air.RemoveVolume(PassiveTransferVol(inlet.Air, outlet.Air));
        if (comp.Reacting)
        {
            var nCO2 = removed.GetMoles(Gas.CarbonDioxide);
            nCO2 *= comp.ConversionFactor;
            removed.AdjustMoles(Gas.CarbonDioxide, -nCO2);
            removed.AdjustMoles(Gas.Oxygen, nCO2);
            comp.AccumulatedMoles += nCO2;
            comp.AccumulatedError += nCO2 * error;

            // Check if we need to produce an output item
            if (comp.AccumulatedMoles >= comp.NeededMoles)
            {
                // TODO: adjust quality of item based on accumulated error.
                for (int i = 0; i < comp.SpawnQuantity; i++)
                {
                    SpawnNextToOrDrop(comp.SpawnName, ent);
                }

                comp.AccumulatedMoles = 0;
                comp.AccumulatedError = 0;

                // Consume input from the slot
                if (ent.Comp.ConsumedSeedItems < ent.Comp.SeedItemsUsedPerRun
                    && _container.TryGetContainer(ent, ent.Comp.SeedSlotName, out var seedContainer))
                {
                    foreach (var containedEntity in new List<EntityUid>(seedContainer.ContainedEntities))
                    {
                        if (TryComp(containedEntity, out StackComponent? stack))
                        {
                            var numToRemove = int.Min(stack.Count, ent.Comp.SeedItemsUsedPerRun - ent.Comp.ConsumedSeedItems);
                            _stack.Use(containedEntity, numToRemove);
                            ent.Comp.ConsumedSeedItems += numToRemove;
                        }
                        else
                        {
                            QueueDel(containedEntity);
                            ent.Comp.ConsumedSeedItems += 1;
                        }
                        if (ent.Comp.ConsumedSeedItems >= ent.Comp.SeedItemsUsedPerRun)
                            break;
                    }
                }

                // Start running if we have consumed enough items.
                ent.Comp.ConsumedSeed = ent.Comp.ConsumedSeedItems >= ent.Comp.SeedItemsUsedPerRun;
                if (ent.Comp.ConsumedSeed)
                    ent.Comp.ConsumedSeedItems -= ent.Comp.SeedItemsUsedPerRun;
            }
        }

        _atmosphereSystem.Merge(outlet.Air, removed);
        UpdateAppearance(ent, comp);
        _ambientSoundSystem.SetAmbience(ent, true);
    }

    // TODO: figure out how this thing really works - is this just a pump
    public float PassiveTransferVol(GasMixture inlet, GasMixture outlet)
    {
        if (inlet.Pressure < outlet.Pressure)
        {
            return 0;
        }
        float overPressConst = 300; // pressure difference (in atm) to get 200 L/sec transfer rate
        float alpha = Atmospherics.MaxTransferRate * _atmosphereSystem.PumpSpeedup() / (float)Math.Sqrt(overPressConst * Atmospherics.OneAtmosphere);
        return alpha * (float)Math.Sqrt(inlet.Pressure - outlet.Pressure);
    }

    private void OnDisabled(EntityUid uid, DiamondDepositorComponent comp, ref AtmosDeviceDisabledEvent args)
    {
        comp.Reacting = false;
        UpdateAppearance(uid, comp);
    }

    private void UpdateAppearance(EntityUid uid, DiamondDepositorComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        _appearance.SetData(uid, PumpVisuals.Enabled, comp.Reacting);
    }

    private void OnSeedInserted(Entity<DiamondDepositorComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SeedSlotName)
            return;

        if (!ent.Comp.ConsumedSeed)
        {
            // Consume items if needed.
            if (ent.Comp.ConsumedSeedItems < ent.Comp.SeedItemsUsedPerRun)
            {
                if (TryComp(args.Entity, out StackComponent? stack))
                {
                    var numToRemove = int.Min(stack.Count, ent.Comp.SeedItemsUsedPerRun - ent.Comp.ConsumedSeedItems);
                    _stack.Use(args.Entity, numToRemove);
                    ent.Comp.ConsumedSeedItems += numToRemove;
                }
                else
                {
                    QueueDel(args.Entity);
                    ent.Comp.ConsumedSeedItems += 1;
                }
            }

            // Start running if we have consumed enough items.
            ent.Comp.ConsumedSeed = ent.Comp.ConsumedSeedItems >= ent.Comp.SeedItemsUsedPerRun;
            if (ent.Comp.ConsumedSeed)
                ent.Comp.ConsumedSeedItems -= ent.Comp.SeedItemsUsedPerRun;
        }
    }
}
