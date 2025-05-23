namespace Content.Server._NF.Projectile.Components;

/// <summary>
/// Randomly blinds an entity hitting something else as a projectile.
/// </summary>
[RegisterComponent]
public sealed partial class BlindOnProjectileHitComponent : Component
{
    [DataField]
    public float Prob = 1.0f;

    [DataField]
    public TimeSpan BlindTime = TimeSpan.FromSeconds(2);
}
