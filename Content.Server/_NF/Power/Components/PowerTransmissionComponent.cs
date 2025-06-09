using Content.Shared._NF.Bank.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._NF.Power.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PowerTransmissionComponent : Component
{
    ///<summary>
    /// The period between depositing money into a sector account.
    /// Also the T in Tk*a^log10(x/T) for rate calculation
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
    /// The total energy accumulated, in watts.
    ///</summary>
    [DataField]
    public float AccumulatedEnergy;

    ///<summary>
    /// The account to deposit funds from sold energy into.
    ///</summary>
    [DataField(required: true)]
    public SectorBankAccount Account = SectorBankAccount.Invalid;

    ///<summary>
    /// The rate per joule to credit the account while in the linear mode.
    ///</summary>
    [DataField]
    public float LinearRate = 0.00005f; // $1/20 kJ

    ///<summary>
    /// The maximum value (inclusive) of the linear mode per deposit, in joules
    ///</summary>
    [DataField]
    public float LinearMaxValue = 20000000; // 20 MJ (1 MW * 20 s) ($50/s)

    // Logarithmic fields: at very high levels of power generation, incremental gains decrease logarithmically to prevent runaway

    ///<summary>
    /// The base on power the logarithmic mode (a in Tk*a^log10(x/T))
    ///</summary>
    [DataField]
    public float LogarithmRateBase = 3.0f;

    ///<summary>
    /// The coefficient of the logarithmic mode (k in Tk*a^log10(x/T))
    /// Note: should be set to LinearRate*LinearMaxValue*log10(LinearMaxValue) for a continuous function.
    ///</summary>
    [DataField]
    public float LogarithmCoefficient = 8.33333f;
}
