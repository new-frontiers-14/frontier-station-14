using Content.Shared._NF.Bank.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._NF.Power.Components;

/// <summary>
/// An entity with this will pay out a given sector bank account regularly depending on the amount of power received.
/// Payouts occur at a fixed period, but the rate of pay depends on the average power input over that period.
/// At high power input, the incremental rate of pay diminishes logarithmically.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PowerTransmissionComponent : Component
{
    #region Power
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
    public TimeSpan DepositPeriod = TimeSpan.FromSeconds(20);

    ///<summary>
    /// The next time this power plant is selling accumulated power.
    /// Should not be changedduring runtime, will cause errors in deposit amounts.
    ///</summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDeposit;

    ///<summary>
    /// The total energy accumulated, in joules.
    ///</summary>
    [DataField]
    public float AccumulatedEnergy;

    ///<summary>
    /// The account to deposit funds from sold energy into.
    ///</summary>
    [DataField(required: true)]
    public SectorBankAccount Account = SectorBankAccount.Invalid;
    #endregion Power Sale

    #region Linear Rates
    ///<summary>
    /// The rate per joule to credit the account while in the linear mode.
    ///</summary>
    [DataField]
    public float LinearRate = 0.00003f; // $1/100 kJ

    ///<summary>
    /// The maximum value (inclusive) of the linear mode per deposit, in watts
    ///</summary>
    [DataField]
    public float LinearMaxValue = 1_000_000; // 1 MW ($30/s)
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
    public float LogarithmCoefficient = 30f;

    ///<summary>
    /// The exponential subtrahend of the logarithmic mode: R in Tk*a^(log10(x/T)-R)
    /// Note: should be set to log10(LinearMaxValue) for a continuous function.
    ///</summary>
    [DataField]
    public float LogarithmSubtrahend = 6.0f; // log10(1_000_000)
    #endregion Logarithmic Rates

    ///<summary>
    ///</summary>
    [DataField]
    public float MaxValuePerSecond = 150.0f; // ~57 MW, ~$540k/h

    ///<summary>
    /// True if the entity was powered last tick.
    ///</summary>
    [ViewVariables]
    public bool LastPowered;

    ///<summary>
    /// The minimum requestable power, in watts.
    ///</summary>
    [DataField]
    public float MinimumRequestablePower = 500; // 500 W

    ///<summary>
    /// The maximum requestable power, in watts.
    ///</summary>
    [DataField]
    public float MaximumRequestablePower = 100_000_000_000; // 100 GW
}
