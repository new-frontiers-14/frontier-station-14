using Content.Client._NF.Pirate.UI;
using Content.Shared._NF.Pirate.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

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

        if (_menu == null)
        {
            _menu = this.CreateWindow<PirateBountyMenu>();
            _menu.OnLabelButtonPressed += id =>
            {
                SendMessage(new PirateBountyAcceptMessage(id));
            };

            _menu.OnSkipButtonPressed += id =>
            {
                SendMessage(new PirateBountySkipMessage(id));
            };
        }
    }

    protected override void UpdateState(BoundUserInterfaceState message)
    {
        base.UpdateState(message);

        if (message is not PirateBountyConsoleState state)
            return;

        _menu?.UpdateEntries(state.Bounties, state.UntilNextSkip);
    }
}
