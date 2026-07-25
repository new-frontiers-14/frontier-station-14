using Content.Shared.Trigger.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Trigger.Components;

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
