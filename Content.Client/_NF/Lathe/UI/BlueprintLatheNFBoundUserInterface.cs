using Content.Shared._NF.Lathe;
using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._NF.Lathe.UI;

// Suffixed with NF to avoid BUI collisions with LatheBUI
[UsedImplicitly]
public sealed class BlueprintLatheNFBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BlueprintLatheNFMenu? _menu;
    public BlueprintLatheNFBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindowCenteredRight<BlueprintLatheNFMenu>();
        _menu.SetEntity(Owner);

        _menu.OnServerListButtonPressed += _ =>
        {
            SendMessage(new ConsoleServerSelectionMessage());
        };

        _menu.RecipeQueueAction += (blueprintType, recipes, amount) =>
        {
            SendMessage(new BlueprintLatheQueueRecipeMessage(blueprintType, recipes, amount));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case BlueprintLatheUpdateState msg:
                if (_menu != null)
                    _menu.RecipesByBlueprintType = msg.RecipeBitsetByBlueprintType;
                _menu?.PopulateRecipes();
                _menu?.UpdateCategories();
                _menu?.PopulateQueueList(msg.Queue);
                _menu?.SetQueueInfo(msg.CurrentlyProducing);
                break;
        }
    }
}
