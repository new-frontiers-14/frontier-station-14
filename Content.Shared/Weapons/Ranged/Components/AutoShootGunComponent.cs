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
    /// Frontier - Whether the gun is switched on (e.g. through user interaction)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool On { get; set; } = true;

    /// <summary>
    /// Frontier - Whether or not the gun can actually fire (i.e. switched on and receiving power if needed)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanFire;

    /// <summary>
    /// Frontier - Amount of power this gun needs from an APC in Watts to function.
    /// </summary>
    public float OriginalLoad { get; set; } = 0;
}
