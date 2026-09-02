using Content.Shared.Atmos;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._NF.Manufacturing.Components;

/// <summary>
/// An entity with this will produce some amount of gas over time if supplied with power.
/// Gas is output at a regular frequency, and the amount of gas spawned scales with the amount of power given.
/// At high power input, gas returns diminish logarithmically.
/// Expected to be used with a GasCanister that can contain the mixture.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class GasSpawnPowerConsumerComponent : Component
{
    #region Generation Params
    ///<summary>
    /// The name of the power node to be connected/disconnected.
    ///</summary>
    [DataField]
    public string PowerNodeName = "input";

    ///<summary>
    /// The period between depositing money into a sector account.
    /// Also the T in Tk*a^(log10(x/T)-R) for rate calculation
    ///</summary>
    [DataField]
    public TimeSpan SpawnCheckPeriod = TimeSpan.FromSeconds(4);

    ///<summary>
    /// The next time this power plant is selling accumulated power.
    /// Should not be changedduring runtime, will cause errors in deposit amounts.
    ///</summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSpawnCheck;

    ///<summary>
    /// The total energy accumulated, in watts.
    ///</summary>
    [DataField]
    public float AccumulatedEnergy;

    ///<summary>
    /// The energy accumulated this spawn check, in watts.
    ///</summary>
    [DataField]
    public float AccumulatedSpawnCheckEnergy;

    ///<summary>
    /// The total amount of energy required to spawn one mole of gas.
    ///</summary>
    [DataField]
    public float EnergyPerMole = 100_000;

    ///<summary>
    /// The total mixture to spawn per unit of energy.
    ///</summary>
    [DataField]
    public GasMixture SpawnMixture { get; set; } = new();
    #endregion Generation Params

    #region Linear Rates
    ///<summary>
    /// The number of moles of gas to spawn per joule of power.
    ///</summary>
    [DataField]
    public float LinearRate = 0.000001f; // 1 mol/100 kW

    ///<summary>
    /// The maximum value (inclusive) of the linear mode per deposit, in watts
    ///</summary>
    [DataField]
    public float LinearMaxValue = 2_000_000; // 1 MW (10 mol/s)
    #endregion Linear Rates

    // Logarithmic fields: at very high levels of power generation, incremental gains decrease logarithmically to prevent runaway cash generation
    #region Logarithmic Rates

    ///<summary>
    /// The base on power the logarithmic mode: a in Tk*a^(log10(x/T)-R)
    ///</summary>
    [DataField]
    public float LogarithmRateBase = 2.5f;

    ///<summary>
    /// The coefficient of the logarithmic mode: k in Tk*a^(log10(x/T)-R)
    /// Note: should be set to LinearRate*LinearMaxValue for a continuous function.
    ///</summary>
    [DataField]
    public float LogarithmCoefficient = 2000000f;

    ///<summary>
    /// The exponential subtrahend of the logarithmic mode: R in Tk*a^(log10(x/T)-R)
    /// Note: should be set to log10(LinearMaxValue) for a continuous function.
    ///</summary>
    [DataField]
    public float LogarithmSubtrahend = 6.0f; // log10(1_000_000)
    #endregion Logarithmic Rates

    ///<summary>
    /// The maximum number of moles of gas to spawn, per second.
    ///</summary>
    [DataField]
    public float MaximumMolesPerSecond = 150.0f; // ~0.93 GW

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
