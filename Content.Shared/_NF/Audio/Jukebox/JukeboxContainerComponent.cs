using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Audio.Jukebox;

// contains a list of JukeboxPrototypes which represent the contents of the container
[RegisterComponent]
public sealed partial class JukeboxContainerComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<JukeboxPrototype>> Tracks = new();
}