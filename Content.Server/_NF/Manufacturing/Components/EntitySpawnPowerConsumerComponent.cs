using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._NF.Manufacturing.Components;

/// <summary>
/// An entity with this will produce an entity over time after accumulating charge.
/// Entities are output after a given amount of energy is accumulated.
/// At high power input, energy accumulated diminishes logarithmically.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class EntitySpawnPowerConsumerComponent : Component
{
    #region Generation
    ///<summary>
    /// The name of the node to be connected/disconnected.
    ///</summary>
    [DataField]
    public string NodeName = "input";

    ///<summary>
    /// The period between depositing money into a sector account.
    /// Also the T in Tk*a^(log10(x/T)-R) for rate calculation
    ///</summary>
    [DataField]
    public TimeSpan SpawnCheckPeriod = TimeSpan.FromSeconds(20);

    ///<summary>
    /// The next time this power plant is selling accumulated power.
    /// Should not be changedduring runtime, will cause errors in deposit amounts.
    ///</summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSpawnCheck;

    ///<summary>
    /// The total energy accumulated, in joules.
    ///</summary>
    [DataField]
    public float AccumulatedEnergy;

    ///<summary>
    /// The total energy accumulated this spawn check, in joules.
    ///</summary>
    [DataField]
    public float AccumulatedSpawnCheckEnergy;

    ///<summary>
    /// The name of the container to output the
    ///</summary>
    [DataField]
    public string SlotName = "output";

    ///<summary>
    /// The entity prototype ID to spawn when enough energy is accumulated.
    ///</summary>
    [DataField(required: true)]
    public EntProtoId Spawn;

    ///<summary>
    /// The necessary energy to spawn a unit in the output
    ///</summary>
    [DataField(required: true)]
    public float EnergyPerSpawn;
    #endregion Generation

    #region Efficiency Scaling
    ///<summary>
    /// The maximum power to increase without logarithmic reduction.
    ///</summary>
    [DataField]
    public float LinearMaxValue = 1_000_000;

    ///<summary>
    /// The base on power the logarithmic mode: a in Tk*a^(log10(x/T)-R)
    ///</summary>
    [DataField]
    public float LogarithmRateBase = 3.0f;

    ///<summary>
    /// The coefficient of the logarithmic mode: k in Tk*a^(log10(x/T)-R)
    /// Note: should be set to LinearMaxValue for a continuous function.
    ///</summary>
    [DataField]
    public float LogarithmCoefficient = 1_000_000f;

    ///<summary>
    /// The exponential subtrahend of the logarithmic mode: R in Tk*a^(log10(x/T)-R)
    /// Note: should be set to log10(LinearMaxValue) for a continuous function.
    ///</summary>
    [DataField]
    public float LogarithmSubtrahend = 6.0f; // log10(1_000_000)
    #endregion Logarithmic Rates
}
