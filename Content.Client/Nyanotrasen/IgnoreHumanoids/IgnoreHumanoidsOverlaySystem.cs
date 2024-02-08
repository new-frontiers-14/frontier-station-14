using Content.Shared.IgnoreHumanoids;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.IgnoreHumanoids
{
    public sealed class IgnoreHumanoidsOverlaySystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IOverlayManager _overlayMan = default!;

        private IgnoreHumanoidsOverlay _overlay = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<IgnoreHumanoidsOverlayComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<IgnoreHumanoidsOverlayComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<IgnoreHumanoidsOverlayComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<IgnoreHumanoidsOverlayComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

            _overlay = new(EntityManager, _protoMan);
        }

        private void OnInit(EntityUid uid, IgnoreHumanoidsOverlayComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _overlayMan.AddOverlay(_overlay);
            }


        }
        private void OnRemove(EntityUid uid, IgnoreHumanoidsOverlayComponent component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _overlay.Reset();
                _overlayMan.RemoveOverlay(_overlay);
            }
        }

        private void OnPlayerAttached(EntityUid uid, IgnoreHumanoidsOverlayComponent component, LocalPlayerAttachedEvent args)
        {
            _overlayMan.AddOverlay(_overlay);
        }

        private void OnPlayerDetached(EntityUid uid, IgnoreHumanoidsOverlayComponent component, LocalPlayerDetachedEvent args)
        {
            _overlay.Reset();
            _overlayMan.RemoveOverlay(_overlay);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            _overlay.Reset();
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
