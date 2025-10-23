using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Explosion.Components;

/// <summary>
/// Replaces an entity with a given prototype when triggered.
/// </summary>
[RegisterComponent, Access(typeof(TriggerSystem))]
public sealed partial class ReplaceOnTriggerComponent : Component
{
    /// <summary>
    ///     The prototype to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Proto = string.Empty;
}
