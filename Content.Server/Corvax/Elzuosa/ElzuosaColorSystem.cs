using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Content.Server.GameTicking;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;

namespace Content.Server.Corvax.Elzuosa
{
    public sealed class ElzuosaColorSystem : EntitySystem
    {
        [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
        [Dependency] private readonly SharedRgbLightControllerSystem _rgbSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly StaminaSystem _stamina = default!;

        public bool SelfEmagged;

        public StaminaComponent? StaminaComponent;
        public HungerComponent? HungerComponent;
        private PointLightComponent? _pointLightComponent;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ElzuosaColorComponent, GotEmaggedEvent>(OnEmagged);
            SubscribeLocalEvent<ElzuosaColorComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var query = EntityQueryEnumerator<ElzuosaColorComponent>();
            while (query.MoveNext(out var uid, out var elzuosaColorComponent))
            {

                var hunger = EntityManager.EnsureComponent<HungerComponent>(uid).CurrentHunger;
                if (TryComp(uid, out _pointLightComponent))
                    //Да простит меня боженька...
                    if(hunger <= 50)
                        _pointLightSystem.SetRadius(uid,(float)0.5);
                    else if(hunger <= 55)
                        _pointLightSystem.SetRadius(uid,(float)1.05);
                    else if(hunger <= 60)
                        _pointLightSystem.SetRadius(uid,(float)1.1);
                    else if(hunger <= 65)
                        _pointLightSystem.SetRadius(uid,(float)1.2);
                    else if(hunger <= 70)
                        _pointLightSystem.SetRadius(uid,(float)1.4);
                    else if(hunger <= 75)
                        _pointLightSystem.SetRadius(uid,(float)1.6);
                    else if(hunger <= 80)
                        _pointLightSystem.SetRadius(uid,(float)1.8);
                    else if(hunger <= 85)
                        _pointLightSystem.SetRadius(uid,(float)2);
                    else if(hunger > 90)
                        _pointLightSystem.SetRadius(uid,(float)2.3);

                if (elzuosaColorComponent.StannedByEmp)
                {
                    _stamina.TakeStaminaDamage(uid,120,StaminaComponent);
                    elzuosaColorComponent.StannedByEmp = false;
                }
            }
        }

        private void OnEmagged(EntityUid uid, ElzuosaColorComponent comp, ref GotEmaggedEvent args)
        {
            if ((args.UserUid == uid))
                SelfEmagged = true;
            else
                SelfEmagged = false;

            comp.Hacked = !comp.Hacked;



            if (SelfEmagged)
            {
                if (comp.Hacked)
                {
                    _popupSystem.PopupEntity(Loc.GetString("elzuosa-selfemag-success"),uid);
                    var rgb = EnsureComp<RgbLightControllerComponent>(uid);
                    _rgbSystem.SetCycleRate(uid, comp.CycleRate, rgb);
                }
                else
                {
                    _popupSystem.PopupEntity(Loc.GetString("elzuosa-selfdeemag-success"),uid);
                    RemComp<RgbLightControllerComponent>(uid);
                }
            }
            else
            {
                if (comp.Hacked)
                {
                    _popupSystem.PopupEntity(Loc.GetString("elzuosa-emag-success",("target", Identity.Entity(uid, EntityManager))),uid,
                        args.UserUid);
                    _popupSystem.PopupEntity(Loc.GetString("elzuosa-emagged-success",("user", Identity.Entity(args.UserUid, EntityManager))),args.UserUid,
                        uid);
                    var rgb = EnsureComp<RgbLightControllerComponent>(uid);
                    _rgbSystem.SetCycleRate(uid, comp.CycleRate, rgb);
                }
                else
                {
                    _popupSystem.PopupEntity(Loc.GetString("elzuosa-deemag-success",("target", Identity.Entity(uid, EntityManager))),uid,
                        args.UserUid);
                    _popupSystem.PopupEntity(Loc.GetString("elzuosa-deemagged-success",("user", Identity.Entity(args.UserUid, EntityManager))),args.UserUid,
                        uid);
                    RemComp<RgbLightControllerComponent>(uid);
                }
            }
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
