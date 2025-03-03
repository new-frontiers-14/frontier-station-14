using Robust.Shared.Random;
using Content.Server.Medical;
using Content.Shared.Jittering;
using Content.Shared.Speech.EntitySystems;

namespace Content.Server._NF.Traits.Assorted;

/// <summary>
/// This handles the trait, causing the affected to shake uncontrollably at a random interval.
/// </summary>
public sealed class TremorsTraitSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] protected readonly IRobustRandom RobustRandom = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TremorsTraitComponent, ComponentStartup>(SetupTremorsTrait);
    }

    private void SetupTremorsTrait(EntityUid uid, TremorsTraitComponent component, ComponentStartup args)
    {
        component.NextIncidentTime =
            _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);
    }

    public void AdjustTremorsTraitTimer(EntityUid uid, int timerReset, TremorsTraitComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        comp.NextIncidentTime = timerReset;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TremorsTraitComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            component.NextIncidentTime -= frameTime;

            if (component.NextIncidentTime >= 0)
                continue;

            // Set the new time.
            component.NextIncidentTime +=
                _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);

            _stuttering.DoStutter(uid, TimeSpan.FromSeconds(component.IncidentLength), false); // Gives stuttering
            _jittering.DoJitter(uid, TimeSpan.FromSeconds(component.IncidentLength), true, component.IncidentAmplitude, component.IncidentFrequency, true, null);
        }
    }
}
