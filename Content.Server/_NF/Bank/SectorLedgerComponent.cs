using Content.Shared._NF.Bank.BUI;
using Content.Shared.Bank.Components;

namespace Content.Server._NF.Bank;

/// <summary>
/// Tracks all mail statistics for mail activity in the sector.
/// </summary>
[RegisterComponent, Access(typeof(SectorLedgerSystem))]
public sealed partial class SectorLedgerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<(SectorBankAccount Account, LedgerEntryType Type), int> AccountLedgerEntries { get; set; } = new();
}


