using Content.Server.Solar.Components;
using Content.Server.UserInterface;
using Content.Shared.Solar;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Server._NF.Solar.Components; // Frontier
using Content.Server._NF.Solar.EntitySystems; // Frontier

namespace Content.Server.Solar.EntitySystems
{
    /// <summary>
    /// Responsible for updating solar control consoles.
    /// </summary>
    [UsedImplicitly]
    internal sealed class PowerSolarControlConsoleSystem : EntitySystem
    {
        [Dependency] private readonly NFPowerSolarSystem _powerSolarSystem = default!; // Frontier: use NF variant.
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        /// <summary>
        /// Timer used to avoid updating the UI state every frame (which would be overkill)
        /// </summary>
        private float _updateTimer;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolarControlConsoleComponent, SolarControlConsoleAdjustMessage>(OnUIMessage);
        }

        public override void Update(float frameTime)
        {
            _updateTimer += frameTime;
            if (_updateTimer >= 1)
            {
                _updateTimer -= 1;
                // Frontier: per-grid state
                // var state = new SolarControlConsoleBoundInterfaceState(_powerSolarSystem.TargetPanelRotation, _powerSolarSystem.TargetPanelVelocity, _powerSolarSystem.TotalPanelPower, _powerSolarSystem.TowardsSun);
                var query = EntityQueryEnumerator<SolarControlConsoleComponent, UserInterfaceComponent, TransformComponent>();
                while (query.MoveNext(out var uid, out _, out var uiComp, out var xform))
                {
                    SolarControlConsoleBoundInterfaceState state;
                    if (xform.GridUid != null && TryComp<SolarPoweredGridComponent>(xform.GridUid, out var gridPower))
                        state = new SolarControlConsoleBoundInterfaceState(gridPower.TargetPanelRotation, gridPower.TargetPanelVelocity, gridPower.TotalPanelPower, _powerSolarSystem.TowardsSun);
                    else
                        state = new SolarControlConsoleBoundInterfaceState(0, 0, 0, _powerSolarSystem.TowardsSun);

                    _uiSystem.SetUiState((uid, uiComp), SolarControlConsoleUiKey.Key, state);
                }
                // End Frontier: per-grid state
            }
        }

        private void OnUIMessage(EntityUid uid, SolarControlConsoleComponent component, SolarControlConsoleAdjustMessage msg)
        {
            // Frontier: ensure we have a powered grid
            if (!TryComp(uid, out TransformComponent? xform)
                || xform.GridUid == null
                || !TryComp(xform.GridUid, out SolarPoweredGridComponent? powerComp))
            {
                return;
            }
            // End Frontier

            if (double.IsFinite(msg.Rotation))
            {
                powerComp.TargetPanelRotation = msg.Rotation.Reduced(); // Frontier: _powerSolarSystem<powerComp
            }
            if (double.IsFinite(msg.AngularVelocity))
            {
                var degrees = msg.AngularVelocity.Degrees;
                degrees = Math.Clamp(degrees, -PowerSolarSystem.MaxPanelVelocityDegrees, PowerSolarSystem.MaxPanelVelocityDegrees);
                powerComp.TargetPanelVelocity = Angle.FromDegrees(degrees); // Frontier: _powerSolarSystem<powerComp
            }
        }

    }
}
