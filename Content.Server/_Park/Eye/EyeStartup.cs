using Content.Shared.Ghost;
using Content.Server.Ghost.Components;
using Content.Server._Park.Species.Shadowkin.Systems;
using Content.Server.Visible;
using Robust.Server.GameObjects;

namespace Content.Server._Park.Eye
{
    /// <summary>
    ///     Place to handle eye component startup for whatever systems.
    /// </summary>
    public sealed class EyeStartup : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly ShadowkinDarkSwapSystem _shadowkinPowerSystem = default!;

        [Dependency] private readonly SharedEyeSystem _eye = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EyeComponent, ComponentStartup>(OnEyeStartup);
        }

        private void OnEyeStartup(EntityUid uid, EyeComponent component, ComponentStartup args)
        {
            if (_entityManager.HasComponent<GhostComponent>(uid))
                _eye.SetVisibilityMask(uid, component.VisibilityMask | (int) (VisibilityFlags.AIEye), component);
                // component.VisibilityMask |= (int) VisibilityFlags.AIEye;

            _shadowkinPowerSystem.SetVisibility(uid, _entityManager.HasComponent<GhostComponent>(uid));
        }
    }
}
