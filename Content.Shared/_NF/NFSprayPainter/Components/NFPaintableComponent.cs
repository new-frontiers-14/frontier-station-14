using Content.Shared._NF.NFSprayPainter.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.NFSprayPainter.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NFPaintableComponent : Component
{
    /// <summary>
    /// Group of styles this airlock can be painted with, e.g. glass, standard or external.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<NFPaintableGroupPrototype> Group = string.Empty;
}
