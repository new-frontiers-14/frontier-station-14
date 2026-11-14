namespace Content.Server._NF.River.Components;

/// <summary>
/// Add this Component to a POI to force it to be connected with Rivers with regards to the RiverNodeSystem.WorldGen where relevant.
/// This is used to allow POIs with the [Hide] IFFFlag to still be connected by rivers. Useful for discoverable locations and such.
/// </summary>
[RegisterComponent]
public sealed partial class ForceRiverConnectionComponent : Component
{

}
