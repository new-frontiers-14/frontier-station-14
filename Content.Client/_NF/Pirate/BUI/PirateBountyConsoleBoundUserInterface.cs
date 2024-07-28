using Content.Client._NF.Pirate.UI;
using Content.Shared._NF.Pirate.Components;
using JetBrains.Annotations;

namespace Content.Client._NF.Pirate.BUI;

[UsedImplicitly]
public sealed class PirateBountyConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PirateBountyMenu? _menu;

    public PirateBountyConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new();

        _menu.OnClose += Close;

        _menu.OnLabelButtonPressed += id =>
        {
            SendMessage(new PirateBountyAcceptMessage(id));
        };

        _menu.OnSkipButtonPressed += id =>
        {
            SendMessage(new PirateBountySkipMessage(id));
        };

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState message)
    {
        base.UpdateState(message);

        if (message is not PirateBountyConsoleState state)
            return;

        _menu?.UpdateEntries(state.Bounties, state.UntilNextSkip);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
