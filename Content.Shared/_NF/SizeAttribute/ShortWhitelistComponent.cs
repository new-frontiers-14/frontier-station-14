// Must be shared, used by character setup UI
namespace Content.Shared._NF.SizeAttribute;

[RegisterComponent]
public sealed partial class ShortWhitelistComponent : Component
{
    [DataField]
    public float Scale = 0f;

    [DataField]
    public float Density = 0f;

    [DataField]
    public bool PseudoItem = false;

    [DataField]
    public bool CosmeticOnly = true;

    [DataField]
    public List<Box2i>? Shape;

    [DataField]
    public Vector2i? StoredOffset;

    [DataField]
    public float StoredRotation = 0;
}
