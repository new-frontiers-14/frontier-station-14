using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.Power.Components;
using Robust.Shared.Random;

namespace Content.Server.Power.EntitySystems;

public sealed class ElectricalOverloadSystem : EntitySystem
{
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectricalOverloadComponent, ApcToggledMainBreakerEvent>(OnApcToggleMainBreaker);
    }

    private void OnApcToggleMainBreaker(EntityUid uid, ElectricalOverloadComponent component, ApcToggledMainBreakerEvent args)
    {
        if (args.Enabled)
        {
            // Toggled on, means Emp!
            component.EmpAt = DateTime.Now + TimeSpan.FromSeconds(_random.NextDouble(25, 35));
            component.NextBuzz = DateTime.Now + TimeSpan.FromSeconds(_random.NextDouble(3, 5));
        }
        else
        {
            // Toggled off, means cancel emp.
            component.EmpAt = DateTime.MaxValue;
            component.NextBuzz = DateTime.MaxValue;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<ElectricalOverloadComponent>();
        while (enumerator.MoveNext(out var entity, out var component))
        {
            if (component.EmpAt > DateTime.Now)
            {
                if (component.NextBuzz > DateTime.Now)
                    continue;

                component.NextBuzz = DateTime.Now + TimeSpan.FromSeconds(_random.NextDouble(7, 15));
                _chatSystem.TrySendInGameICMessage(
                    entity,
                    Loc.GetString("electrical-overload-system-buzz"),
                    InGameICChatType.Emote,
                    hideChat: true,
                    ignoreActionBlocker: true
                );
                continue;
            }

            var coords = _transform.GetMapCoordinates(entity); // Frontier - EMP
            _emp.EmpPulse(coords, component.EmpRange, component.EmpConsumption, component.EmpDuration); // Frontier - EMP
            // if the device survives, we add a bit of randomness to the next emp time
            component.EmpAt = DateTime.Now + TimeSpan.FromSeconds(_random.NextDouble(3, 10));
        }
    }
}
