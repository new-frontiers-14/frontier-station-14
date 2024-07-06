using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows GunSystem to automatically fire while this component is enabled
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunSystem)), AutoGenerateComponentState]
public sealed partial class AutoShootGunComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Whether the thruster has been force to be enabled / disabled (e.g. VV, interaction, etc.)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool On { get; set; } = true;

    /// <summary>
    /// This determines whether the thruster is actually enabled for the purposes of thrust
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsOn;

    /// <summary>
    ///     Frontier - Amount of charge this needs from an APC per second to function.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float OriginalLoad { get; set; } = 0;
}
