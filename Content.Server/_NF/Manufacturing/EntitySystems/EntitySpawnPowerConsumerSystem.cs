using Content.Server.Materials;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Shared._NF.Manufacturing;
using Content.Shared._NF.Manufacturing.Components;
using Content.Shared._NF.Manufacturing.EntitySystems;
using Content.Shared._NF.Power;
using Content.Shared.Examine;
using Content.Shared.Materials;
using Content.Shared.NodeContainer;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._NF.Manufacturing.EntitySystems;

/// <inheritdoc/>
public sealed partial class EntitySpawnPowerConsumerSystem : SharedEntitySpawnPowerConsumerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly NodeContainerSystem _node = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private EntityQuery<AppearanceComponent> _appearanceQuery;

    public override void Initialize()
    {
        base.Initialize();

        _appearanceQuery = GetEntityQuery<AppearanceComponent>();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<EntitySpawnPowerConsumerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EntitySpawnPowerConsumerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EntitySpawnPowerConsumerComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<EntitySpawnPowerConsumerComponent, MaterialEntityInsertedEvent>(OnMaterialInserted);

        Subs.BuiEvents<EntitySpawnPowerConsumerComponent>(
            AdjustablePowerDrawUiKey.Key,
            subs =>
            {
                subs.Event<AdjustablePowerDrawSetEnabledMessage>(HandleSetEnabled);
                subs.Event<AdjustablePowerDrawSetLoadMessage>(HandleSetLoad);
            });
    }

    private void OnMapInit(Entity<EntitySpawnPowerConsumerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSpawnCheck = _timing.CurTime + ent.Comp.SpawnCheckPeriod;
        if (TryComp(ent, out PowerConsumerComponent? power))
            power.DrawRate = Math.Clamp(power.DrawRate, ent.Comp.MinimumRequestablePower, ent.Comp.MaximumRequestablePower);
    }

    private void OnExamined(Entity<EntitySpawnPowerConsumerComponent> ent, ref ExaminedEvent args)
    {
        if (TryComp(ent, out PowerConsumerComponent? power))
        {
            args.PushMarkup(Loc.GetString("entity-spawn-power-consumer-examine", ("actual", power.ReceivedPower), ("requested", power.DrawRate)));

            var powered = power.NetworkLoad.Enabled && power.NetworkLoad.ReceivingPower > 0;
            args.PushMarkup(
                Loc.GetString("power-receiver-component-on-examine-main",
                    ("stateText", Loc.GetString(powered
                        ? "power-receiver-component-on-examine-powered"
                        : "power-receiver-component-on-examine-unpowered"))
                )
            );
        }
    }

    private void OnMaterialInserted(Entity<EntitySpawnPowerConsumerComponent> ent, ref MaterialEntityInsertedEvent args)
    {
        if (ent.Comp.Processing)
            return;

        TryConsumeResources(ent);
    }

    private void TryConsumeResources(Entity<EntitySpawnPowerConsumerComponent> ent)
    {
        if (ent.Comp.Material == null
            || ent.Comp.MaterialAmount <= 0
            || _materialStorage.TryChangeMaterialAmount(ent, ent.Comp.Material, -ent.Comp.MaterialAmount))
        {
            ent.Comp.Processing = true;
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<EntitySpawnPowerConsumerComponent, PowerConsumerComponent>();
        while (query.MoveNext(out var uid, out var spawn, out var power))
        {
            if (spawn.Processing && power.NetworkLoad.Enabled)
            {
                spawn.AccumulatedSpawnCheckEnergy += power.NetworkLoad.ReceivingPower * frameTime;
            }

            if (_timing.CurTime >= spawn.NextSpawnCheck)
            {
                spawn.NextSpawnCheck += spawn.SpawnCheckPeriod;

                // Ensure accumulated energy is never infinite.
                if (!float.IsFinite(spawn.AccumulatedEnergy) || !float.IsPositive(spawn.AccumulatedEnergy))
                    spawn.AccumulatedEnergy = 0;

                // Adjust spawn check energy
                if (float.IsFinite(spawn.AccumulatedSpawnCheckEnergy) && float.IsPositive(spawn.AccumulatedSpawnCheckEnergy))
                {
                    float totalPeriodSeconds = (float)spawn.SpawnCheckPeriod.TotalSeconds;
                    var effectivePower = GetEffectivePower((uid, spawn), spawn.AccumulatedSpawnCheckEnergy / totalPeriodSeconds);
                    spawn.AccumulatedEnergy += effectivePower * totalPeriodSeconds;
                }
                spawn.AccumulatedSpawnCheckEnergy = 0.0f;

                if (spawn.AccumulatedEnergy >= spawn.EnergyPerSpawn)
                {
                    // End current run.
                    spawn.AccumulatedEnergy = 0;
                    spawn.Processing = false;
                    TrySpawnInContainer(spawn.Spawn, uid, spawn.SlotName, out _);

                    // Try to start next run.
                    TryConsumeResources((uid, spawn));
                }
            }

            UpdateAppearance(uid, spawn, power);
        }
    }

    /// <summary>
    /// Gets the actual effective power in watts for some amount of input power.
    /// No range check on power.
    /// </summary>
    /// <param name="power">Input power level, in watts.</param>
    /// <returns>Effective power, in watts.</returns>
    private float GetEffectivePower(Entity<EntitySpawnPowerConsumerComponent> ent, float power)
    {
        float actualPower;
        if (power <= ent.Comp.LinearMaxValue)
            actualPower = power;
        else
            actualPower = ent.Comp.LogarithmCoefficient * MathF.Pow(ent.Comp.LogarithmRateBase, MathF.Log10(power) - ent.Comp.LogarithmSubtrahend);
        return MathF.Min(actualPower, ent.Comp.MaxEffectivePower);
    }

    /// <summary>
    /// Gets the expected generation time for an object in seconds.
    /// </summary>
    /// <param name="power">Input power level, in watts</param>
    /// <returns>Expected item generation time in seconds</returns>
    public TimeSpan GetGenerationTime(Entity<EntitySpawnPowerConsumerComponent> ent, float power)
    {
        if (!float.IsFinite(power) || !float.IsPositive(power))
        {
            return TimeSpan.Zero;
        }

        power = GetEffectivePower(ent, power);
        return TimeSpan.FromSeconds(ent.Comp.EnergyPerSpawn / power);
    }

    private void OnUIOpen(Entity<EntitySpawnPowerConsumerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        if (TryComp(ent, out PowerConsumerComponent? power))
            UpdateUI(ent, power);
    }

    private void HandleSetEnabled(Entity<EntitySpawnPowerConsumerComponent> ent, ref AdjustablePowerDrawSetEnabledMessage args)
    {
        if (TryComp(ent, out NodeContainerComponent? node) &&
            _node.TryGetNode<CableDeviceNode>(node, ent.Comp.NodeName, out var deviceNode))
        {
            deviceNode.Enabled = args.On;
            if (deviceNode.Enabled)
                _nodeGroup.QueueReflood(deviceNode);
            else
                _nodeGroup.QueueNodeRemove(deviceNode);

            if (TryComp(ent, out PowerConsumerComponent? power))
                UpdateUI(ent, power);
        }
    }

    private void HandleSetLoad(Entity<EntitySpawnPowerConsumerComponent> ent, ref AdjustablePowerDrawSetLoadMessage args)
    {
        if (args.Load >= 0 && TryComp(ent, out PowerConsumerComponent? power))
        {
            power.DrawRate = Math.Clamp(args.Load, ent.Comp.MinimumRequestablePower, ent.Comp.MaximumRequestablePower);
            UpdateUI(ent, power);
        }
    }

    private void UpdateUI(Entity<EntitySpawnPowerConsumerComponent> ent, PowerConsumerComponent power)
    {
        if (!_ui.IsUiOpen(ent.Owner, AdjustablePowerDrawUiKey.Key))
            return;

        bool nodeEnabled = false;
        if (TryComp(ent, out NodeContainerComponent? node) &&
            _node.TryGetNode<CableDeviceNode>(node, ent.Comp.NodeName, out var deviceNode))
        {
            nodeEnabled = deviceNode.Enabled;
        }

        _ui.SetUiState(
            ent.Owner,
            AdjustablePowerDrawUiKey.Key,
            new AdjustablePowerDrawBuiState
            {
                On = nodeEnabled,
                Load = power.DrawRate,
                Text = Loc.GetString("entity-spawn-power-consumer-estimated-time", ("time", GetGenerationTime(ent, power.DrawRate)))
            });
    }

    private void UpdateAppearance(EntityUid uid, EntitySpawnPowerConsumerComponent spawner, PowerConsumerComponent power)
    {
        if (_appearanceQuery.TryComp(uid, out var appearance))
        {
            _appearance.SetData(uid, PowerDeviceVisuals.Powered, power.NetworkLoad.Enabled && power.NetworkLoad.ReceivingPower > 0, appearance);
            _appearance.SetData(uid, EntitySpawnMaterialVisuals.SufficientMaterial, spawner.Processing, appearance);
        }
    }
}
