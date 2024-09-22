using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.VariationPass.Components;

/// <summary>
/// This handles generating round-start dead drop hints.
/// </summary>
[RegisterComponent]
public sealed partial class DeadDropHintVariationPassComponent : Component
{
    /// <summary>
    ///     Chance that a potential hint will be generated on a table.
    ///     Remember, the average number 
    /// </summary>
    [DataField]
    public float HintSpawnChance = 0.02f;

    /// <summary>
    ///     The entity to spawn for a hint.
    /// </summary>
    [DataField]
    public EntProtoId HintSpawnPrototype = "PaperDeadDropHint";
}
