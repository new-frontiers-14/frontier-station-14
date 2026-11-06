using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.Weapons.Ranged.Components;

/// <summary>
/// Component for checking inside a characters hands and other slots, to which entity is bound.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunSystem))]
public sealed partial class BorgAmmoProviderComponent : Component
{
    /// <summary>
    /// A whitelist for determining whether container is valid or not .
    /// </summary>
    [DataField]
    public EntityWhitelist? ContainerWhitelist;
}
