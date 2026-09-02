namespace Content.Server._NF.Pirate.Components;

/// <summary>
/// Denotes an entity whose mind gets the pirate first mate role when spawned.
/// Similar to AutoTraitorComponent.
/// </summary>
[RegisterComponent]
public sealed partial class AutoPirateFirstMateComponent : Component
{
    [DataField]
    public bool ApplyFaction = true;
}
