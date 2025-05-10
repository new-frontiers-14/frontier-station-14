namespace Content.Shared.Power;

/// <summary>
/// Component for a powered machine that slowly powers on and off over a period of time.
/// </summary>
public abstract partial class SharedPowerChargeComponent : Component
{
    /// <summary>
    /// The title used for the default charged machine window if used
    /// </summary>
    [DataField]
    public LocId WindowTitle { get; set; } = string.Empty;

    // Frontier: actions
    /// <summary>
    /// Show a action button on UI
    /// </summary>
    [DataField]
    public bool ActionUI { get; set; } = false;
    // End Frontier: actions
}
