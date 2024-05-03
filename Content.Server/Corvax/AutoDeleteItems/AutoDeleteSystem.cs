using Content.Server.Construction.Completions;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Content.Shared.SSDIndicator;

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

            if (autoDeleteComponent.NextTimeToCheck > _gameTiming.CurTime)
                return;

            SSDIndicatorComponent? ssdComponent = null;
            //if (!TryComp<SSDIndicatorComponent>(xform., out var ssd))
            //    continue;
            foreach (var iterator in _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(xform.Coordinates, autoDeleteComponent.DistanceToCheck))
            {
                if (TryComp(iterator, out ssdComponent) && ssdComponent.IsSSD == true)
                {
                    autoDeleteComponent.IsSSDNear = true;
                }
                else if(TryComp(iterator, out ssdComponent) && ssdComponent.IsSSD == false)
                {
                    autoDeleteComponent.IsSSDNear = false;
                }
                    
                if (iterator.Owner == uid)
                    continue;

                var humanoids = new HashSet<Entity<HumanoidAppearanceComponent>>();

                _lookup.GetEntitiesInRange(xform.Coordinates, autoDeleteComponent.DistanceToCheck, humanoids);

                if (humanoids.Count > 0 && !autoDeleteComponent.IsSSDNear)
                    autoDeleteComponent.IsHumanoidNear = true;
                else
                    autoDeleteComponent.IsHumanoidNear = false;

                if (autoDeleteComponent.IsHumanoidNear == false && autoDeleteComponent.ReadyToDelete == true && autoDeleteComponent.NextTimeToDelete < _gameTiming.CurTime)
                {
                    EntityManager.DeleteEntity(uid);
                }
                if (autoDeleteComponent.IsSSDNear == true)
                {
                    autoDeleteComponent.IsHumanoidNear = false;
                }
                if (autoDeleteComponent.IsHumanoidNear == true && autoDeleteComponent.IsSSDNear == false)
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
            }
        }
    }
}
