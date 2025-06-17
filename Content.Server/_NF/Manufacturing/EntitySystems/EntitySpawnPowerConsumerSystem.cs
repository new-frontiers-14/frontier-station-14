using Content.Server._NF.Manufacturing.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Shared._NF.Power;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.NodeContainer;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Manufacturing.EntitySystems;

/// <summary>
/// Consumes large quantities of power, scales excessive overage down to reasonable values.
/// Spawns entities when thresholds reached.
/// </summary>
public sealed partial class EntitySpawnPowerConsumerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
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
        SubscribeLocalEvent<EntitySpawnPowerConsumerComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);

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
    }

    private void OnExamined(Entity<EntitySpawnPowerConsumerComponent> ent, ref ExaminedEvent args)
    {
        if (TryComp(ent, out PowerConsumerComponent? power))
        {
            args.PushMarkup(Loc.GetString("entity-spawn-power-consumer-examine", ("value", power.DrawRate)));

            if (power.NetworkLoad.Enabled && power.NetworkLoad.ReceivingPower > 0)
                args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-powered"));
            else
                args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-unpowered"));
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<EntitySpawnPowerConsumerComponent, PowerConsumerComponent>();
        while (query.MoveNext(out var uid, out var xmit, out var power))
        {
            if (power.NetworkLoad.Enabled)
                xmit.AccumulatedSpawnCheckEnergy += power.NetworkLoad.ReceivingPower * frameTime;

            if (_timing.CurTime >= xmit.NextSpawnCheck)
            {
                xmit.NextSpawnCheck += xmit.SpawnCheckPeriod;

                // Ensure accumulated energy is never infinite.
                if (!float.IsFinite(xmit.AccumulatedEnergy) || !float.IsPositive(xmit.AccumulatedEnergy))
                    xmit.AccumulatedEnergy = 0;

                // Adjust spawn check energy
                if (float.IsFinite(xmit.AccumulatedSpawnCheckEnergy) && float.IsPositive(xmit.AccumulatedSpawnCheckEnergy))
                {
                    if (xmit.AccumulatedSpawnCheckEnergy <= xmit.LinearMaxValue * xmit.SpawnCheckPeriod.TotalSeconds)
                    {
                        xmit.AccumulatedEnergy += xmit.AccumulatedSpawnCheckEnergy;
                    }
                    else
                    {
                        var spawnCheckPeriodSeconds = (float)xmit.SpawnCheckPeriod.TotalSeconds;
                        xmit.AccumulatedEnergy += spawnCheckPeriodSeconds * xmit.LogarithmCoefficient * MathF.Pow(xmit.LogarithmRateBase, MathF.Log10(xmit.AccumulatedEnergy / spawnCheckPeriodSeconds) - xmit.LogarithmSubtrahend);
                    }
                }
                xmit.AccumulatedSpawnCheckEnergy = 0.0f;

                if (xmit.AccumulatedEnergy >= xmit.EnergyPerSpawn)
                {
                    xmit.AccumulatedEnergy -= xmit.EnergyPerSpawn;
                    TrySpawnInContainer(xmit.Spawn, uid, xmit.SlotName, out _);
                }
            }

            _appearance.SetData(uid, PowerDeviceVisuals.Powered, power.NetworkLoad.Enabled && power.NetworkLoad.ReceivingPower > 0);
        }
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

        float actualPower;
        if (power < ent.Comp.LinearMaxValue)
            actualPower = power;
        else
            actualPower = ent.Comp.LogarithmCoefficient * MathF.Pow(ent.Comp.LogarithmRateBase, MathF.Log10(power) - ent.Comp.LogarithmSubtrahend);

        return TimeSpan.FromSeconds(ent.Comp.EnergyPerSpawn / actualPower);
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
            power.DrawRate = args.Load;
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

    // Prevent insertion from any users - should only be handled by the system.
    private void OnItemSlotInsertAttempt(Entity<EntitySpawnPowerConsumerComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.User != null)
            args.Cancelled = true;
    }
}
