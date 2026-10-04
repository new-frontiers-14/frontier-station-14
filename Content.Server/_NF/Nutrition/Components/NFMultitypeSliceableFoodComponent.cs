using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.Components;

[RegisterComponent, Access(typeof(SliceableFoodSystem))]
public sealed partial class NFMultitypeSliceableFoodComponent : Component
{
    /// <summary>
    /// List of entity spawning entries to produce when sliced
    /// (consider: adding filters of some kind to influence which reagents go into what item. that's probably too much #effortpoasting though)
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry>? Slices;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");

    /// <summary>
    /// how long it takes for this food to be sliced
    /// </summary>
    [DataField]
    public float SliceTime = 1f;

    /// <summary>
    /// all the pieces will be shifted in random directions.
    /// </summary>
    [DataField]
    public float SpawnOffset = 0.5f;
}
