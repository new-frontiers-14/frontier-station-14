using Robust.Shared.GameStates;

namespace Content.Shared._NF.Weapons.Components;

/// <summary>
/// Holds details for a given weapon.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NFWeaponDetailsComponent : Component
{
    /// <summary>
    /// Who manufactured this weapon?
    /// </summary>
    [DataField]
    public LocId? Manufacturer;

    /// <summary>
    /// What color should the manufacturer be printed in?
    /// </summary>
    [DataField]
    public Color ManufacturerColor = Color.LightBlue;

    /// <summary>
    /// What class of weapon is this?
    /// </summary>
    [DataField]
    public LocId? Class;
}
