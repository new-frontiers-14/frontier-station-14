using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Robust.Shared.Random;

namespace Content.Server.Power.EntitySystems;

public sealed class ElectricalOverloadSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectricalOverloadComponent, ApcToggledMainBreakerEvent>(OnApcToggleMainBreaker);
    }

    private void OnApcToggleMainBreaker(EntityUid uid, ElectricalOverloadComponent component, ApcToggledMainBreakerEvent args)
    {
        if (args.Enabled)
        {
            // Toggled on, means explode!
            component.ExplodeAt = DateTime.Now + TimeSpan.FromSeconds(_random.NextDouble(25, 35));
            component.NextBuzz = DateTime.Now + TimeSpan.FromSeconds(_random.NextDouble(3, 5));
        }
        else
        {
            // Toggled off, means cancel explosion.
            component.ExplodeAt = DateTime.MaxValue;
            component.NextBuzz = DateTime.MaxValue;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<ElectricalOverloadComponent>();
        while (enumerator.MoveNext(out var entity, out var component))
        {
            if (component.ExplodeAt > DateTime.Now)
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

            _explosionSystem.QueueExplosion(entity, component.ExplosionOnOverload, 2f, 0.5f, 2f, 1f, int.MaxValue, false, null, true);
            // if the device survives, we add a bit of randomness to the next explosion time
            component.ExplodeAt = DateTime.Now + TimeSpan.FromSeconds(_random.NextDouble(3, 10));
        }
    }
}
