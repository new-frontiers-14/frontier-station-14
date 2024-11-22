using Content.Shared.Station.Components;
using Robust.Shared.Map.Components;

namespace Content.Shared.Station.Systems;

public sealed class StationSystem : SharedStationSystem
{

    /// <summary>
    /// Gets the station that "owns" the given entity (essentially, the station the grid it's on is attached to)
    /// </summary>
    /// <param name="entity">Entity to find the owner of.</param>
    /// <param name="xform">Resolve pattern, transform of the entity.</param>
    /// <returns>The owning station, if any.</returns>
    /// <remarks>
    /// This does not remember what station an entity started on, it simply checks where it is currently located.
    /// </remarks>
    public EntityUid? GetOwningStation(EntityUid entity, TransformComponent? xform = null)
    {
        if (!Resolve(entity, ref xform))
            throw new ArgumentException("Tried to use an abstract entity!", nameof(entity));

        if (TryComp<StationDataComponent>(entity, out _))
        {
            // We are the station, just return ourselves.
            return entity;
        }

        if (TryComp<MapGridComponent>(entity, out _))
        {
            // We are the station, just check ourselves.
            return CompOrNull<StationMemberComponent>(entity)?.Station;
        }

        if (xform.GridUid == EntityUid.Invalid)
        {
            Log.Debug("Unable to get owning station - GridUid invalid.");
            return null;
        }

        return CompOrNull<StationMemberComponent>(xform.GridUid)?.Station;
    }
}
