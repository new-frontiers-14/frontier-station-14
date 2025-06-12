using Content.Server._NF.Bank;
using Content.Server._NF.Power.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Shared._NF.Bank;
using Content.Shared._NF.Bank.BUI;
using Content.Shared.Examine;
using Content.Shared.NodeContainer;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Power.EntitySystems;

public sealed partial class PowerTransmissionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly NodeContainerSystem _node = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<PowerTransmissionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PowerTransmissionComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PowerTransmissionComponent, AfterActivatableUIOpenEvent>(OnUIOpen);

        Subs.BuiEvents<PowerTransmissionComponent>(
            AdjustablePowerDrawUiKey.Key,
            subs =>
            {
                subs.Event<AdjustablePowerDrawSetEnabledMessage>(HandleSetEnabled);
                subs.Event<AdjustablePowerDrawSetLoadMessage>(HandleSetLoad);
            });
    }

    private void OnMapInit(Entity<PowerTransmissionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextDeposit = _timing.CurTime + ent.Comp.DepositPeriod;
    }

    private void OnExamined(Entity<PowerTransmissionComponent> ent, ref ExaminedEvent args)
    {
        if (TryComp(ent, out PowerConsumerComponent? power))
            args.PushMarkup(Loc.GetString("power-transmission-examine", ("value", power.DrawRate)));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PowerTransmissionComponent, PowerConsumerComponent>();
        while (query.MoveNext(out _, out var xmit, out var power))
        {
            if (power.NetworkLoad.Enabled)
                xmit.AccumulatedEnergy += power.NetworkLoad.ReceivingPower * frameTime;

            if (_timing.CurTime >= xmit.NextDeposit)
            {
                xmit.NextDeposit += xmit.DepositPeriod;

                if (!float.IsFinite(xmit.AccumulatedEnergy) || !float.IsPositive(xmit.AccumulatedEnergy))
                {
                    xmit.AccumulatedEnergy = 0.0f;
                    return;
                }

                float depositValue;
                if (xmit.AccumulatedEnergy <= xmit.LinearMaxValue * xmit.DepositPeriod.TotalSeconds)
                {
                    depositValue = xmit.AccumulatedEnergy * xmit.LinearRate;
                }
                else
                {
                    var depositPeriodSeconds = (float)xmit.DepositPeriod.TotalSeconds;
                    depositValue = depositPeriodSeconds * xmit.LogarithmCoefficient * MathF.Pow(xmit.LogarithmRateBase, MathF.Log10(xmit.AccumulatedEnergy / depositPeriodSeconds) - xmit.LogarithmSubtrahend);
                }

                xmit.AccumulatedEnergy = 0.0f;
                var depositSpesos = (int)depositValue;
                if (depositSpesos > 0)
                    _bank.TrySectorDeposit(xmit.Account, depositSpesos, LedgerEntryType.PowerTransmission);
            }
        }
    }

    /// <summary>
    /// Gets the expected pay rate, in spesos per second.
    /// </summary>
    /// <param name="power">Input power level, in watts</param>
    /// <returns>Expected power sale value in spesos per second</returns>
    public float GetPowerRate(Entity<PowerTransmissionComponent> ent, float power)
    {
        if (!float.IsFinite(power) || !float.IsPositive(power))
        {
            return 0f;
        }

        var depositPeriodSeconds = ent.Comp.DepositPeriod.TotalSeconds;
        if (power <= ent.Comp.LinearMaxValue / depositPeriodSeconds)
        {
            return ent.Comp.LinearRate * power;
        }
        else
        {
            return ent.Comp.LogarithmCoefficient * MathF.Pow(ent.Comp.LogarithmRateBase, MathF.Log10(power) - ent.Comp.LogarithmSubtrahend);
        }
    }

    private void OnUIOpen(Entity<PowerTransmissionComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        if (TryComp(ent, out PowerConsumerComponent? power))
            UpdateUI(ent, power);
    }

    private void HandleSetEnabled(Entity<PowerTransmissionComponent> ent, ref AdjustablePowerDrawSetEnabledMessage args)
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

    private void HandleSetLoad(Entity<PowerTransmissionComponent> ent, ref AdjustablePowerDrawSetLoadMessage args)
    {
        if (args.Load >= 0 && TryComp(ent, out PowerConsumerComponent? power))
        {
            power.DrawRate = args.Load;
            UpdateUI(ent, power);
        }
    }

    private void UpdateUI(Entity<PowerTransmissionComponent> ent, PowerConsumerComponent power)
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
                Text = Loc.GetString("power-transmission-estimated-value", ("value", BankSystemExtensions.ToSpesoString((int)GetPowerRate(ent, power.DrawRate))))
            });
    }
}
