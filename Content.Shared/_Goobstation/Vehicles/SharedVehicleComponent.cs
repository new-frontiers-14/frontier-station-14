using System.Numerics; // Frontier
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Vehicles; // Frontier: migrate under _Goobstation

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] // Frontier: add AutoGenerateComponentState
public sealed partial class VehicleComponent : Component
{
    [DataField, AutoNetworkedField] // Frontier: ViewVariables to DataField & AutoNetworked
    public EntityUid? Driver;

    [DataField, AutoNetworkedField] // Frontier: VV<DataField, AutoNetwork
    public EntityUid? HornAction;

    [DataField, AutoNetworkedField] // Frontier: VV<DataField, AutoNetwork
    public EntityUid? SirenAction;

    // public bool SirenEnabled = false; // Frontier

    [ViewVariables] // Frontier
    public EntityUid? SirenStream;

    /// <summary>
    /// If non-zero how many virtual items to spawn on the driver
    /// unbuckles them if they dont have enough
    /// </summary>
    [DataField]
    public int RequiredHands = 1;

    /// <summary>
    /// Will the vehicle move when a driver buckles
    /// </summary>
    [DataField]
    public bool EngineRunning = false;

    /// <summary>
    /// What sound to play when the driver presses the horn action (plays once)
    /// </summary>
    [DataField]
    public SoundSpecifier? HornSound;

    /// <summary>
    /// What sound to play when the driver presses the siren action (loops)
    /// </summary>
    [DataField]
    public SoundSpecifier? SirenSound;

    /// <summary>
    /// If they should be rendered ontop of the vehicle if true or behind
    /// </summary>
    [DataField]
    public VehicleRenderOver RenderOver = VehicleRenderOver.None;

    // Frontier: sprite offset per
    [DataField]
    public Vector2 SouthOffset = Vector2.Zero;

    [DataField]
    public Vector2 NorthOffset = Vector2.Zero;

    [DataField]
    public Vector2 EastOffset = Vector2.Zero;

    [DataField]
    public Vector2 WestOffset = Vector2.Zero;

    /// <summary>
    /// The container name for the vehicle key.
    /// </summary>
    [DataField]
    public string KeySlotId = "key_slot";
    // End Frontier: old buckle offset logic
}
[Serializable, NetSerializable]
public enum VehicleState : byte
{
    Animated,
    DrawOver
}

// Frontier: use RsiDirection-compatible flags
[Serializable, NetSerializable, Flags]
public enum VehicleRenderOver
{
    None = 0,
    South = 1,
    North = 2,
    East = 4,
    West = 8,
}
// End Frontier
