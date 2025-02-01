using Content.Shared._Corvax.Respawn;

namespace Content.Client._Corvax.Respawn;

public sealed class RespawnSystem : EntitySystem
{
    public TimeSpan? RespawnResetTime { get; private set; }

    public event Action? RespawnReseted;

    public override void Initialize()
    {
        SubscribeNetworkEvent<RespawnResetEvent>(OnRespawnReset);
    }

    private void OnRespawnReset(RespawnResetEvent e)
    {
        RespawnResetTime = e.Time;

        RespawnReseted?.Invoke();
    }
}
