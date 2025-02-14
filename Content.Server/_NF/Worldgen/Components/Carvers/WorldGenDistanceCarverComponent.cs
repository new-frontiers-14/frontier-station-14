namespace Content.Server._NF.Worldgen.Components.Carvers;

/// <summary>
/// This denotes an entity that should have a 
/// </summary>
[RegisterComponent]
public sealed partial class WorldGenDistanceCarverComponent : Component
{
    [DataField]
    public float MinDistance;

    [DataField]
    public float MaxDistance;
}
