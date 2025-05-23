using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Atmos.Piping.Binary.Components;

/// <summary>
/// A machine that converts high temperature CO2 into oxygen and converts accumulated carbon into diamonds.
/// </summary>
[RegisterComponent]
public sealed partial class DiamondDepositorComponent : Component
{
    /// <summary>
    /// Whether or not the machine is currently reacting on its input gas.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public bool Reacting { get; set; }

    /// <summary>
    /// Whether or not the machine has consumed a seed item (and can react to gas)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public bool ConsumedSeed { get; set; }

    [DataField]
    public string InletName { get; set; } = "inlet";

    [DataField]
    public string OutletName { get; set; } = "outlet";

    [ViewVariables(VVAccess.ReadWrite)]
    public float TargetTemp = 15000 + Atmospherics.T0C;

    /// <summary>
    /// The maximum error, in Kelvin, that the input gas needs to be at.
    /// </summary>
    [DataField]
    public float MaxTempError = 3000;

    /// <summary>
    /// The accumulated temperature error multiplied by the number of moles of gas received so far.
    /// </summary>
    [DataField]
    public float AccumulatedError;

    /// <summary>
    /// The accumulated number of moles of gas.
    /// </summary>
    [DataField]
    public float AccumulatedMoles;

    /// <summary>
    /// The fraction of the gas input that is converted
    /// </summary>
    [DataField]
    public float ConversionFactor = 0.05f;

    /// <summary>
    /// The number of moles needed to output one item of 
    /// </summary>
    [DataField]
    public float NeededMoles = 250;

    /// <summary>
    /// The product of the accumulated number of moles
    /// </summary>
    [DataField]
    public int SeedItemsUsedPerRun = 1;

    /// <summary>
    /// The number of seed items consumed before running
    /// </summary>
    [DataField]
    public int ConsumedSeedItems;

    /// <summary>
    /// The name of the slot used to store seed material.
    /// </summary>
    [DataField]
    public string SeedSlotName = "seed";

    /// <summary>
    /// The ID of the entity prototype to spawn.
    /// </summary>
    [DataField]
    public EntProtoId SpawnName;

    /// <summary>
    /// The number of items to produce.
    /// </summary>
    [DataField]
    public int SpawnQuantity = 1;
}
