using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._DV.Abilities;

/// <summary>
/// Spawns the item from <see cref="ItemCougherComponent"/> after the coughing sound is finished.
/// </summary>
/// <remarks>
/// Client doesn't care about spawning so the field isn't networked.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(ItemCougherSystem))]
[AutoGenerateComponentPause]
public sealed partial class CoughingUpItemComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextCough;
}
