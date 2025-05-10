namespace Content.Server._NF.Pirate.Components;

/// <summary>
/// Denotes an entity whose mind gets the pirate role when spawned.
/// Similar to AutoTraitorComponent.
/// </summary>
[RegisterComponent]
public sealed partial class AutoPirateComponent : Component
{
    [DataField]
    public bool ApplyFaction = true;
}
