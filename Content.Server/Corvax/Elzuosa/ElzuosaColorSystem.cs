using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Content.Server.GameTicking;
namespace Content.Server.Corvax.Elzuosa
{
    public sealed class ElzuosaColorSystem : EntitySystem
    {
        [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ElzuosaColorComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        }

        private void OnPlayerSpawn(EntityUid uid, ElzuosaColorComponent comp, PlayerSpawnCompleteEvent args)
        {
            if (!HasComp<HumanoidAppearanceComponent>(uid))
                return;
            if (args == null)
                return;
            var profile = args.Profile;
            SetEntityPointLightColor(uid, profile);
        }

        public void SetEntityPointLightColor(EntityUid uid, HumanoidCharacterProfile? profile)
        {
            if (profile == null)
                return;

            var color = profile.Appearance.SkinColor;
            _pointLightSystem.SetColor(uid,color);

        }
    }
}
