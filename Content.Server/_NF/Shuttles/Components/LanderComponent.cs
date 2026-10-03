namespace Content.Server._NF.Shuttles.Components;

/// <summary>
///     Denotes an entity as being an Expedition Lander.
/// </summary>
[RegisterComponent]
public sealed partial class LanderComponent : Component
{
    [DataField]
    public EntityUid MotherStation;
}
