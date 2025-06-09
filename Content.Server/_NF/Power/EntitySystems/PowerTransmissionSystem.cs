using Content.Server._NF.Bank;
using Content.Server._NF.Power.Components;
using Content.Server.Power.Components;
using Content.Shared._NF.Bank.BUI;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Power.EntitySystems;

public sealed partial class PowerTransmissionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private BankSystem _bank = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerTransmissionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PowerTransmissionComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<PowerTransmissionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextDeposit = _timing.CurTime + ent.Comp.DepositPeriod;
    }

    private void OnExamined(Entity<PowerTransmissionComponent> ent, ref ExaminedEvent args)
    {
        if (TryComp(ent, out ApcPowerReceiverComponent? power))
            args.PushMarkup(Loc.GetString("power-transmission-examine", ("value", power.NetworkLoad)));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQuery<PowerTransmissionComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var xmit, out var power))
        {
            if (!power.PowerDisabled)
                xmit.AccumulatedEnergy += power.PowerReceived * frameTime;

            if (_timing.CurTime >= xmit.NextDeposit)
            {
                xmit.NextDeposit += xmit.DepositPeriod;

                if (!float.IsFinite(xmit.AccumulatedEnergy) || !float.IsPositive(xmit.AccumulatedEnergy))
                {
                    xmit.AccumulatedEnergy = 0.0f;
                    return;
                }

                float depositValue;
                if (xmit.AccumulatedEnergy <= xmit.LinearMaxValue)
                {
                    depositValue = xmit.AccumulatedEnergy * xmit.LinearRate;
                }
                else
                {
                    var depositPeriodSeconds = xmit.DepositPeriod.Seconds;
                    depositValue = depositPeriodSeconds * xmit.LogarithmCoefficient * MathF.Pow(xmit.LogarithmRateBase, MathF.Log10(xmit.AccumulatedEnergy / depositPeriodSeconds));
                }

                xmit.AccumulatedEnergy = 0.0f;
                _bank.TrySectorDeposit(xmit.Account, (int)depositValue, LedgerEntryType.PowerTransmission);
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

        var depositPeriodSeconds = ent.Comp.DepositPeriod.Seconds;
        if (power <= ent.Comp.LinearMaxValue / depositPeriodSeconds)
        {
            return ent.Comp.LinearRate * power;
        }
        else
        {
            return ent.Comp.LogarithmCoefficient * MathF.Pow(ent.Comp.LogarithmRateBase, MathF.Log10(power));
        }
    }
}
