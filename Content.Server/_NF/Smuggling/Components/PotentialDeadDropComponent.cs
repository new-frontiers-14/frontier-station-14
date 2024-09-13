namespace Content.Server._NF.Smuggling.Components;

/// <summary>
///     Denotes an item as being a potential dead drop candidate.
///     If/when dead drops are generated or moved, this entity may
///     receive a DeadDropComponent.
/// </summary>
/// <remarks>
///     Should be improved upon as a way to generate random dead drops.
/// </remarks>
[RegisterComponent]
public sealed partial class PotentialDeadDropComponent : Component
{
    [DataField]
    public string HintText = "dead-drop-hint-generic";
}
