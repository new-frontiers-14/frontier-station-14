using Content.Server.StationEvents.Events;
using Content.Shared.Storage;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BluespaceErrorRule))]
public sealed partial class BluespaceErrorRuleComponent : Component
{
    /// <summary>
    /// Path to the grid that gets bluspaced in
    /// </summary>
    [DataField("gridPath")]
    public string GridPath = "";

    /// <summary>
    /// The color of your thing. the name should be set by the mapper when mapping.
    /// </summary>
    [DataField("color")]
    public Color Color = new Color(225, 15, 155);

    /// <summary>
    /// Multiplier to apply to the remaining value of a grid, to be deposited in the station account for defending
    /// </summary>
    [DataField("rewardFactor")]
    public float RewardFactor = 0f;

    /// <summary>
    /// The grid in question, set after starting the event
    /// </summary>
    [DataField("gridUid")]
    public EntityUid? GridUid = null;

    /// <summary>
    /// How much the grid is appraised at upon entering into existance, set after starting the event
    /// </summary>
    [DataField("startingValue")]
    public double startingValue = 0;
}
