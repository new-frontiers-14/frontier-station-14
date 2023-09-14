using Content.Server.Atmos.EntitySystems;
using Content.Server.Light.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Light.EntitySystems
{
    public sealed class MatchstickSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly SharedPointLightSystem _lights = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        private HashSet<MatchstickComponent> _litMatches = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MatchstickComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<MatchstickComponent, IsHotEvent>(OnIsHotEvent);
            SubscribeLocalEvent<MatchstickComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnShutdown(EntityUid uid, MatchstickComponent component, ComponentShutdown args)
        {
            _litMatches.Remove(component);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var match in _litMatches)
            {
                if (match.CurrentState != SmokableState.Lit || Paused(match.Owner) || match.Deleted)
                    continue;

                var xform = Transform(match.Owner);

                if (xform.GridUid is not {} gridUid)
                    return;

                var position = _transformSystem.GetGridOrMapTilePosition(match.Owner, xform);

                _atmosphereSystem.HotspotExpose(gridUid, position, 400, 50, match.Owner, true);
            }
        }

        private void OnInteractUsing(EntityUid uid, MatchstickComponent component, InteractUsingEvent args)
        {
            if (args.Handled || component.CurrentState != SmokableState.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent, false);

            if (!isHotEvent.IsHot)
                return;

            Ignite(uid, component, args.User);
            args.Handled = true;
        }

        private void OnIsHotEvent(EntityUid uid, MatchstickComponent component, IsHotEvent args)
        {
            args.IsHot = component.CurrentState == SmokableState.Lit;
        }

        public void Ignite(EntityUid uid, MatchstickComponent component, EntityUid user)
        {
            // Play Sound
            SoundSystem.Play(component.IgniteSound.GetSound(), Filter.Pvs(component.Owner),
                component.Owner, AudioHelpers.WithVariation(0.125f).WithVolume(-0.125f));

            // Change state
            SetState(uid, component, SmokableState.Lit);
            _litMatches.Add(component);
            component.Owner.SpawnTimer(component.Duration * 1000, delegate
            {
                SetState(uid, component, SmokableState.Burnt);
                _litMatches.Remove(component);
            });
        }

        private void SetState(EntityUid uid, MatchstickComponent component, SmokableState value)
        {
            component.CurrentState = value;

            if (_lights.TryGetLight(uid, out var pointLightComponent))
            {
                _lights.SetEnabled(uid, component.CurrentState == SmokableState.Lit, pointLightComponent);
            }

            if (EntityManager.TryGetComponent(uid, out ItemComponent? item))
            {
                switch (component.CurrentState)
                {
                    case SmokableState.Lit:
                        _item.SetHeldPrefix(uid, "lit", item);
                        break;
                    default:
                        _item.SetHeldPrefix(uid, "unlit", item);
                        break;
                }
            }

            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                _appearance.SetData(uid, SmokingVisuals.Smoking, component.CurrentState, appearance);
            }
        }
    }
}
