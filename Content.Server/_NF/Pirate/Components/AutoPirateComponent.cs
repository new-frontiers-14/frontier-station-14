using Content.Server.Station.Systems;

namespace Content.Server._NF.Pirate.Components;

/// <summary>
/// Denotes an entity whose mind gets the pirate role when spawned.
/// Comparable to AutoTraitorComponent
/// </summary>
[RegisterComponent]
public sealed partial class AutoPirateComponent : Component
{
    // Whether or not this rule should be a captain.
    [DataField]
    public bool Captain = false;
}
