using Robust.Shared.Map;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on a gun when it would like to take the specified amount of ammo.
/// </summary>
public sealed class TakeAmmoEvent : EntityEventArgs
{
    public readonly EntityUid? User;
    public readonly int Shots;
    public List<(EntityUid? Entity, IShootable Shootable)> Ammo;

    /// <summary>
    /// If no ammo returned what is the reason for it?
    /// </summary>
    public string? Reason;

    /// <summary>
    /// Coordinates to spawn the ammo at.
    /// </summary>
    public EntityCoordinates Coordinates;

    // Frontier: better revolver reloading
    /// <summary>
    /// Does this event represent an intent to fire, or to safely remove ammo from an entity?
    /// </summary>
    public bool WillBeFired;
    // End Frontier

    public TakeAmmoEvent(int shots, List<(EntityUid? Entity, IShootable Shootable)> ammo, EntityCoordinates coordinates, EntityUid? user, bool willBeFired = false) // Frontier: add willBeFired
    {
        Shots = shots;
        Ammo = ammo;
        Coordinates = coordinates;
        User = user;
        WillBeFired = willBeFired; // Frontier
    }
}
