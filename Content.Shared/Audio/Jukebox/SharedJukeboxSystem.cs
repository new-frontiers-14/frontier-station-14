using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers; // Frontier

namespace Content.Shared.Audio.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly IPrototypeManager _protoManager = default!; // wizden#42210

    // wizden#42210
    public IEnumerable<JukeboxPrototype> GetAvailableTracks(Entity<JukeboxComponent> entity)
    {
        // Frontier: Music Discs
        if (!TryComp<ContainerManagerComponent>(entity.Owner, out var containers))
            return [];

        HashSet<JukeboxPrototype> availableMusic = new();

        foreach (var container in containers.Containers.Values)
        {
            foreach (var ent in container.ContainedEntities)
            {
                if (!TryComp(ent, out JukeboxContainerComponent? tracklist))
                    continue;

                foreach (var trackID in tracklist.Tracks)
                {
                    if (_protoManager.TryIndex<JukeboxPrototype>(trackID, out var track))
                        availableMusic.Add(track);
                }
            }
        }
        // End Frontier: Music Discs
        return availableMusic; // Frontier _protoManager.EnumeratePrototypes<JukeboxPrototype>()<availableMusic
    }
    // End wizden#42210
}