using Content.Client.Movement.Systems;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Shared.Actions;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Client.Ghost
{
    public sealed class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly IClientConsoleHost _console = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly ILightManager _lightManager = default!;
        [Dependency] private readonly ContentEyeSystem _contentEye = default!;
        [Dependency] private readonly IEyeManager _eye = default!;
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Update(float frameTime)
        {
            foreach (var ghost in EntityManager.EntityQuery<GhostComponent, MindComponent>(true))
            {
                var ui = _uiManager.GetActiveUIWidgetOrNull<GhostGui>();
                if (ui != null && Player != null)
                    ui.UpdateRespawn(ghost.Item2.TimeOfDeath);
            }
        }

        public int AvailableGhostRoleCount { get; private set; }

        private bool _ghostVisibility = true;

        private bool GhostVisibility
        {
            get => _ghostVisibility;
            set
            {
                if (_ghostVisibility == value)
                {
                    return;
                }

                _ghostVisibility = value;

                foreach (var ghost in EntityQuery<GhostComponent, SpriteComponent>(true))
                {
                    ghost.Item2.Visible = true;
                }
            }
        }

        public GhostComponent? Player => CompOrNull<GhostComponent>(_playerManager.LocalPlayer?.ControlledEntity);
        public bool IsGhost => Player != null;

        public event Action<GhostComponent>? PlayerRemoved;
        public event Action<GhostComponent>? PlayerUpdated;
        public event Action<GhostComponent>? PlayerAttached;
        public event Action? PlayerDetached;
        public event Action<GhostWarpsResponseEvent>? GhostWarpsResponse;
        public event Action<GhostUpdateGhostRoleCountEvent>? GhostRoleCountUpdated;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<GhostComponent, ComponentRemove>(OnGhostRemove);
            SubscribeLocalEvent<GhostComponent, AfterAutoHandleStateEvent>(OnGhostState);

            SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnGhostPlayerAttach);
            SubscribeLocalEvent<GhostComponent, PlayerDetachedEvent>(OnGhostPlayerDetach);

            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttach);

            SubscribeNetworkEvent<GhostWarpsResponseEvent>(OnGhostWarpsResponse);
            SubscribeNetworkEvent<GhostUpdateGhostRoleCountEvent>(OnUpdateGhostRoleCount);

            SubscribeLocalEvent<GhostComponent, ToggleLightingActionEvent>(OnToggleLighting);
            SubscribeLocalEvent<GhostComponent, ToggleFoVActionEvent>(OnToggleFoV);
            SubscribeLocalEvent<GhostComponent, ToggleGhostsActionEvent>(OnToggleGhosts);
        }

        private void OnStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            if (TryComp(uid, out SpriteComponent? sprite))
                sprite.Visible = GhostVisibility;
        }

        private void OnToggleLighting(EntityUid uid, GhostComponent component, ToggleLightingActionEvent args)
        {
            if (args.Handled)
                return;

            Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-lighting-manager-popup"), args.Performer);
            _lightManager.Enabled = !_lightManager.Enabled;
            args.Handled = true;
        }

        private void OnToggleFoV(EntityUid uid, GhostComponent component, ToggleFoVActionEvent args)
        {
            if (args.Handled)
                return;

            Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-fov-popup"), args.Performer);
            _contentEye.RequestToggleFov(uid);
            args.Handled = true;
        }

        private void OnToggleGhosts(EntityUid uid, GhostComponent component, ToggleGhostsActionEvent args)
        {
            if (args.Handled)
                return;

            Popup.PopupEntity(Loc.GetString("ghost-gui-toggle-ghost-visibility-popup"), args.Performer);
            ToggleGhostVisibility();
            args.Handled = true;
        }

        private void OnGhostRemove(EntityUid uid, GhostComponent component, ComponentRemove args)
        {
            _actions.RemoveAction(uid, component.ToggleLightingActionEntity);
            _actions.RemoveAction(uid, component.ToggleFoVActionEntity);
            _actions.RemoveAction(uid, component.ToggleGhostsActionEntity);
            _actions.RemoveAction(uid, component.ToggleGhostHearingActionEntity);

            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            _lightManager.Enabled = true;

            if (component.IsAttached)
            {
                GhostVisibility = false;
            }

            PlayerRemoved?.Invoke(component);
        }

        private void OnGhostPlayerAttach(EntityUid uid, GhostComponent component, PlayerAttachedEvent playerAttachedEvent)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;
            component.TimeOfDeath = _gameTiming.CurTime;
            GhostVisibility = true;
            component.IsAttached = true;
            PlayerAttached?.Invoke(component);
        }

        private void OnGhostState(EntityUid uid, GhostComponent component, ref AfterAutoHandleStateEvent args)
        {
            if (TryComp<SpriteComponent>(uid, out var sprite))
                sprite.LayerSetColor(0, component.color);

            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return;

            PlayerUpdated?.Invoke(component);
        }

        private bool PlayerDetach(EntityUid uid)
        {
            if (uid != _playerManager.LocalPlayer?.ControlledEntity)
                return false;

            GhostVisibility = false;
            PlayerDetached?.Invoke();
            return true;
        }

        private void OnGhostPlayerDetach(EntityUid uid, GhostComponent component, PlayerDetachedEvent args)
        {
            if (PlayerDetach(uid))
                component.IsAttached = false;
        }

        private void OnPlayerAttach(PlayerAttachedEvent ev)
        {
            if (!HasComp<GhostComponent>(ev.Entity))
                PlayerDetach(ev.Entity);
        }

        private void OnGhostWarpsResponse(GhostWarpsResponseEvent msg)
        {
            if (!IsGhost)
            {
                return;
            }

            GhostWarpsResponse?.Invoke(msg);
        }

        private void OnUpdateGhostRoleCount(GhostUpdateGhostRoleCountEvent msg)
        {
            AvailableGhostRoleCount = msg.AvailableGhostRoles;
            GhostRoleCountUpdated?.Invoke(msg);
        }

        public void RequestWarps()
        {
            RaiseNetworkEvent(new GhostWarpsRequestEvent());
        }

        public void ReturnToBody()
        {
            var msg = new GhostReturnToBodyRequest();
            RaiseNetworkEvent(msg);
        }

        public void OpenGhostRoles()
        {
            _console.RemoteExecuteCommand(null, "ghostroles");
        }

        public void ToggleGhostVisibility()
        {
            _console.RemoteExecuteCommand(null, "toggleghosts");
        }
    }
}
