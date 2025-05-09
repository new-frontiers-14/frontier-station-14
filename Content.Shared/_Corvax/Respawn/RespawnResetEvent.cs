using Robust.Shared.Serialization;

namespace Content.Shared._Corvax.Respawn;

[Serializable, NetSerializable]
public sealed class RespawnResetEvent(TimeSpan? time) : EntityEventArgs
{
    public readonly TimeSpan? Time = time;
}
