using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NF.Shuttles.Components;

/// <summary>
/// This is a stub component for allowing/denying FTL on a shuttle.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShuttleFTLComponent : Component
{
}
