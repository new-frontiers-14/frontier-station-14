using Content.Shared.Materials;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._NF.Manufacturing.Components;

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
    [DataField(serverOnly: true)]
    public string NodeName = "input";

    ///<summary>
    /// The period between depositing money into a sector account.
    /// Also the T in Tk*a^(log10(x/T)-R) for rate calculation
    ///</summary>
    [DataField(serverOnly: true)]
    public TimeSpan SpawnCheckPeriod = TimeSpan.FromSeconds(20);

    ///<summary>
    /// The next time this power plant is selling accumulated power.
    /// Should not be changedduring runtime, will cause errors in deposit amounts.
    ///</summary>
    [DataField(serverOnly: true, customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSpawnCheck;

    ///<summary>
    /// The total energy accumulated, in joules.
    ///</summary>
    [DataField(serverOnly: true)]
    public float AccumulatedEnergy;

    ///<summary>
    /// The total energy accumulated this spawn check, in joules.
    ///</summary>
    [DataField(serverOnly: true)]
    public float AccumulatedSpawnCheckEnergy;

    ///<summary>
    /// The material to use, if any.
    ///</summary>
    [DataField(serverOnly: true)]
    public ProtoId<MaterialPrototype>? Material;

    ///<summary>
    /// The amount of material to use for one unit of output.
    ///</summary>
    [DataField(serverOnly: true)]
    public int MaterialAmount;

    ///<summary>
    /// If true, the machine is currently producing an entity, and has consumed any requisite materials.
    ///</summary>
    [DataField(serverOnly: true)]
    public bool Processing;

    ///<summary>
    /// The name of the container to output the created entity.
    ///</summary>
    [DataField(serverOnly: true)]
    public string SlotName = "output";

    ///<summary>
    /// The entity prototype ID to spawn when enough energy is accumulated.
    ///</summary>
    [DataField(serverOnly: true, required: true)]
    public EntProtoId Spawn;

    ///<summary>
    /// The necessary energy to spawn a unit in the output slot.
    ///</summary>
    [DataField(serverOnly: true, required: true)]
    public float EnergyPerSpawn;
    #endregion Generation

    #region Efficiency Scaling
    ///<summary>
    /// The maximum power to increase without logarithmic reduction.
    ///</summary>
    [DataField(serverOnly: true)]
    public float LinearMaxValue = 3_000_000;

    ///<summary>
    /// The base on power the logarithmic mode: a in Tk*a^(log10(x/T)-R)
    ///</summary>
    [DataField(serverOnly: true)]
    public float LogarithmRateBase = 2.5f;

    ///<summary>
    /// The coefficient of the logarithmic mode: k in Tk*a^(log10(x/T)-R)
    /// Note: should be set to LinearMaxValue for a continuous function.
    ///</summary>
    [DataField(serverOnly: true)]
    public float LogarithmCoefficient = 3_000_000f;

    ///<summary>
    /// The exponential subtrahend of the logarithmic mode: R in Tk*a^(log10(x/T)-R)
    /// Note: should be set to log10(LinearMaxValue) for a continuous function.
    ///</summary>
    [DataField(serverOnly: true)]
    public float LogarithmSubtrahend = 6.0f; // log10(1_000_000)
    #endregion Efficiency Scaling

    ///<summary>
    /// Maximum effective power to store towards spawning an item.
    ///</summary>
    [DataField(serverOnly: true)]
    public float MaxEffectivePower = 15_000_000; // 80s per entity, ~910 MW

    ///<summary>
    /// The minimum requestable power.
    ///</summary>
    [DataField]
    public float MinimumRequestablePower = 500; // 500 W

    ///<summary>
    /// The maximum requestable power.
    ///</summary>
    [DataField]
    public float MaximumRequestablePower = 100_000_000_000; // 100 GW
}
