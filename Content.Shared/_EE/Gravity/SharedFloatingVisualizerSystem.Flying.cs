using Content.Shared._EE.Flight.Events;

namespace Content.Shared.Gravity;

/// <summary>
/// Handles flying event handlers.
/// </summary>
public abstract partial class SharedFloatingVisualizerSystem : EntitySystem
{
    private void OnFlight(FlightEvent args)
    {
        var uid = GetEntity(args.Uid);
        if (!TryComp<FloatingVisualsComponent>(uid, out var floating))
            return;

        floating.CanFloat = args.IsFlying;

        if (!args.IsFlying || !args.IsAnimated)
            return;

        FloatAnimation(uid, floating.Offset, floating.AnimationKey, floating.AnimationTime);
    }
}