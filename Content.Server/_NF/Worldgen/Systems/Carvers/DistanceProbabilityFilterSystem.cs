using System.Numerics;
using Content.Server._NF.Worldgen.Components.Carvers;
using Content.Server.Worldgen.Systems.Debris;
using Robust.Shared.Random;

namespace Content.Server._NF.Worldgen.Systems.Carvers;

/// <summary>
/// This carves out holes in world gen based on distance from a set of known points.
/// </summary>
public sealed class PointSetDistanceCarverSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    // Cache points for lookup

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<PointSetDistanceCarverComponent, PrePlaceDebrisFeatureEvent>(OnPrePlaceDebris);
    }

    private void OnPrePlaceDebris(EntityUid uid, PointSetDistanceCarverComponent component,
        ref PrePlaceDebrisFeatureEvent args)
    {
        // Frontier: something handled this, nothing to do
        if (args.Handled)
            return;
        // End Frontier

        var coords = _transform.ToMapCoordinates(args.Coords);

        var prob = 1.0f;
        var query = EntityQueryEnumerator<WorldGenDistanceCarverComponent, TransformComponent>();
        while (query.MoveNext(out _, out var carver, out var xform))
        {
            var distanceSquared = Vector2.Distance(_transform.ToMapCoordinates(xform.Coordinates).Position, coords.Position);
            if (distanceSquared < carver.MinDistance)
            {
                args.Handled = true;
                return;
            }
            if (distanceSquared < carver.MaxDistance)
                prob = Math.Min(prob, (distanceSquared - carver.MinDistance) / (carver.MaxDistance - carver.MinDistance));
        }

        if (!_random.Prob(prob))
            args.Handled = true;
    }
}

