using System.Diagnostics.CodeAnalysis;
using Content.Server._NF.CartridgeLoader.Cartridges;
using Content.Server._NF.SectorServices;
using Content.Server.CartridgeLoader;
using Content.Shared._NF.Bank.BUI;
using Content.Shared.Bank.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.GameTicking;

namespace Content.Server._NF.Bank;

// System for ledger cartridges - pushes updates to PDA UI when ledger is updated.
public sealed class SectorLedgerSystem : EntitySystem
{
    [Dependency] private SectorServiceSystem _sectorService = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
    }

    public void OnCleanup(RoundRestartCleanupEvent _)
    {
        if (!TryComp<SectorLedgerComponent>(_sectorService.GetServiceEntity(), out var ledger))
            return;
        ledger.AccountLedgerEntries.Clear();
    }

    // Adds an entry to the ledger.
    // Only positive amounts are added.
    public void AddLedgerEntry(SectorBankAccount account, LedgerEntryType entryType, int amount)
    {
        if (amount <= 0)
            return;
        if (!TryComp<SectorLedgerComponent>(_sectorService.GetServiceEntity(), out var ledger))
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
