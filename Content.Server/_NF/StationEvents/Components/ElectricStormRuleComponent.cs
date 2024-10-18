using System.Threading;
using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Solar Flare event specific configuration
/// </summary>
[RegisterComponent, Access(typeof(ElectricStormRule))]
public sealed partial class ElectricStormRuleComponent : Component
{
    /// <summary>
    ///     Chance light bulb breaks per second during event
    /// </summary>
    [DataField]
    public float ComputerChance;

    /// <summary>
    ///     Chance door toggles per second during event
    /// </summary>
    [DataField]
    public float MachineChance;

    /// <summary>
    ///     Minimum faxes to send
    /// </summary>
    [DataField]
    public int PlayersPerTargets { get; private set; } = 5;

    /// <summary>
    ///     Minimum faxes to send
    /// </summary>
    [DataField]
    public int MinTargets { get; private set; } = 1;

    /// <summary>
    ///     Maximum faxes to send
    /// </summary>
    [DataField]
    public int MaxTargets { get; private set; } = 10;
}
