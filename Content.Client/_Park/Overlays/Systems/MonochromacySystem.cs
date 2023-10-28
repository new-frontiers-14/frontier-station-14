using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Network;
using Content.Shared._Park.Traits;

namespace Content.Client._Park.Overlays;
public sealed class MonochromacySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private MonochromacyOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonochromacyComponent, ComponentStartup>(OnMonochromacyStartup);
        SubscribeLocalEvent<MonochromacyComponent, ComponentShutdown>(OnMonochromacyShutdown);

        SubscribeLocalEvent<MonochromacyComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MonochromacyComponent, PlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnMonochromacyStartup(EntityUid uid, MonochromacyComponent component, ComponentStartup args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnMonochromacyShutdown(EntityUid uid, MonochromacyComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    private void OnPlayerAttached(EntityUid uid, MonochromacyComponent component, PlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, MonochromacyComponent component, PlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}
