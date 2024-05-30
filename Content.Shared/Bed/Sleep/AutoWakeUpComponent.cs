using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Bed.Sleep;

/// <summary>
/// Frontier - Added to AI to allow auto waking up after 5 secs.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class AutoWakeUpComponent : Component
{
}
