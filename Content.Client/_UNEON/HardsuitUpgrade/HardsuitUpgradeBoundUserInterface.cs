using Content.Shared._UNEON.HardsuitUpgrade;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._UNEON.HardsuitUpgrade;

public sealed class HardsuitUpgradeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private HardsuitUpgradeWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<HardsuitUpgradeWindow>();
        _window.Upgrade += kind => SendMessage(new HardsuitUpgradeStartMessage(kind));
        _window.Eject += () => SendMessage(new HardsuitUpgradeEjectMessage());
        _window.EjectMaterials += () => SendMessage(new HardsuitUpgradeMaterialsMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is HardsuitUpgradeState upgrade)
            _window?.UpdateState(upgrade);
    }
}
