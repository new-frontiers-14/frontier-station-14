using Content.Shared._CitadelStation.ERP.Interaction.UI.Messages;
using Robust.Client.UserInterface;
using Robust.Client.ResourceManagement;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Graphics;

namespace Content.Client._CitadelStation.ERP.Interaction.UI;

public sealed class ERPInteractionMenuBoundUserInterface : BoundUserInterface
{

    private readonly ERPInteractionMenu _window;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private ERPInteractionMenuInteractorOptions _currentActiveButton;

    private ERPInteractionMenuInteractorModes _currentMode;

    Dictionary<ERPInteractionMenuInteractorOptions, Button> _interactorButtons = new Dictionary<ERPInteractionMenuInteractorOptions, Button>();

    Dictionary<ERPInteractionMenuInteractorModes, Button> _interactorModes = new Dictionary<ERPInteractionMenuInteractorModes, Button>();

    private Texture _dollFrontTexture;
    private Texture _dollBackTexture;
    private StyleBoxTexture _dollFront;

    private StyleBoxTexture _dollBack;

    public ERPInteractionMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        base.Open();
        _window = this.CreateWindow<ERPInteractionMenu>();

        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _dollFrontTexture = _resourceCache.GetTexture("/Textures/_CitadelStation/Interface/Misc/doll.png");

        _dollFront = new StyleBoxTexture {
            Texture = _dollFrontTexture,
            //Modulate = Color.FromHex("#ffffffff")
        };

        _dollBackTexture = _resourceCache.GetTexture("/Textures/_CitadelStation/Interface/Misc/doll_back.png");

        _dollBack = new StyleBoxTexture {
            Texture = _dollBackTexture,
            //Modulate = Color.FromHex("#ffffffff")
        };

        _interactorButtons.Add(ERPInteractionMenuInteractorOptions.Groin, _window.GroinButton);
        _interactorButtons.Add(ERPInteractionMenuInteractorOptions.Hand, _window.HandButton);
        _interactorButtons.Add(ERPInteractionMenuInteractorOptions.Mouth, _window.MouthButton);

        _interactorModes.Add(ERPInteractionMenuInteractorModes.Gentle, _window.GentleModeButton);
        _interactorModes.Add(ERPInteractionMenuInteractorModes.Rough, _window.RoughModeButton);
        _interactorModes.Add(ERPInteractionMenuInteractorModes.Violent, _window.ViolentModeButton);

        foreach (var interactor_button in _interactorButtons)
        {
            interactor_button.Value.ToggleMode = true;
            interactor_button.Value.Pressed = false;
        }

        foreach (var mode in _interactorModes)
        {
            mode.Value.ToggleMode = true;
            mode.Value.Pressed = false;
        }

        _window.ChangeViewButton.OnMouseEntered += _ =>
        {
            _window.ChangeViewButton.Modulate = Color.Gray;
        };

        _window.ChangeViewButton.OnMouseExited += _ =>
        {
            _window.ChangeViewButton.Modulate = Color.White;
        };

        _window.ChangeViewButton.OnPressed += _ =>
        {
            _window.isFrontView = _window.isFrontView ? false : true;

            if (_window.isFrontView)
            {
                //_window.ChangeViewButton.Modulate = Color.Green;
                _window.Doll.PanelOverride = _dollFront;
            }
            else
            {
                //_window.ChangeViewButton.Modulate = Color.Transparent;
                _window.Doll.PanelOverride = _dollBack;
            }
            ;
        };

        _window.MouthButton.OnPressed += _ => UpdateCurrentInteractor(ERPInteractionMenuInteractorOptions.Mouth);
        _window.GroinButton.OnPressed += _ => UpdateCurrentInteractor(ERPInteractionMenuInteractorOptions.Groin);
        _window.HandButton.OnPressed += _ => UpdateCurrentInteractor(ERPInteractionMenuInteractorOptions.Hand);

        _window.GentleModeButton.OnPressed += _ => UpdateCurrentMode(ERPInteractionMenuInteractorModes.Gentle);
        _window.RoughModeButton.OnPressed += _ => UpdateCurrentMode(ERPInteractionMenuInteractorModes.Rough);
        _window.ViolentModeButton.OnPressed += _ => UpdateCurrentMode(ERPInteractionMenuInteractorModes.Violent);



        _window.HeadButton.OnPressed += _ => SendInteractorMessage(ERPInteractionMenuBodyParts.Head);

        _window.LArmButton.OnPressed += _ => SendInteractorMessage(ERPInteractionMenuBodyParts.LArm);
        _window.ChestButton.OnPressed += _ => SendInteractorMessage(ERPInteractionMenuBodyParts.Chest);
        _window.RArmButton.OnPressed += _ => SendInteractorMessage(ERPInteractionMenuBodyParts.RArm);

        _window.LForearmButton.OnPressed += _ => SendInteractorMessage(ERPInteractionMenuBodyParts.LForearm);
        _window.AbdomenButton.OnPressed += _ => SendInteractorMessage(ERPInteractionMenuBodyParts.Abdomen);
        _window.RForearmButton.OnPressed += _ => SendInteractorMessage(ERPInteractionMenuBodyParts.RForearm);

        _window.LHandButton.OnPressed += _ => SendInteractorMessage(ERPInteractionMenuBodyParts.LHand);
        _window.GroinTargetButton.OnPressed += _ => SendInteractorMessage(ERPInteractionMenuBodyParts.Groin);
        _window.RHandButton.OnPressed += _ => SendInteractorMessage(ERPInteractionMenuBodyParts.RHand);
    }

    private void SendInteractorMessage(ERPInteractionMenuBodyParts interactor) {
        Logger.Debug($"Interactor = {_currentActiveButton} Mode = {_currentMode} Bodypart = {interactor}");
        SendMessage(new ERPActionUserInterfaceMessage() {
            Interactor = _currentActiveButton,
            Mode = _currentMode,
            Bodypart = interactor
        });
    }

    private void UpdateCurrentInteractor(ERPInteractionMenuInteractorOptions _newInteractor)
    {
        _currentActiveButton = _newInteractor;
        foreach (var interactor_button in _interactorButtons)
        {
            if (interactor_button.Key != _newInteractor)
            {
                interactor_button.Value.Pressed = false;
            }
        }
    }

    private void UpdateCurrentMode(ERPInteractionMenuInteractorModes _newMode)
    {
        _currentMode = _newMode;
        foreach (var mode in _interactorModes)
        {
            if (mode.Key != _newMode)
            {
                mode.Value.Pressed = false;
            }
        }
    }
};
