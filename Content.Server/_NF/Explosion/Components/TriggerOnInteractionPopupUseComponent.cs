namespace Content.Server._NF.Explosion.Components;

/// <summary>
/// Triggers an object when used for a successful/unsuccessful popup interaction.
/// Defaults to triggering on success only.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerOnInteractionPopupUseComponent : Component
{
    [DataField]
    public bool TriggerOnFailure = false;

    [DataField]
    public bool TriggerOnSuccess = true;
}
