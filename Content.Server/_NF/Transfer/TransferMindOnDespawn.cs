using Content.Shared.Mind;
using Robust.Shared.Spawners;
using Robust.Shared.Prototypes;
using Content.Server._NF.Transfer.Components;

namespace Content.Server._NF.Transfer;

/// <summary>
/// Meant to be used along "TimedDespawn" component to transfer the player mind
/// after the animation for a smooth transition between entities
/// </summary>
public sealed class TransferMindOnDespawnSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager= default!;

    ///Subscribe to the despawn event
    public override void Initialize()
    {
        SubscribeLocalEvent<TransferMindOnDespawnComponent, TimedDespawnEvent>(OnDespawnTransfer);
    }

    private void OnDespawnTransfer(EntityUid uid, TransferMindOnDespawnComponent component, TimedDespawnEvent args)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        if (!_protoManager.TryIndex<EntityPrototype>(component.EntityPrototype, out var entityProto))
            return;

        ///Spawn new entity on the same place where the animation ends and transfer the mind to the new entity
        var coords = Transform(uid).Coordinates;
        var dragon = EntityManager.SpawnAtPosition(entityProto.ID, coords);

        _mindSystem.TransferTo(mindId, dragon, mind: mind);
    }
}
