using Content.Server._NF.Market.Components;
using Content.Server.Bank;
using Content.Shared._NF.Market.Components;
using Content.Shared._NF.Market.Events;
using Content.Shared.Bank.Components;
using Content.Shared.Placeable;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Market.Systems;

public sealed partial class MarketSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly BankSystem _bankSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private void InitializeCrateMachine()
    {
        SubscribeLocalEvent<CrateMachineComponent, CrateMachinePurchaseMessage>(OnPurchaseCrateMessage);
    }

    private void OnPurchaseCrateMessage(EntityUid uid, SharedCrateMachineComponent component, CrateMachinePurchaseMessage args)
    {
        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;

        if (!TryComp<BankAccountComponent>(player, out var bankAccount))
            return;

        TrySpawnCrate(uid, player, component);
    }

    public void TrySpawnCrate(EntityUid uid, EntityUid player, SharedCrateMachineComponent component)
    {
        var placer = Comp<ItemPlacerComponent>(uid);
        if (placer.PlacedEntities.Count > 0)
        {
            return;
        }

        // Withdraw spesos from player
        //_bankSystem.TryBankWithdraw(player, );

        var xform = Transform(uid);
        Spawn(component.CratePrototype, xform.Coordinates);

        UpdateVisualState(uid, component);
        Dirty(uid, component);
    }

    public void UpdateVisualState(EntityUid uid, SharedCrateMachineComponent component)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
        {
            return;
        }

        if (!Transform(uid).Anchored)
        {
            _appearanceSystem.SetData(uid, SharedCrateMachineComponent.CrateMachineVisuals.VisualState, SharedCrateMachineComponent.CrateMachineVisualState.Opening, appearance);
            return;
        }
    }

}
