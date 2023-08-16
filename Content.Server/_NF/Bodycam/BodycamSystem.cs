using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared._NF.Bodycam;
using Content.Shared.Rounding;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._NF.Bodycam.EntitySystems
{
    [UsedImplicitly]
    public sealed class BodycamSystem : SharedBodycamSystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        // TODO: Ideally you'd be able to subscribe to power stuff to get events at certain percentages.. or something?
        // But for now this will be better anyway.
        private readonly HashSet<BodycamComponent> _activeLights = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodycamComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<BodycamComponent, ComponentGetState>(OnGetState);

            SubscribeLocalEvent<BodycamComponent, ExaminedEvent>(OnExamine);

            SubscribeLocalEvent<BodycamComponent, ActivateInWorldEvent>(OnActivate);

            SubscribeLocalEvent<BodycamComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<BodycamComponent, ToggleActionEvent>(OnToggleAction);
        }

        private void OnGetActions(EntityUid uid, BodycamComponent component, GetItemActionsEvent args)
        {
            if (component.ToggleAction == null
                && _proto.TryIndex(component.ToggleActionId, out InstantActionPrototype? act))
            {
                component.ToggleAction = new(act);
            }

            if (component.ToggleAction != null)
                args.Actions.Add(component.ToggleAction);
        }

        private void OnToggleAction(EntityUid uid, BodycamComponent component, ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            if (component.Activated)
                TurnOff(uid, component);
            else
                TurnOn(args.Performer, uid, component);

            args.Handled = true;
        }

        private void OnGetState(EntityUid uid, BodycamComponent component, ref ComponentGetState args)
        {
            //args.State = new BodycamComponent.BodycamComponentState(component.Activated, GetLevel(uid, component));
        }

        private void OnRemove(EntityUid uid, BodycamComponent component, ComponentRemove args)
        {
            _activeLights.Remove(component);
        }

        private void OnActivate(EntityUid uid, BodycamComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            if (ToggleStatus(args.User, uid, component))
                args.Handled = true;
        }

        /// <summary>
        ///     Illuminates the light if it is not active, extinguishes it if it is active.
        /// </summary>
        /// <returns>True if the light's status was toggled, false otherwise.</returns>
        public bool ToggleStatus(EntityUid user, EntityUid uid, BodycamComponent component)
        {
            return component.Activated ? TurnOff(uid, component) : TurnOn(user, uid, component);
        }

        private void OnExamine(EntityUid uid, BodycamComponent component, ExaminedEvent args)
        {
            args.PushMarkup(component.Activated
                ? Loc.GetString("handheld-light-component-on-examine-is-on-message")
                : Loc.GetString("handheld-light-component-on-examine-is-off-message"));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _activeLights.Clear();
        }

        public override void Update(float frameTime)
        {
            var toRemove = new RemQueue<BodycamComponent>();

            foreach (var handheld in _activeLights)
            {
                var uid = handheld.Owner;

                if (handheld.Deleted)
                {
                    toRemove.Add(handheld);
                    continue;
                }

                if (Paused(uid)) continue;
                TryUpdate(uid, handheld, frameTime);
            }

            foreach (var light in toRemove)
            {
                _activeLights.Remove(light);
            }
        }

        public bool TurnOff(EntityUid uid, BodycamComponent component, bool makeNoise = true)
        {
            if (!component.Activated)
            {
                return false;
            }
            return true;
        }

        public bool TurnOn(EntityUid user, EntityUid uid, BodycamComponent component)
        {
            if (component.Activated || !TryComp<PointLightComponent>(uid, out var pointLightComponent))
            {
                return false;
            }

            SetActivated(uid, true, component, true);
            _activeLights.Add(component);

            return true;
        }

        public void TryUpdate(EntityUid uid, BodycamComponent component, float frameTime)
        {
        //    var appearanceComponent = EntityManager.GetComponentOrNull<AppearanceComponent>(uid);
        }
    }
}
