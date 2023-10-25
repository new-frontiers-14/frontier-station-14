using System.Numerics;
using Content.Client.IoC;
using Content.Client.Resources;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.HealthOverlay.UI
{
    public sealed class HealthOverlayGui : BoxContainer
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        public HealthOverlayGui(EntityUid entity)
        {
            IoCManager.InjectDependencies(this);
            UserInterfaceManager.WindowRoot.AddChild(this);
            SeparationOverride = 0;
            Orientation = LayoutOrientation.Vertical;

            CritBar = new HealthOverlayBar
            {
                Visible = false,
                VerticalAlignment = VAlignment.Center,
                Color = Color.Red
            };

            HealthBar = new HealthOverlayBar
            {
                Visible = false,
                VerticalAlignment = VAlignment.Center,
                Color = Color.LimeGreen
            };

            AddChild(Panel = new PanelContainer
            {
                Children =
                {
                    new TextureRect
                    {
                        Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/Misc/health_bar.rsi/icon.png"),
                        TextureScale = Vector2.One * HealthOverlayBar.HealthBarScale,
                        VerticalAlignment = VAlignment.Center,
                    },
                    CritBar,
                    HealthBar
                }
            });

            Entity = entity;
        }

        public PanelContainer Panel { get; }

        public HealthOverlayBar HealthBar { get; }

        public HealthOverlayBar CritBar { get; }

        public EntityUid Entity { get; }

        public void SetVisibility(bool val)
        {
            Visible = val;
            Panel.Visible = val;
        }

        private void MoreFrameUpdate(FrameEventArgs args)
        {
            if (_entities.Deleted(Entity))
            {
                return;
            }

            if (!_entities.TryGetComponent(Entity, out MobStateComponent? mobState) ||
                !_entities.TryGetComponent(Entity, out DamageableComponent? damageable))
            {
                CritBar.Visible = false;
                HealthBar.Visible = false;
                return;
            }

            var mobStateSystem = _entities.EntitySysManager.GetEntitySystem<MobStateSystem>();
            var mobThresholdSystem = _entities.EntitySysManager.GetEntitySystem<MobThresholdSystem>();
            if (mobStateSystem.IsAlive(mobState.Owner, mobState))
            {
                if (!mobThresholdSystem.TryGetThresholdForState(Entity,MobState.Critical, out var threshold))
                {
                    CritBar.Visible = false;
                    HealthBar.Visible = false;
                    return;
                }

                CritBar.Ratio = 1;
                CritBar.Visible = true;
                HealthBar.Ratio = 1 - ((FixedPoint2)(damageable.TotalDamage / threshold)).Float();
                HealthBar.Visible = true;
            }
            else if (mobStateSystem.IsCritical(mobState.Owner, mobState))
            {
                HealthBar.Ratio = 0;
                HealthBar.Visible = false;

                if (!mobThresholdSystem.TryGetThresholdForState(Entity, MobState.Critical, out var critThreshold) ||
                    !mobThresholdSystem.TryGetThresholdForState(Entity, MobState.Dead, out var deadThreshold))
                {
                    CritBar.Visible = false;
                    return;
                }

                CritBar.Visible = true;
                CritBar.Ratio = 1 -
                    ((damageable.TotalDamage - critThreshold) /
                    (deadThreshold - critThreshold)).Value.Float();
            }
            else if (mobStateSystem.IsDead(mobState.Owner, mobState))
            {
                CritBar.Ratio = 0;
                CritBar.Visible = false;
                HealthBar.Ratio = 0;
                HealthBar.Visible = true;
            }
            else
            {
                CritBar.Visible = false;
                HealthBar.Visible = false;
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            MoreFrameUpdate(args);

            if (_entities.Deleted(Entity) || _eyeManager.CurrentMap != _entities.GetComponent<TransformComponent>(Entity).MapID)
            {
                Visible = false;
                return;
            }

            Visible = true;

            var screenCoordinates = _eyeManager.CoordinatesToScreen(_entities.GetComponent<TransformComponent>(Entity).Coordinates);
            var playerPosition = UserInterfaceManager.ScreenToUIPosition(screenCoordinates);
            LayoutContainer.SetPosition(this, new Vector2(playerPosition.X - Width / 2, playerPosition.Y - Height - 30.0f));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            HealthBar.Dispose();
        }
    }
}
