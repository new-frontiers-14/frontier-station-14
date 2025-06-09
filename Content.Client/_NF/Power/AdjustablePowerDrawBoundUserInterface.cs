using Content.Client._NF.Power;
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

        _menu.OnSetLoad += val => _pred!.SendMessage(new AdjustablePowerDrawSetLoadMessage(val));
        _menu.OnSetPowered += val => _pred!.SendMessage(new AdjustablePowerDrawSetEnabledMessage(val));
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        if (_chargeRateCoalescer.CheckIsModified(out var chargeRateValue))
            _pred!.SendMessage(new BatterySetChargeRateMessage(chargeRateValue));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not AdjustablePowerDrawBuiState powerState)
            return;

        foreach (var replayMsg in _pred!.MessagesToReplay())
        {
            switch (replayMsg)
            {
                case AdjustablePowerDrawSetLoadMessage setLoad:
                    powerState.Load = setLoad.Load;
                    break;
                case AdjustablePowerDrawSetEnabledMessage setEnabled:
                    powerState.On = setEnabled.On;
                    break;
            }
        }

        _menu?.Update(powerState);
    }
}
