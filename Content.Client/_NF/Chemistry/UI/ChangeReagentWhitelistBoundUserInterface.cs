using Content.Shared._NF.Chemistry.Events;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Chemistry.UI;

[UsedImplicitly]
public sealed class ChangeReagentWhitelistBoundUserInterface : BoundUserInterface
{
    private ChangeReagentWhitelistWindow? _window;
    public ChangeReagentWhitelistBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        if (_window == null)
        {
            _window = this.CreateWindow<ChangeReagentWhitelistWindow>();
            _window.SetEntity(Owner);
            _window.OnChangeWhitelistedReagent += ChangeReagentWhitelist;
            _window.OnResetWhitelistReagent += ResetReagentWhitelist;
        }
    }

    public void ChangeReagentWhitelist(ProtoId<ReagentPrototype> newReagentProto)
    {
        SendMessage(new ReagentWhitelistChangeMessage(newReagentProto));
        Close();
    }

    public void ResetReagentWhitelist()
    {
        SendMessage(new ReagentWhitelistResetMessage());
        Close();
    }
}
