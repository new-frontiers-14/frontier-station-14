using Content.Client.Shipyard.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Shipyard;
using Content.Shared.Shipyard.BUI;
using Content.Shared.Shipyard.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Shipyard.BUI;

public sealed class ShipyardConsoleBoundUserInterface : BoundUserInterface
{
    private ShipyardConsoleMenu? _menu;
    private ShipyardRulesPopup? _rulesWindow;
    public int Balance { get; private set; }

    public int? ShipSellValue { get; private set; }

    public ShipyardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = new ShipyardConsoleMenu(this);
        // Disable the NFSD popup for now.
        // var rules = new FormattedMessage();
        // _rulesWindow = new ShipyardRulesPopup(this);
        _menu.OpenCentered();
        // if (ShipyardConsoleUiKey.Security == (ShipyardConsoleUiKey) UiKey)
        // {
        //     rules.AddText(Loc.GetString($"shipyard-rules-default1"));
        //     rules.PushNewline();
        //     rules.AddText(Loc.GetString($"shipyard-rules-default2"));
        //     _rulesWindow.ShipRules.SetMessage(rules);
        //     _rulesWindow.OpenCentered();
        // }
        _menu.OnClose += Close;
        _menu.OnOrderApproved += ApproveOrder;
        _menu.OnSellShip += SellShip;
        _menu.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent("ShipyardConsole-targetId"));
    }

    private void Populate(List<string> availablePrototypes, List<string> unavailablePrototypes, bool freeListings, bool validId)
    {
        if (_menu == null)
            return;

        _menu.PopulateProducts(availablePrototypes, unavailablePrototypes, freeListings, validId);
        _menu.PopulateCategories(availablePrototypes, unavailablePrototypes);
        _menu.PopulateClasses(availablePrototypes, unavailablePrototypes);
        _menu.PopulateEngines(availablePrototypes, unavailablePrototypes);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ShipyardConsoleInterfaceState cState)
            return;

        Balance = cState.Balance;
        ShipSellValue = cState.ShipSellValue;
        var castState = (ShipyardConsoleInterfaceState) state;
        Populate(castState.ShipyardPrototypes.available, castState.ShipyardPrototypes.unavailable, castState.FreeListings, castState.IsTargetIdPresent);
        _menu?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing) return;

        _menu?.Dispose();
    }

    private void ApproveOrder(ButtonEventArgs args)
    {
        if (args.Button.Parent?.Parent is not VesselRow row || row.Vessel == null)
        {
            return;
        }

        var vesselId = row.Vessel.ID;
        SendMessage(new ShipyardConsolePurchaseMessage(vesselId));
    }
    private void SellShip(ButtonEventArgs args)
    {
        //reserved for a sanity check, but im not sure what since we check all the important stuffs on server already
        SendMessage(new ShipyardConsoleSellMessage());
    }
}
