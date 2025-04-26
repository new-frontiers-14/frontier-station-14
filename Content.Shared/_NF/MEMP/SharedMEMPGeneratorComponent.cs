using Content.Shared.Power;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.MEMP;

[NetworkedComponent()]
[Virtual]
public partial class SharedMEMPGeneratorComponent : Component
{
    /// <summary>
    /// A map of the sprites used by the mobile emp given its status.
    /// </summary>
    [DataField]
    public Dictionary<PowerChargeStatus, string> SpriteMap = new();

    /// <summary>
    /// The sprite used by the core of the mobile emp when the mobile emp is starting up.
    /// </summary>
    [DataField]
    public string CoreStartupState = "startup";

    /// <summary>
    /// The sprite used by the core of the mobile emp when the mobile emp is idle.
    /// </summary>
    [DataField]
    public string CoreIdleState = "idle";

    /// <summary>
    /// The sprite used by the core of the mobile emp when the mobile emp is activating.
    /// </summary>
    [DataField]
    public string CoreActivatingState = "activating";

    /// <summary>
    /// The sprite used by the core of the mobile emp when the mobile emp is active.
    /// </summary>
    [DataField]
    public string CoreActivatedState = "activated";

    [DataField]
    public float Range = 100.0f;

    /// <summary>
    /// How much energy will be consumed per battery in range
    /// </summary>
    [DataField]
    public float EnergyConsumption = 1000000;

    /// <summary>
    /// How long it disables targets in seconds
    /// </summary>
    [DataField]
    public float DisableDuration = 60f;
}
