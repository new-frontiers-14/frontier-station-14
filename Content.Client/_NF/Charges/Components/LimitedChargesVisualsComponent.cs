using Content.Client._NF.Charges.Systems;

namespace Content.Client._NF.Charges.Components;

/// <summary>
/// Visualizer for limited charge items, largely based on MagazineVisuals; can change states based on charge count.
/// </summary>
[RegisterComponent, Access(typeof(LimitedChargesVisualizerSystem))]
public sealed partial class LimitedChargesVisualsComponent : Component
{
    /// <summary>
    /// The prefix we use for states.
    /// </summary>
    [DataField] public string? ChargePrefix;

    /// <summary>
    /// How many steps there are.
    /// </summary>
    [DataField] public int ChargeSteps;

    /// <summary>
    /// Should we hide when the count is 0?
    /// </summary>
    [DataField] public bool ZeroVisible;
}

public enum LimitedChargesVisualLayers : byte
{
    Charges,
}
