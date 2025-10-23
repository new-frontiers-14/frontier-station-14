namespace Content.Server._NF.DangerTether;

/// <summary>
/// A dangerous object that is tethered around one or more particular entities.  If it breaks a maximum distance from all of them, it is deleted.
/// Currently, all tethers are equivalent, bound all tethered objects, and are map-scoped.
/// </summary>
[RegisterComponent]
public sealed partial class DangerTetherComponent : Component
{
    [DataField(required: true)]
    public float MaxDistance;
}
