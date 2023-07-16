using Content.Client._NF.StationBounties.UI;
using Robust.Client.GameObjects;

namespace Content.Client._NF.StationBounties.BUI;

public sealed class StationBountyBoundUserInterface : BoundUserInterface
{
    public StationBountyBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) { }
    private BountyContractMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = new BountyContractMenu();
        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _menu?.Dispose();
        }
    }
}
