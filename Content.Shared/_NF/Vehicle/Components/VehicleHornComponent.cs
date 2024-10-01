using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Vehicle;

namespace Content.Shared._NF.Vehicle.Components;

/// <summary>
/// 
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedVehicleSystem))]
public sealed partial class VehicleHornComponent : Component
{
    /// <summary>
    /// The sound that the horn makes
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? HornSound = new SoundPathSpecifier("/Audio/Effects/Vehicle/carhorn.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f)
    };

    [ViewVariables]
    public EntityUid? HonkPlayingStream;

    [DataField]
    public EntProtoId? Action = "ActionVehicleHorn";

    /// <summary>
    /// The action for the horn (if any)
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ActionEntity;
}
