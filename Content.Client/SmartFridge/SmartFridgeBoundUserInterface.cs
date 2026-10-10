using Content.Client.UserInterface.Controls;
using Content.Shared.SmartFridge;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.SmartFridge;

public sealed class SmartFridgeBoundUserInterface : BoundUserInterface
{
    private SmartFridgeMenu? _menu;

    public SmartFridgeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SmartFridgeMenu>();
        _menu.OnItemSelected += OnItemSelected;

        //Frontier: menu title from yml
        //Since we have now fridge variants, can't hardcode the title in xaml. Declare title field in yml instead
        if (EntMan.TryGetComponent<SmartFridgeComponent>(Owner, out var comp))
        {
            _menu.Title = Loc.GetString(comp.Title);
        }
        //end frontier

        Refresh();
    }

    public void Refresh()
    {
        if (_menu is not {} menu || !EntMan.TryGetComponent(Owner, out SmartFridgeComponent? fridge))
            return;

        menu.SetFlavorText(Loc.GetString(fridge.FlavorText));
        menu.Populate((Owner, fridge));
    }

    private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (data is not SmartFridgeListData entry)
            return;
        SendPredictedMessage(new SmartFridgeDispenseItemMessage(entry.Entry));
    }
}
