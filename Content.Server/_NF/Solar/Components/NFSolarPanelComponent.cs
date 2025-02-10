using Content.Server._NF.Solar.EntitySystems;

namespace Content.Server._NF.Solar.Components;

/// <summary>
///     This is a solar panel.
///     It updates with grid-based tracking information.
///     It generates power from the sun based on coverage.
///     Largely based on Space Station 14's SolarPanelComponent.
/// </summary>
[RegisterComponent]
[Access(typeof(NFPowerSolarSystem))]
public sealed partial class NFSolarPanelComponent : Component
{
    /// <summary>
    /// Maximum supply output by this panel (coverage = 1)
    /// </summary>
    [DataField("maxSupply")]
    public int MaxSupply = 750;

    /// <summary>
    /// Current coverage of this panel (from 0 to 1).
    /// This is updated by <see cref='PowerSolarSystem'/>.
    /// DO NOT WRITE WITHOUT CALLING UpdateSupply()!
    /// </summary>
    [ViewVariables]
    public float Coverage { get; set; } = 0;
}
