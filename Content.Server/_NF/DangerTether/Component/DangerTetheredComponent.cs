namespace Content.Server._NF.DangerTether;

/// <summary>
/// If this entity is out of range of an OutOfRangeTether, it will be deleted.
/// </summary>
[RegisterComponent]
public sealed partial class DangerTetheredComponent : Component;
