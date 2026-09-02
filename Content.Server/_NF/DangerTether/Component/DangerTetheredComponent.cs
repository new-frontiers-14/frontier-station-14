namespace Content.Server._NF.DangerTether;

/// <summary>
/// If this entity is out of range of all entities with DangerTetherComponent on the same map, it will be deleted.
/// </summary>
[RegisterComponent]
public sealed partial class DangerTetheredComponent : Component;
