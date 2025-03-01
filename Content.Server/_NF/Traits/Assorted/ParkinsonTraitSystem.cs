using Robust.Shared.Random;
using Content.Server.Medical;
using Content.Shared.Jittering;
using Content.Shared.Speech.EntitySystems;

namespace Content.Server._NF.Traits.Assorted;

/// <summary>
/// This handles parkinson, causing the affected to shake uncontrollably at a random interval.
/// </summary>
public sealed class ParkinsonTraitSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] protected readonly IRobustRandom RobustRandom = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ParkinsonTraitComponent, ComponentStartup>(SetupParkinsonTrait);
    }

    private void SetupParkinsonTrait(EntityUid uid, ParkinsonTraitComponent component, ComponentStartup args)
    {
        component.NextIncidentTime =
            _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);
    }

    public void AdjustParkinsonTraitTimer(EntityUid uid, int timerReset, ParkinsonTraitComponent? stinky = null)
    {
        if (!Resolve(uid, ref stinky, false))
            return;

        stinky.NextIncidentTime = timerReset;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ParkinsonTraitComponent>();
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
