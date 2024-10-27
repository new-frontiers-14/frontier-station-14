using Content.Server.StationEvents.Events;
using Content.Shared.Bank.Components;
using Content.Shared.Storage;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BluespaceErrorRule))]
public sealed partial class BluespaceErrorRuleComponent : Component
{
    /// <summary>
    /// List of paths to the grids that can be bluespaced in.
    /// </summary>
    [DataField]
    public List<string> GridPaths = new();

    /// <summary>
    /// The color of your thing. The name should be set by the mapper when mapping.
    /// </summary>
    [DataField]
    public Color Color = new Color(225, 15, 155);

    /// <summary>
    /// Multiplier to apply to the remaining value of a grid, to be deposited in the station account for defending
    /// </summary>
    [DataField]
    public float RewardFactor = 0f;

    /// <summary>
    /// Sector accounts to be credited on event completion.
    /// </summary>
    [DataField]
    public List<SectorBankAccount> RewardAccounts = new();

    /// <summary>
    /// The grid in question, set after starting the event
    /// </summary>
    [DataField]
    public EntityUid? GridUid = null;

    /// <summary>
    /// How much the grid is appraised at upon entering into existence, set after starting the event
    /// </summary>
    [DataField]
    public double startingValue = 0;
}
