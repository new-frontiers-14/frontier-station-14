using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Server._NF.SectorServices;
using Content.Shared._NF.Bank.BUI;
using System.Diagnostics.CodeAnalysis;
using Content.Server._NF.Bank;

namespace Content.Server._NF.CartridgeLoader.Cartridges;

// System for ledger cartridges - pushes updates to PDA UI when ledger is updated.
public sealed class NFLedgerCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly SectorServiceSystem _sectorService = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NFLedgerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<SectorLedgerUpdatedEvent>(OnSectorLedgerUpdated);
    }
    private void OnUiReady(Entity<NFLedgerCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        if (GetUIState(out var uiState))
            UpdateUI(args.Loader, uiState);
    }

    private void OnSectorLedgerUpdated(SectorLedgerUpdatedEvent args)
    {
        UpdateAllCartridges();
    }

    private void UpdateAllCartridges()
    {
        var query = EntityQueryEnumerator<NFLedgerCartridgeComponent, CartridgeComponent>();

        if (!GetUIState(out var uiState))
            return;

        while (query.MoveNext(out _, out _, out var cartridge))
        {
            if (cartridge.LoaderUid is not { } loader)
                continue;
            UpdateUI(loader, uiState);
        }
    }

    private bool GetUIState([NotNullWhen(true)] out NFLedgerState? uiState)
    {
        uiState = null;
        if (!TryComp(_sectorService.GetServiceEntity(), out SectorBankComponent? ledger))
            return false;

        var ledgerCount = ledger.AccountLedgerEntries.Count;
        NFLedgerEntry[] entries = new NFLedgerEntry[ledgerCount];
        var index = 0;
        foreach (var ledgerEntry in ledger.AccountLedgerEntries)
        {
            // Bounds check, just to be sure.
            if (index >= ledgerCount)
                break;
            entries[index].Account = ledgerEntry.Key.Account;
            entries[index].Type = ledgerEntry.Key.Type;
            entries[index].Amount = ledgerEntry.Value;
            index++;
        }
        uiState = new NFLedgerState(entries);
        return true;
    }

    private void UpdateUI(EntityUid loader, NFLedgerState state)
    {
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }
}
