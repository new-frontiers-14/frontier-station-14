using Robust.Shared.GameStates;

namespace Content.Shared._Park.Species.Shadowkin.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowkinDarkSwappedComponent : Component
{
    /// <summary>
    ///     If it should be sent to the dark
    /// </summary>
    [DataField("invisible")]
    public bool Invisible = true;

    /// <summary>
    ///     If it should be pacified
    /// </summary>
    [DataField("pacify")]
    public bool Pacify = true;

    /// <summary>
    ///     If it should dim nearby lights
    /// </summary>
    [DataField("darken"), ViewVariables(VVAccess.ReadWrite)]
    public bool Darken = true;


    /// <summary>
    ///     How far to dim nearby lights
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float DarkenRange = 5f;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> DarkenedLights = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public float DarkenRate = 0.084f; // 1/12th of a second

    [ViewVariables(VVAccess.ReadWrite)]
    public float DarkenAccumulator = 0f;
}
