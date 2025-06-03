namespace Content.Server._NF.Atmos.Components;

/// <summary>
/// Component that can be used to add (or remove) fire stacks when used as a projectile.
/// Useful vs. IgniteOnCollide for non-disposable projectiles, like crossbow bolts.
/// </summary>
[RegisterComponent]
public sealed partial class IgniteOnProjectileHitComponent : Component
{
    [DataField]
    public float FireStacks { get; set; }
}
