using Content.Shared.Implants.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Implants.UI;

public sealed class DistressImplantBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DistressImplantWindow? _window;

    public DistressImplantBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DistressImplantWindow>();
        _window.OnModeChanged += mode => SendMessage(new DistressImplantSetModeMessage(mode));
        _window.OnMessageSubmitted += message => SendMessage(new DistressImplantSetMessageMessage(message));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not DistressImplantBuiState cast || _window == null)
            return;

        _window.UpdateState(cast.Message, cast.Mode);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
    }
}
