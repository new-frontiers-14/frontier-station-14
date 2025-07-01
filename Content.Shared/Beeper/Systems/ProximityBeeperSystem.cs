using Content.Shared.Beeper.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Pinpointer;
using Content.Shared.ProximityDetection;
using Content.Shared.ProximityDetection.Components;
using Content.Shared.ProximityDetection.Systems;

namespace Content.Shared.Beeper.Systems;

/// <summary>
/// This handles controlling a beeper from proximity detector events.
/// </summary>
public sealed class ProximityBeeperSystem : EntitySystem
{
    [Dependency] private readonly BeeperSystem _beeper = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProximityBeeperComponent, NewProximityTargetEvent>(OnNewProximityTarget);
        SubscribeLocalEvent<ProximityBeeperComponent, ProximityTargetUpdatedEvent>(OnProximityTargetUpdate);
    }

    private void OnProximityTargetUpdate(EntityUid owner, ProximityBeeperComponent proxBeeper, ref ProximityTargetUpdatedEvent args)
    {
        if (!TryComp<BeeperComponent>(owner, out var beeper))
            return;

        // Frontier: minimum range for beeper
        if (args.Distance <= proxBeeper.MinRange)
            _beeper.SetIntervalScaling(owner, 0, beeper);
        else
            _beeper.SetIntervalScaling(owner, (args.Distance - proxBeeper.MinRange) / (args.Detector.Comp.Range - proxBeeper.MinRange), beeper);
        // End Frontier
    }

    private void OnNewProximityTarget(EntityUid owner, ProximityBeeperComponent proxBeeper, ref NewProximityTargetEvent args)
    {
        _beeper.SetMute(owner, args.Target == null);
    }
}
