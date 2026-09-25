using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Item.PseudoItemWhitelist;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class PseudoItemWhitelistComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<Box2i> Shape = new List<Box2i>{
        new Box2i(0, 0, 1, 4),
        new Box2i(0, 2, 3, 4),
        new Box2i(4, 0, 5, 4)
    };

    [DataField, AutoNetworkedField]
    public Vector2i StoredOffset = new(0, 17);

    [DataField, AutoNetworkedField]
    public float StoredRotation = 0f;

    /// <summary>
    ///     Maximum profile scale that allows a PseudoItem component.
    /// </summary>
    [DataField]
    public float MaxProfileScale { get; private set; } = 0.9f;

    /// <summary>
    ///     Minimum profile scale that allows a PseudoItem component.
    /// </summary>
    [DataField]
    public float MinProfileScale { get; private set; } = 0;
}
