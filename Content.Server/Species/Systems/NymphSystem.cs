using Content.Server.Cargo.Components;
using Content.Server.Mind;
using Content.Shared.Bank.Components;
using Content.Shared.Species.Components;
using Content.Shared.Body.Events;
using Content.Shared.Zombies;
using Content.Server.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Species.Systems;

public sealed partial class NymphSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager= default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NymphComponent, OrganRemovedFromBodyEvent>(OnRemovedFromPart);
    }

    private void OnRemovedFromPart(EntityUid uid, NymphComponent comp, ref OrganRemovedFromBodyEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (TerminatingOrDeleted(uid) || TerminatingOrDeleted(args.OldBody))
            return;

        if (!_protoManager.TryIndex<EntityPrototype>(comp.EntityPrototype, out var entityProto))
            return;

        // Get the organs' position & spawn a nymph there
        var coords = Transform(uid).Coordinates;
        var nymph = EntityManager.SpawnAtPosition(entityProto.ID, coords);

        if (HasComp<ZombieComponent>(args.OldBody)) // Zombify the new nymph if old one is a zombie
            _zombie.ZombifyEntity(nymph);

        if (comp.TransferMind == true && _mindSystem.TryGetMind(args.OldBody, out var mindId, out var mind))
        {
            // Move the mind if there is one and it's supposed to be transferred
            _mindSystem.TransferTo(mindId, nymph, mind: mind);


            // Frontier
            EnsureComp<CargoSellBlacklistComponent>(nymph);

            // Frontier
            // bank account transfer
            if (TryComp<BankAccountComponent>(args.OldBody, out var bank))
            {
                // Do this carefully since changing the value of a bank account component on a entity will save the balance immediately through subscribers.
                var oldBankBalance = bank.Balance;
                var newBank = EnsureComp<BankAccountComponent>(nymph);
                newBank.Balance = oldBankBalance;
            }
        }

        // Delete the old organ
        QueueDel(uid);
    }
}
