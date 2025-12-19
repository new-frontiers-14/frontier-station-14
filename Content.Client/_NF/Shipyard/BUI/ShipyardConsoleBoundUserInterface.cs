using Content.Client._NF.Shipyard.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared._NF.Shipyard.BUI;
using Content.Shared._NF.Shipyard.Events;
using Content.Shared._NF.Shipyard.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Shipyard.BUI;

public sealed class ShipyardConsoleBoundUserInterface : BoundUserInterface
{
    private ShipyardConsoleMenu? _menu;
    // private ShipyardRulesPopup? _rulesWindow; // Frontier
    public int Balance { get; private set; }

    public int? ShipSellValue { get; private set; }

    public ShipyardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        if (_menu == null)
        {
            _menu = this.CreateWindow<ShipyardConsoleMenu>();
            _menu.OnOrderApproved += ApproveOrder;
            _menu.OnSellShip += SellShip;
            _menu.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent("ShipyardConsole-targetId"));

            // Disable the NFSD popup for now.
            // var rules = new FormattedMessage();
            // _rulesWindow = new ShipyardRulesPopup(this);
            // if (ShipyardConsoleUiKey.Security == (ShipyardConsoleUiKey) UiKey)
            // {
            //     rules.AddText(Loc.GetString($"shipyard-rules-default1"));
            //     rules.PushNewline();
            //     rules.AddText(Loc.GetString($"shipyard-rules-default2"));
            //     _rulesWindow.ShipRules.SetMessage(rules);
            //     _rulesWindow.OpenCentered();
            // }
        }
    }

    private void Populate(List<string> availablePrototypes, List<string> unavailablePrototypes, bool freeListings, bool validId, List<ProtoId<ShuttleAtmospherePrototype>>? atmospheres)
    {
        if (_menu == null)
            return;

        _menu.PopulateProducts(availablePrototypes, unavailablePrototypes, freeListings, validId);
        _menu.PopulateCategories(availablePrototypes, unavailablePrototypes);
        _menu.PopulateClasses(availablePrototypes, unavailablePrototypes);
        _menu.PopulateEngines(availablePrototypes, unavailablePrototypes);
        _menu.PopulateAtmos(atmospheres);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ShipyardConsoleInterfaceState cState)
            return;

        Balance = cState.Balance;
        ShipSellValue = cState.ShipSellValue;
        Populate(
            cState.ShipyardPrototypes.available,
            cState.ShipyardPrototypes.unavailable,
            cState.FreeListings,
            cState.IsTargetIdPresent,
            cState.AtmosPrototypes
        );
        _menu?.UpdateState(cState);
    }

    private void ApproveOrder(ShipyardConsoleMenu.OrderApprovedEventArgs args)
    {
        SendMessage(new ShipyardConsolePurchaseMessage(args.Vessel, args.Atmosphere));
    }
    private void SellShip(ButtonEventArgs args)
    {
        //reserved for a sanity check, but im not sure what since we check all the important stuffs on server already
        SendMessage(new ShipyardConsoleSellMessage());
    }
}
