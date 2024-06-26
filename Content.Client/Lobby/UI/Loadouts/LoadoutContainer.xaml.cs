using Content.Shared.Clothing;
using Content.Shared.Preferences.Loadouts;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI.Loadouts;

[GenerateTypedNameReferences]
public sealed partial class LoadoutContainer : BoxContainer
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private readonly EntityUid? _entity;

    public Button Select => SelectButton;

    public LoadoutContainer(ProtoId<LoadoutPrototype> proto, bool disabled, FormattedMessage? reason)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        SelectButton.Disabled = disabled;

        if (disabled && reason != null)
        {
            var tooltip = new Tooltip();
            tooltip.SetMessage(reason);
            SelectButton.TooltipSupplier = _ => tooltip;
        }

        if (_protoManager.TryIndex(proto, out var loadProto))
        {
            // Frontier: overrideable prototype fields (description, name, icon [via entity])
            Price.Text = "$" + loadProto.Price;

            bool hasDescription = !string.IsNullOrEmpty(loadProto.Description);
            bool hasEntity = !string.IsNullOrEmpty(loadProto.PreviewEntity?.Id);

            EntProtoId? ent = null;
            if (!hasEntity || !hasDescription) {
                ent = _entManager.System<LoadoutSystem>().GetFirstOrNull(loadProto);
            }
            var finalEnt = hasEntity ? loadProto.PreviewEntity : ent;
            if (finalEnt != null)
            {
                _entity = _entManager.SpawnEntity(finalEnt, MapCoordinates.Nullspace);
                Sprite.SetEntity(_entity);

                var spriteTooltip = new Tooltip();
                var description = hasDescription ? loadProto.Description : _entManager.GetComponent<MetaDataComponent>(_entity.Value).EntityDescription; 
                spriteTooltip.SetMessage(FormattedMessage.FromUnformatted(description));
                Sprite.TooltipSupplier = _ => spriteTooltip; // Frontier: TooltipSupplier<Sprite.TooltipSupplier?
            }
            // End Frontier
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _entManager.DeleteEntity(_entity);
    }

    public bool Pressed
    {
        get => SelectButton.Pressed;
        set => SelectButton.Pressed = value;
    }

    public string? Text
    {
        get => SelectButton.Text;
        set => SelectButton.Text = value;
    }
}
