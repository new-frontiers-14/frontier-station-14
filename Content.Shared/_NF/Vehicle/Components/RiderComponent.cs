using Content.Shared._Goobstation.Vehicles;
using Robust.Shared.GameStates;
namespace Content.Shared._NF.Vehicle.Components;

/// <summary>
/// Denotes an entity as being in control of a vehicle.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedVehicleSystem))]
public sealed partial class VehicleRiderComponent : Component;
