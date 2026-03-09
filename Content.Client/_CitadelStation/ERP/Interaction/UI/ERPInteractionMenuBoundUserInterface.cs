using Content.Shared._CitadelStation.ERP.Interaction.UI.Messages;
using Robust.Client.UserInterface;
using Robust.Client.ResourceManagement;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._CitadelStation.ERP.Interaction.UI;

public sealed class ERPInteractionMenuBoundUserInterface : BoundUserInterface
{

    private readonly ERPInteractionMenu _window;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private ERPInteractionMenuInteractorOptions _currentActiveButton;

    Dictionary<ERPInteractionMenuInteractorOptions, Button> _interactorButtons = new Dictionary<ERPInteractionMenuInteractorOptions, Button>();

    public ERPInteractionMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        base.Open();
        _window = this.CreateWindow<ERPInteractionMenu>();

        _interactorButtons.Add(ERPInteractionMenuInteractorOptions.Groin, _window.GroinButton);
        _interactorButtons.Add(ERPInteractionMenuInteractorOptions.Hand, _window.HandButton);
        _interactorButtons.Add(ERPInteractionMenuInteractorOptions.Mouth, _window.MouthButton);

        foreach (var interactor_button in _interactorButtons) {
            interactor_button.Value.ToggleMode = true;
            interactor_button.Value.Pressed = false;
        }

        _window.ChangeViewButton.OnPressed += _ =>
        {
            _window.isFrontView = _window.isFrontView ? false : true;

            if (_window.isFrontView)
            {
                _window.Doll.Texture = _resourceCache.GetTexture("/Textures/_CitadelStation/Interface/Misc/doll.png");
            }
            else
            {
                _window.Doll.Texture = _resourceCache.GetTexture("/Textures/_CitadelStation/Interface/Misc/doll_back.png");
            }
            ;
        };

        _window.MouthButton.OnPressed += _ => UpdateCurrentInteractor(ERPInteractionMenuInteractorOptions.Mouth);
        _window.GroinButton.OnPressed += _ => UpdateCurrentInteractor(ERPInteractionMenuInteractorOptions.Groin);
        _window.HandButton.OnPressed += _ => UpdateCurrentInteractor(ERPInteractionMenuInteractorOptions.Hand);
    }

    private void UpdateCurrentInteractor(ERPInteractionMenuInteractorOptions _newInteractor) {
        _currentActiveButton = _newInteractor;
        foreach (var interactor_button in _interactorButtons) {
            if (interactor_button.Key != _newInteractor) {
                interactor_button.Value.Pressed = false;
            }
        }
    }
};
