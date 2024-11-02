using Content.Shared._NF.Bank.BUI;
using Content.Shared.Bank;
using Content.Shared.Bank.Components;

namespace Content.Server._NF.Bank;

public sealed partial class BankSystem : SharedBankSystem
{
    public void CleanupLedger()
    {
        if (!TryComp(_sectorService.GetServiceEntity(), out SectorBankComponent? ledger))
            return;
        ledger.AccountLedgerEntries.Clear();
    }

    // Adds an entry to the ledger.
    // Only positive amounts are added.
    public void AddLedgerEntry(SectorBankAccount account, LedgerEntryType entryType, int amount)
    {
        if (amount <= 0)
            return;
        if (!TryComp(_sectorService.GetServiceEntity(), out SectorBankComponent? ledger))
            return;

        var tuple = (account, entryType);
        if (ledger.AccountLedgerEntries.ContainsKey(tuple))
            ledger.AccountLedgerEntries[tuple] += amount;
        else
            ledger.AccountLedgerEntries[tuple] = amount;
        RaiseLocalEvent(new SectorLedgerUpdatedEvent());
    }
}

public sealed class SectorLedgerUpdatedEvent : EntityEventArgs;
