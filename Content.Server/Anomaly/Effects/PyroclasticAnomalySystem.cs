﻿using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Robust.Shared.Map;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="PyroclasticAnomalyComponent"/> and the events from <seealso cref="AnomalySystem"/>
/// </summary>
public sealed class PyroclasticAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PyroclasticAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<PyroclasticAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnPulse(EntityUid uid, PyroclasticAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var xform = Transform(uid);
        var ignitionRadius = component.MaximumIgnitionRadius * args.Stability;
        IgniteNearby(uid, xform.Coordinates, args.Severity, ignitionRadius);
    }

    private void OnSupercritical(EntityUid uid, PyroclasticAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(uid);
        IgniteNearby(uid, xform.Coordinates, 1, component.MaximumIgnitionRadius * 2);
    }

    public void IgniteNearby(EntityUid uid, EntityCoordinates coordinates, float severity, float radius)
    {
        foreach (var flammable in _lookup.GetComponentsInRange<FlammableComponent>(coordinates, radius))
        {
            var ent = flammable.Owner;
            var stackAmount = 1 + (int) (severity / 0.15f);
            _flammable.AdjustFireStacks(ent, stackAmount, flammable);
            _flammable.Ignite(ent, uid, flammable);
        }
    }
}
