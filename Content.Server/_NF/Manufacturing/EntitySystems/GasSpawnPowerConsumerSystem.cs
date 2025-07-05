using Content.Server._NF.Manufacturing.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Shared._NF.Power;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Examine;
using Content.Shared.NodeContainer;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Manufacturing.EntitySystems;

/// <summary>
/// Consumes large quantities of power, scales excessive overage down to reasonable values.
/// Spawns gas regularly depending on the amount of power received.
/// </summary>
public sealed partial class GasSpawnPowerConsumerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly NodeContainerSystem _node = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private GasMixture _mixture = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<GasSpawnPowerConsumerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GasSpawnPowerConsumerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GasSpawnPowerConsumerComponent, AfterActivatableUIOpenEvent>(OnUIOpen);

        Subs.BuiEvents<GasSpawnPowerConsumerComponent>(
            AdjustablePowerDrawUiKey.Key,
            subs =>
            {
                subs.Event<AdjustablePowerDrawSetEnabledMessage>(HandleSetEnabled);
                subs.Event<AdjustablePowerDrawSetLoadMessage>(HandleSetLoad);
            });
    }

    private void OnMapInit(Entity<GasSpawnPowerConsumerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSpawnCheck = _timing.CurTime + ent.Comp.SpawnCheckPeriod;
        if (TryComp(ent, out PowerConsumerComponent? power))
            power.DrawRate = Math.Clamp(power.DrawRate, ent.Comp.MinimumRequestablePower, ent.Comp.MaximumRequestablePower);
    }

    private void OnExamined(Entity<GasSpawnPowerConsumerComponent> ent, ref ExaminedEvent args)
    {
        if (TryComp(ent, out PowerConsumerComponent? power))
        {
            args.PushMarkup(Loc.GetString("gas-spawn-power-consumer-examine", ("actual", power.ReceivedPower), ("requested", power.DrawRate)));

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

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GasSpawnPowerConsumerComponent, PowerConsumerComponent>();
        while (query.MoveNext(out var uid, out var spawn, out var power))
        {
            if (power.NetworkLoad.Enabled)
                spawn.AccumulatedSpawnCheckEnergy += power.NetworkLoad.ReceivingPower * frameTime;

            if (_timing.CurTime >= spawn.NextSpawnCheck)
            {
                spawn.NextSpawnCheck += spawn.SpawnCheckPeriod;

                if (!TryComp<GasCanisterComponent>(uid, out var canister))
                {
                    spawn.AccumulatedSpawnCheckEnergy = 0;
                    continue;
                }

                if (!float.IsFinite(spawn.AccumulatedSpawnCheckEnergy) || !float.IsPositive(spawn.AccumulatedSpawnCheckEnergy))
                {
                    spawn.AccumulatedSpawnCheckEnergy = 0;
                    continue;
                }

                // Ensure accumulated energy is never infinite.
                if (!float.IsFinite(spawn.AccumulatedEnergy) || !float.IsPositive(spawn.AccumulatedEnergy))
                    spawn.AccumulatedEnergy = 0;

                // Adjust spawn check energy
                if (float.IsFinite(spawn.AccumulatedSpawnCheckEnergy) && float.IsPositive(spawn.AccumulatedSpawnCheckEnergy))
                {
                    var totalPeriodSeconds = (float)spawn.SpawnCheckPeriod.TotalSeconds;
                    var effectivePower = GetEffectivePower((uid, spawn), spawn.AccumulatedSpawnCheckEnergy / totalPeriodSeconds);
                    spawn.AccumulatedEnergy += effectivePower * totalPeriodSeconds;
                }
                spawn.AccumulatedSpawnCheckEnergy = 0;

                // Require at least enough energy for one mole of gas before actually producing anything.
                if (spawn.AccumulatedEnergy >= spawn.EnergyPerMole)
                {
                    _mixture.CopyFrom(spawn.SpawnMixture);

                    // Figure out how many moles we can spawn with the energy we have.
                    var molesToSpawn = spawn.AccumulatedEnergy / spawn.EnergyPerMole;

                    molesToSpawn = MathF.Min(molesToSpawn, spawn.MaximumMolesPerSecond);

                    // Figure out how many moles will fit in the canister.
                    var deltaP = Atmospherics.MaxOutputPressure - canister.Air.Pressure;
                    var maxMoles = deltaP * canister.Air.Volume / (_mixture.Temperature * Atmospherics.R);
                    molesToSpawn = MathF.Min(molesToSpawn, maxMoles);

                    _mixture.Multiply(molesToSpawn / _mixture.TotalMoles);
                    _atmos.Merge(canister.Air, _mixture);

                    spawn.AccumulatedEnergy = 0;
                }
            }

            _appearance.SetData(uid, PowerDeviceVisuals.Powered, power.NetworkLoad.Enabled && power.NetworkLoad.ReceivingPower > 0);
        }
    }

    /// <summary>
    /// Gets the actual effective power in watts for some amount of input power.
    /// No range check on power.
    /// </summary>
    /// <param name="power">Input power level, in watts.</param>
    /// <returns>Effective power, in watts.</returns>
    private float GetEffectivePower(Entity<GasSpawnPowerConsumerComponent> ent, float power)
    {
        float actualPower;
        if (power <= ent.Comp.LinearMaxValue)
            actualPower = power;
        else
            actualPower = ent.Comp.LogarithmCoefficient * MathF.Pow(ent.Comp.LogarithmRateBase, MathF.Log10(power) - ent.Comp.LogarithmSubtrahend);
        return actualPower;
    }

    /// <summary>
    /// Gets the expected gas generation rate in moles per second.
    /// </summary>
    /// <param name="power">Input power level, in watts</param>
    /// <returns>Expected item generation time in seconds</returns>
    public float GetGasSpawnRate(Entity<GasSpawnPowerConsumerComponent> ent, float power)
    {
        if (!float.IsFinite(power) || !float.IsPositive(power))
        {
            return 0.0f;
        }

        var numMoles = GetEffectivePower(ent, power) / ent.Comp.EnergyPerMole;
        return MathF.Min(numMoles, ent.Comp.MaximumMolesPerSecond);
    }

    private void OnUIOpen(Entity<GasSpawnPowerConsumerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        if (TryComp(ent, out PowerConsumerComponent? power))
            UpdateUI(ent, power);
    }

    private void HandleSetEnabled(Entity<GasSpawnPowerConsumerComponent> ent, ref AdjustablePowerDrawSetEnabledMessage args)
    {
        if (TryComp(ent, out NodeContainerComponent? node) &&
            _node.TryGetNode<CableDeviceNode>(node, ent.Comp.PowerNodeName, out var deviceNode))
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

    private void HandleSetLoad(Entity<GasSpawnPowerConsumerComponent> ent, ref AdjustablePowerDrawSetLoadMessage args)
    {
        if (args.Load >= 0 && TryComp(ent, out PowerConsumerComponent? power))
        {
            power.DrawRate = Math.Clamp(args.Load, ent.Comp.MinimumRequestablePower, ent.Comp.MaximumRequestablePower);
            UpdateUI(ent, power);
        }
    }

    private void UpdateUI(Entity<GasSpawnPowerConsumerComponent> ent, PowerConsumerComponent power)
    {
        if (!_ui.IsUiOpen(ent.Owner, AdjustablePowerDrawUiKey.Key))
            return;

        bool nodeEnabled = false;
        if (TryComp(ent, out NodeContainerComponent? node) &&
            _node.TryGetNode<CableDeviceNode>(node, ent.Comp.PowerNodeName, out var deviceNode))
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
                Text = Loc.GetString("gas-spawn-power-consumer-value", ("value", GetGasSpawnRate(ent, power.DrawRate)))
            });
    }
}
