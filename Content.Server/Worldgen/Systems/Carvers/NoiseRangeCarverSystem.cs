using Content.Server.Worldgen.Components.Carvers;
using Content.Server.Worldgen.Systems.Debris;

namespace Content.Server.Worldgen.Systems.Carvers;

/// <summary>
///     This handles carving out holes in world generation according to a noise channel.
/// </summary>
public sealed class NoiseRangeCarverSystem : EntitySystem
{
    [Dependency] private readonly NoiseIndexSystem _index = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<NoiseRangeCarverComponent, PrePlaceDebrisFeatureEvent>(OnPrePlaceDebris);
    }

    private void OnPrePlaceDebris(EntityUid uid, NoiseRangeCarverComponent component,
        ref PrePlaceDebrisFeatureEvent args)
    {
        // Frontier: something handled this, nothing to do
        if (args.Handled)
            return;
        // End Frontier

        var coords = WorldGen.WorldToChunkCoords(args.Coords.ToMapPos(EntityManager, _transform));
        var val = _index.Evaluate(uid, component.NoiseChannel, coords);

        foreach (var (low, high) in component.Ranges)
        {
            if (low > val || high < val)
                continue;

            args.Handled = true;
            return;
        }
    }
}

