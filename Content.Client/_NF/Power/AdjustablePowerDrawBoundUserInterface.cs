using Content.Shared._NF.Power;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Power;

/// <summary>
/// BUI for <see cref="AdjustablePowerDrawUiKey.Key"/>.
/// Controls a machine with adjustable power draw.
/// </summary>
/// <seealso cref="BoundUserInterfaceState"/>
/// <seealso cref="AdjustablePowerDrawWindow"/>
public sealed class AdjustablePowerDrawBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private AdjustablePowerDrawMenu? _window;

    public AdjustablePowerDrawBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AdjustablePowerDrawMenu>();
        _window.SetEntity(Owner);

        _window.OnSetLoad += OnSetLoadButtonPressed;
        _window.OnSetPowered += OnSetPoweredButtonPressed;
        Update();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not AdjustablePowerDrawBuiState powerState)
            return;

        if (_window == null)
            return;

        _window.Update(powerState);
    }

    private void OnSetLoadButtonPressed(float value)
    {
        SendPredictedMessage(new AdjustablePowerDrawSetLoadMessage(value));
    }

    private void OnSetPoweredButtonPressed(bool on)
    {
        SendPredictedMessage(new AdjustablePowerDrawSetEnabledMessage(on));
    }
}
