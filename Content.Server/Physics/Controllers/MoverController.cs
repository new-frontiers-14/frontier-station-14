using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using DroneConsoleComponent = Content.Server.Shuttles.DroneConsoleComponent;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Robust.Shared.Map.Components;

namespace Content.Server.Physics.Controllers
{
    public sealed class MoverController : SharedMoverController
    {
        [Dependency] private readonly ThrusterSystem _thruster = default!;
        [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

        private Dictionary<EntityUid, (ShuttleComponent, HashSet<(EntityUid, PilotComponent, InputMoverComponent, TransformComponent)>)> _shuttlePilots = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RelayInputMoverComponent, PlayerAttachedEvent>(OnRelayPlayerAttached);
            SubscribeLocalEvent<RelayInputMoverComponent, PlayerDetachedEvent>(OnRelayPlayerDetached);
            SubscribeLocalEvent<InputMoverComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<InputMoverComponent, PlayerDetachedEvent>(OnPlayerDetached);
        }

        private void OnRelayPlayerAttached(EntityUid uid, RelayInputMoverComponent component, PlayerAttachedEvent args)
        {
            if (MoverQuery.TryGetComponent(component.RelayEntity, out var inputMover))
                SetMoveInput(inputMover, MoveButtons.None);
        }

        private void OnRelayPlayerDetached(EntityUid uid, RelayInputMoverComponent component, PlayerDetachedEvent args)
        {
            if (MoverQuery.TryGetComponent(component.RelayEntity, out var inputMover))
                SetMoveInput(inputMover, MoveButtons.None);
        }

        private void OnPlayerAttached(EntityUid uid, InputMoverComponent component, PlayerAttachedEvent args)
        {
            SetMoveInput(component, MoveButtons.None);
        }

        private void OnPlayerDetached(EntityUid uid, InputMoverComponent component, PlayerDetachedEvent args)
        {
            SetMoveInput(component, MoveButtons.None);
        }

        protected override bool CanSound()
        {
            return true;
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            // Existing code to update pilots and handle shuttle movement
            UpdatePilots();
            HandleShuttleMovement(frameTime);

            // Existing code to handle mob movement
            var inputQueryEnumerator = AllEntityQuery<InputMoverComponent>();
            while (inputQueryEnumerator.MoveNext(out var uid, out var mover))
            {
                var physicsUid = uid;

                if (RelayQuery.HasComponent(uid))
                    continue;

                if (!XformQuery.TryGetComponent(uid, out var xform))
                {
                    continue;
                }

                PhysicsComponent? body;
                var xformMover = xform;

                if (mover.ToParent && RelayQuery.HasComponent(xform.ParentUid))
                {
                    if (!PhysicsQuery.TryGetComponent(xform.ParentUid, out body) ||
                        !XformQuery.TryGetComponent(xform.ParentUid, out xformMover))
                    {
                        continue;
                    }

                    physicsUid = xform.ParentUid;
                }
                else if (!PhysicsQuery.TryGetComponent(uid, out body))
                {
                    continue;
                }

                HandleMobMovement(uid, mover, physicsUid, body, xformMover, frameTime);
            }
        }


        public (Vector2 Strafe, float Rotation, float Brakes) GetPilotVelocityInput(PilotComponent component)
        {
            if (!Timing.InSimulation)
            {
                // Outside of simulation we'll be running client predicted movement per-frame.
                // So return a full-length vector as if it's a full tick.
                // Physics system will have the correct time step anyways.
                ResetSubtick(component);
                ApplyTick(component, 1f);
                return (component.CurTickStrafeMovement, component.CurTickRotationMovement, component.CurTickBraking);
            }

            float remainingFraction;

            if (Timing.CurTick > component.LastInputTick)
            {
                component.CurTickStrafeMovement = Vector2.Zero;
                component.CurTickRotationMovement = 0f;
                component.CurTickBraking = 0f;
                remainingFraction = 1;
            }
            else
            {
                remainingFraction = (ushort.MaxValue - component.LastInputSubTick) / (float) ushort.MaxValue;
            }

            ApplyTick(component, remainingFraction);

            // Logger.Info($"{curDir}{walk}{sprint}");
            return (component.CurTickStrafeMovement, component.CurTickRotationMovement, component.CurTickBraking);
        }

        private void ResetSubtick(PilotComponent component)
        {
            if (Timing.CurTick <= component.LastInputTick) return;

            component.CurTickStrafeMovement = Vector2.Zero;
            component.CurTickRotationMovement = 0f;
            component.CurTickBraking = 0f;
            component.LastInputTick = Timing.CurTick;
            component.LastInputSubTick = 0;
        }
        
        // Corvax start
        private void UpdatePilots()
        {
            var activePilotQuery = EntityQueryEnumerator<PilotComponent, InputMoverComponent>();
            var shuttleQuery = GetEntityQuery<ShuttleComponent>();
            var newPilots = new Dictionary<EntityUid, (ShuttleComponent Shuttle, HashSet<(EntityUid PilotUid, PilotComponent Pilot, InputMoverComponent Mover, TransformComponent ConsoleXform)>)>();

            while (activePilotQuery.MoveNext(out var uid, out var pilot, out var mover))
            {
                if (!IsPilotActive(pilot)) continue;

                var consoleEnt = pilot.Console;
                if (!TryComp<TransformComponent>(consoleEnt, out var xform)) continue;

                var gridId = xform.GridUid;
                if (!shuttleQuery.TryGetComponent(gridId, out var shuttleComponent) || !shuttleComponent.Enabled) continue;

                if (!newPilots.TryGetValue(gridId!.Value, out var pilots))
                    pilots = (shuttleComponent, new HashSet<(EntityUid, PilotComponent, InputMoverComponent, TransformComponent)>());

                pilots.Item2.Add((uid, pilot, mover, xform));
                newPilots[gridId.Value] = pilots;
            }

            _shuttlePilots = newPilots;
        }

        private bool IsPilotActive(PilotComponent pilot)
        {
            // Determine if the pilot is currently active based on input.
            return pilot.HeldButtons != ShuttleButtons.None;
        }
        // Corvax end
        
        protected override void HandleShuttleInput(EntityUid uid, ShuttleButtons button, ushort subTick, bool state)
        {
            if (!TryComp<PilotComponent>(uid, out var pilot) || pilot.Console == null)
                return;

            ResetSubtick(pilot);

            if (subTick >= pilot.LastInputSubTick)
            {
                var fraction = (subTick - pilot.LastInputSubTick) / (float) ushort.MaxValue;

                ApplyTick(pilot, fraction);
                pilot.LastInputSubTick = subTick;
            }

            var buttons = pilot.HeldButtons;

            if (state)
            {
                buttons |= button;
            }
            else
            {
                buttons &= ~button;
            }

            pilot.HeldButtons = buttons;
        }

        private static void ApplyTick(PilotComponent component, float fraction)
        {
            var x = 0;
            var y = 0;
            var rot = 0;
            int brake;

            if ((component.HeldButtons & ShuttleButtons.StrafeLeft) != 0x0)
            {
                x -= 1;
            }

            if ((component.HeldButtons & ShuttleButtons.StrafeRight) != 0x0)
            {
                x += 1;
            }

            component.CurTickStrafeMovement.X += x * fraction;

            if ((component.HeldButtons & ShuttleButtons.StrafeUp) != 0x0)
            {
                y += 1;
            }

            if ((component.HeldButtons & ShuttleButtons.StrafeDown) != 0x0)
            {
                y -= 1;
            }

            component.CurTickStrafeMovement.Y += y * fraction;

            if ((component.HeldButtons & ShuttleButtons.RotateLeft) != 0x0)
            {
                rot -= 1;
            }

            if ((component.HeldButtons & ShuttleButtons.RotateRight) != 0x0)
            {
                rot += 1;
            }

            component.CurTickRotationMovement += rot * fraction;

            if ((component.HeldButtons & ShuttleButtons.Brake) != 0x0)
            {
                brake = 1;
            }
            else
            {
                brake = 0;
            }

            component.CurTickBraking += brake * fraction;
        }

        /// <summary>
        /// Helper function to extrapolate max velocity for a given Vector2 (really, its angle) and shuttle.
        /// </summary>
        private Vector2 ObtainMaxVel(Vector2 vel, ShuttleComponent shuttle)
        {
            // Use LengthSquared to avoid unnecessary square root computation
            if (vel.LengthSquared() == 0f)
                return Vector2.Zero;

            // Directly extract velocity components
            float velX = vel.X;
            float velY = vel.Y;

            // Simplified index calculation based on the direction of velocity components
            int horizIndex = velX > 0 ? 1 : 3;
            int vertIndex = velY > 0 ? 2 : 0;

            // Calculate thrust factors only if the respective velocity component is non-zero
            float horizThrustRatio = velX != 0 ? shuttle.BaseLinearThrust[horizIndex] / shuttle.LinearThrust[horizIndex] : 0;
            float vertThrustRatio = velY != 0 ? shuttle.BaseLinearThrust[vertIndex] / shuttle.LinearThrust[vertIndex] : 0;

            // Calculate normalized component values
            float horizNormalized = velX * horizThrustRatio;
            float vertNormalized = velY * vertThrustRatio;

            // Calculate the normalization factor using the squared sum of the normalized components
            float normalizationFactor = 1f / MathF.Sqrt(horizNormalized * horizNormalized + vertNormalized * vertNormalized);

            // Apply the normalization factor to the entire vector and scale by maximum velocity
            return new Vector2(velX * normalizationFactor, velY * normalizationFactor) * shuttle.BaseMaxLinearVelocity;
        }

        private void HandleShuttleMovement(float frameTime)
        {
            var directions = new[] { DirectionFlag.South, DirectionFlag.East, DirectionFlag.North, DirectionFlag.West };
            var xformQuery = GetEntityQuery<TransformComponent>();

            foreach (var (shuttleUid, (shuttle, pilots)) in _shuttlePilots)
            {
                if (Paused(shuttleUid) || CanPilot(shuttleUid) || !TryComp<PhysicsComponent>(shuttleUid, out var body))
                    continue;

                var shuttleNorthAngle = _xformSystem.GetWorldRotation(shuttleUid, xformQuery);
                var forceMul = frameTime * body.InvMass;

                // Collate movement linear and angular inputs together
                var linearInput = Vector2.Zero;
                var brakeInput = 0f;
                var angularInput = 0f;

                foreach (var (pilotUid, pilot, _, consoleXform) in pilots)
                {
                    var (strafe, rotation, brakes) = GetPilotVelocityInput(pilot);

                    if (brakes > 0f)
                    {
                        brakeInput += brakes;
                    }

                    if (strafe.LengthSquared() > 0f) // Use LengthSquared to avoid sqrt calculation
                    {
                        var offsetRotation = consoleXform.LocalRotation;
                        linearInput += offsetRotation.RotateVec(strafe);
                    }

                    if (rotation != 0f)
                    {
                        angularInput += rotation;
                    }
                }

                // Normalize pilot inputs by the number of pilots
                int pilotCount = pilots.Count;
                linearInput /= pilotCount;
                angularInput /= pilotCount;
                brakeInput /= pilotCount;

                // Handle shuttle movement
                if (brakeInput > 0f)
                {
                    if (body.LinearVelocity.Length() > 0f)
                    {
                        // Minimum brake velocity for a direction to show its thrust appearance.
                        const float appearanceThreshold = 0.1f;

                        // Get velocity relative to the shuttle so we know which thrusters to fire
                        var shuttleVelocity = (-shuttleNorthAngle).RotateVec(body.LinearVelocity);
                        var force = Vector2.Zero;

                        if (shuttleVelocity.X < 0f)
                        {
                            _thruster.DisableLinearThrustDirection(shuttle, DirectionFlag.West);

                            if (shuttleVelocity.X < -appearanceThreshold)
                                _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.East);

                            var index = (int) Math.Log2((int) DirectionFlag.East);
                            force.X += shuttle.LinearThrust[index];
                        }
                        else if (shuttleVelocity.X > 0f)
                        {
                            _thruster.DisableLinearThrustDirection(shuttle, DirectionFlag.East);

                            if (shuttleVelocity.X > appearanceThreshold)
                                _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.West);

                            var index = (int) Math.Log2((int) DirectionFlag.West);
                            force.X -= shuttle.LinearThrust[index];
                        }

                        if (shuttleVelocity.Y < 0f)
                        {
                            _thruster.DisableLinearThrustDirection(shuttle, DirectionFlag.South);

                            if (shuttleVelocity.Y < -appearanceThreshold)
                                _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.North);

                            var index = (int) Math.Log2((int) DirectionFlag.North);
                            force.Y += shuttle.LinearThrust[index];
                        }
                        else if (shuttleVelocity.Y > 0f)
                        {
                            _thruster.DisableLinearThrustDirection(shuttle, DirectionFlag.North);

                            if (shuttleVelocity.Y > appearanceThreshold)
                                _thruster.EnableLinearThrustDirection(shuttle, DirectionFlag.South);

                            var index = (int) Math.Log2((int) DirectionFlag.South);
                            force.Y -= shuttle.LinearThrust[index];
                        }

                        var impulse = force * brakeInput * ShuttleComponent.BrakeCoefficient;
                        impulse = shuttleNorthAngle.RotateVec(impulse);
                        var forceMul = frameTime * body.InvMass;
                        var maxVelocity = (-body.LinearVelocity).Length() / forceMul;

                        // Don't overshoot
                        if (impulse.Length() > maxVelocity)
                            impulse = impulse.Normalized() * maxVelocity;

                        PhysicsSystem.ApplyForce(shuttleUid, impulse, body: body);
                    }
                    else
                    {
                        _thruster.DisableLinearThrusters(shuttle);
                    }

                    if (body.AngularVelocity != 0f)
                    {
                        var torque = shuttle.AngularThrust * brakeInput * (body.AngularVelocity > 0f ? -1f : 1f) * ShuttleComponent.BrakeCoefficient;
                        var torqueMul = body.InvI * frameTime;

                        if (body.AngularVelocity > 0f)
                        {
                            torque = MathF.Max(-body.AngularVelocity / torqueMul, torque);
                        }
                        else
                        {
                            torque = MathF.Min(-body.AngularVelocity / torqueMul, torque);
                        }

                        if (!torque.Equals(0f))
                        {
                            PhysicsSystem.ApplyTorque(shuttleUid, torque, body: body);
                            _thruster.SetAngularThrust(shuttle, true);
                        }
                    }
                    else
                    {
                        _thruster.SetAngularThrust(shuttle, false);
                    }
                }

                if (linearInput.LengthSquared() > 0f)
                {
                    PhysicsSystem.SetSleepingAllowed(shuttleUid, body, false);
                    var angle = linearInput.ToWorldAngle();
                    var linearDir = angle.GetDir();
                    var dockFlag = linearDir.AsFlag();

                    var totalForce = Vector2.Zero;
                    foreach (var dir in directions)
                    {
                        if ((dir & dockFlag) == 0x0)
                        {
                            _thruster.DisableLinearThrustDirection(shuttle, dir);
                            continue;
                        }

                        int index = Array.IndexOf(directions, dir);
                        var thrust = shuttle.LinearThrust[index];
                        var force = dir switch
                        {
                            DirectionFlag.North => new Vector2(0, thrust),
                            DirectionFlag.South => new Vector2(0, -thrust),
                            DirectionFlag.East => new Vector2(thrust, 0),
                            DirectionFlag.West => new Vector2(-thrust, 0),
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        _thruster.EnableLinearThrustDirection(shuttle, dir);
                        totalForce += force * linearInput.Length();
                    }

                    var forceMul = frameTime * body.InvMass;

                    var localVel = (-shuttleNorthAngle).RotateVec(body.LinearVelocity);
                    var maxVelocity = ObtainMaxVel(localVel, shuttle); // max for current travel dir
                    var maxWishVelocity = ObtainMaxVel(totalForce, shuttle);
                    var properAccel = (maxWishVelocity - localVel) / forceMul;

                    var finalForce = Vector2Dot(totalForce, properAccel.Normalized()) * properAccel.Normalized();

                    if (localVel.Length() >= maxVelocity.Length() && Vector2.Dot(totalForce, localVel) > 0f)
                        finalForce = Vector2.Zero; // burn would be faster if used as such

                    if (finalForce.Length() > properAccel.Length())
                        finalForce = properAccel; // don't overshoot

                    //Log.Info($"shuttle: maxVelocity {maxVelocity} totalForce {totalForce} finalForce {finalForce} forceMul {forceMul} properAccel {properAccel}");

                    finalForce = shuttleNorthAngle.RotateVec(finalForce);

                    if (finalForce.Length() > 0f)
                        PhysicsSystem.ApplyForce(shuttleUid, finalForce, body: body);
                }

                if (MathHelper.CloseTo(angularInput, 0f))
                {
                    PhysicsSystem.SetSleepingAllowed(shuttleUid, body, true);

                    if (brakeInput <= 0f)
                        _thruster.SetAngularThrust(shuttle, false);
                }
                else
                if (!MathHelper.CloseTo(angularInput, 0f))
                {
                    PhysicsSystem.SetSleepingAllowed(shuttleUid, body, false);
                    var torque = shuttle.AngularThrust * -angularInput;
                    var torqueMul = body.InvI * frameTime;
                    torque = Math.Clamp(torque,
                        (-ShuttleComponent.MaxAngularVelocity - body.AngularVelocity) / torqueMul,
                        (ShuttleComponent.MaxAngularVelocity - body.AngularVelocity) / torqueMul);

                    if (!torque.Equals(0f))
                    {
                        PhysicsSystem.ApplyTorque(shuttleUid, torque, body: body);
                        _thruster.SetAngularThrust(shuttle, true);
                    }
                }
                else
                {
                    _thruster.SetAngularThrust(shuttle, false);
                }
            }
        }

        // .NET 8 seem to miscompile usage of Vector2.Dot above. This manual outline fixes it pending an upstream fix.
        // See PR #24008
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Vector2Dot(Vector2 value1, Vector2 value2)
        {
            return Vector2.Dot(value1, value2);
        }

        private bool CanPilot(EntityUid shuttleUid)
        {
            return TryComp<FTLComponent>(shuttleUid, out var ftl)
            && (ftl.State & (FTLState.Starting | FTLState.Travelling | FTLState.Arriving)) != 0x0
                || HasComp<PreventPilotComponent>(shuttleUid);
        }

    }
}
