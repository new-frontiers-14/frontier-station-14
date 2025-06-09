using Content.Client._NF.Power.Battery;
using Content.Client.UserInterface;
using Content.Shared._NF.Power;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.Timing;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Power;

/// <summary>
/// BUI for <see cref="BatteryUiKey.Key"/>.
/// </summary>
/// <seealso cref="BoundUserInterfaceState"/>
/// <seealso cref="BatteryMenu"/>
[UsedImplicitly]
public sealed class AdjustablePowerDrawBoundUserInterface : BoundUserInterface, IBuiPreTickUpdate
{
    [Dependency] private readonly IClientGameTiming _gameTiming = null!;

    [ViewVariables]
    private AdjustablePowerDrawMenu? _menu;

    private BuiPredictionState? _pred;
    private InputCoalescer<float> _chargeRateCoalescer;

    public AdjustablePowerDrawBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _pred = new BuiPredictionState(this, _gameTiming);

        _menu = this.CreateWindow<AdjustablePowerDrawMenu>();
        _menu.SetEntity(Owner);

        _menu.OnPowerSet += val => _pred!.SendMessage(new AdjustablePowerDrawSetLoad(val));
        _menu.On += val => _pred!.SendMessage(new BatterySetOutputBreakerMessage(val));

        _menu.OnChargeRate += val => _chargeRateCoalescer.Set(val);
        _menu.OnDischargeRate += val => _dischargeRateCoalescer.Set(val);
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        if (_chargeRateCoalescer.CheckIsModified(out var chargeRateValue))
            _pred!.SendMessage(new BatterySetChargeRateMessage(chargeRateValue));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not BatteryBuiState batteryState)
            return;

        foreach (var replayMsg in _pred!.MessagesToReplay())
        {
            switch (replayMsg)
            {
                case BatterySetInputBreakerMessage setInputBreaker:
                    batteryState.CanCharge = setInputBreaker.On;
                    break;
            }
        }

        _menu?.Update(batteryState);
    }
}
