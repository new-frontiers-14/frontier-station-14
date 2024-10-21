using Content.Shared._NF.Emp.Components;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._NF.Emp.Systems;

public sealed class EmpBlastSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpBlastComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, EmpBlastComponent component, ComponentStartup args)
    {
        component.StartTime = _timing.RealTime;

        // try to get despawn time or keep default duration time
        if (TryComp<TimedDespawnComponent>(uid, out var despawn))
        {
            component.VisualDuration = despawn.Lifetime;
        }
    }
}
