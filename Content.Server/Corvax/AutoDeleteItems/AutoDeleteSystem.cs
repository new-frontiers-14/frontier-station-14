using Content.Server.Construction.Completions;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Corvax.AutoDeleteItems;

public sealed class AutoDeleteSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<AutoDeleteComponent>();
        while (query.MoveNext(out var uid, out var autoDeleteComponent))
        {
            var xformQuery = GetEntityQuery<TransformComponent>();

            if (!xformQuery.TryGetComponent(uid, out var xform) ||
                        xform.MapUid == null)
            {
                return;
            }
            var humanoids = new HashSet<Entity<HumanoidAppearanceComponent>>();

            if (autoDeleteComponent.NextTimeToCheck > _gameTiming.CurTime)
                return;
            _lookup.GetEntitiesInRange(xform.Coordinates, autoDeleteComponent.DistanceToCheck, humanoids);

            if (humanoids.Count > 0)
                autoDeleteComponent.IsHumanoidNear = true;
            else
                autoDeleteComponent.IsHumanoidNear = false;

            if (autoDeleteComponent.IsHumanoidNear == false && autoDeleteComponent.ReadyToDelete == true && autoDeleteComponent.NextTimeToDelete < _gameTiming.CurTime)
            {
                EntityManager.DeleteEntity(uid);
            }

            if (autoDeleteComponent.IsHumanoidNear == true)
            {
                if (autoDeleteComponent.ReadyToDelete == true)
                    autoDeleteComponent.ReadyToDelete = false;

                autoDeleteComponent.NextTimeToDelete = _gameTiming.CurTime + autoDeleteComponent.DelayToDelete;
                autoDeleteComponent.NextTimeToCheck = _gameTiming.CurTime + autoDeleteComponent.DelayToCheck;
            }

            if (autoDeleteComponent.IsHumanoidNear == false && autoDeleteComponent.ReadyToDelete == false)
            {
                autoDeleteComponent.NextTimeToDelete = _gameTiming.CurTime + autoDeleteComponent.DelayToDelete;
                autoDeleteComponent.NextTimeToCheck = _gameTiming.CurTime + autoDeleteComponent.DelayToCheck;
                autoDeleteComponent.ReadyToDelete = true;
            }

            //autoDeleteComponent.NextTimeToDelete = _gameTiming.CurTime + autoDeleteComponent.DelayToDelete;
            //autoDeleteComponent.NextTimeToCheck = _gameTiming.CurTime + autoDeleteComponent.DelayToCheck;
        }
    }
}
