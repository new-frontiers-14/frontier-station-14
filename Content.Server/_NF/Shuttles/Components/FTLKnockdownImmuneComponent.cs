using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Shuttles.Components;

/// <summary>
///     Denotes an entity as being immune from knockdown on FTL
/// </summary>
[RegisterComponent]
public sealed partial class FTLKnockdownImmuneComponent : Component
{
}
