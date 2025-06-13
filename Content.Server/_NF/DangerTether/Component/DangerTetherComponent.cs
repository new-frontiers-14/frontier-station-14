namespace Content.Server._NF.DangerTether;

/// <summary>
/// A dangerous object that is tethered around a certain point.  If it breaks the point, it is deleted.
/// Currently, all tethers are identical (not under a key) and map-scoped.
/// </summary>
[RegisterComponent]
public sealed partial class DangerTetherComponent : Component
{
    [DataField(required: true)]
    public float MaxDistance;
}
