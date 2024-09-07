// Must be shared, used by character setup UI
namespace Content.Shared._NF.SizeAttribute;

[RegisterComponent]
public sealed partial class TallWhitelistComponent : Component
{
    [DataField]
    public float Scale = 0f;

    [DataField]
    public float Density = 0f;

    [DataField]
    public bool PseudoItem = false;

    [DataField]
    public bool CosmeticOnly = true;
}
