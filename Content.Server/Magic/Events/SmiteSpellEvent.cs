using Content.Shared.Actions;

namespace Content.Server.Magic.Events;

public sealed partial class SmiteSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    /// <summary>
    ///     Should this smite delete all parts/mechanisms gibbed except for the brain?
    /// </summary>
    [DataField("deleteNonBrainParts")]
    public bool DeleteNonBrainParts = false;

    [DataField("speech")]
    public string? Speech { get; set;}
}
