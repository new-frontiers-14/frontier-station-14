using Content.Shared._NF.Bank.BUI;
using Content.Shared._NF.Bank.Components;

namespace Content.Server._NF.Bank;

/// <summary>
/// Tracks accounts of entities (e.g. Frontier Station, the NFSD)
/// </summary>
[RegisterComponent, Access(typeof(BankSystem))]
public sealed partial class SectorBankComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public Dictionary<SectorBankAccount, SectorBankAccountInfo> Accounts = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public float SecondsSinceLastIncrease = 0.0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<(SectorBankAccount Account, LedgerEntryType Type), int> AccountLedgerEntries { get; set; } = new();
}

[DataDefinition]
public sealed partial class SectorBankAccountInfo
{
    /// <summary>
    /// The current balance of the account, in spesos.
    /// </summary>
    [DataField]
    public int Balance;
    /// <summary>
    /// How much the account increases per second.
    /// </summary>
    [DataField]
    public int IncreasePerSecond;
}
