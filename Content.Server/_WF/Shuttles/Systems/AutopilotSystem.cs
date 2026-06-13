using System.Numerics;
using Content.Server._WF.Shuttles.Components;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Chat.Managers;
using Content.Shared.Movement.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Server._WF.Shuttles.Systems;

/// <summary>
/// Handles automatic navigation of shuttles to target destinations.
/// Uses Reynolds steering behaviors for autonomous character movement.
/// Reference: "Steering Behaviors For Autonomous Characters" by Craig W. Reynolds (1999)
/// </summary>
public sealed class AutopilotSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrusterSystem _thruster = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutopilotComponent, ComponentShutdown>(OnAutopilotShutdown);
        SubscribeLocalEvent<AutopilotServerComponent, AnchorStateChangedEvent>(OnAutopilotServerUnanchored);
    }

    private void OnAutopilotShutdown(EntityUid uid, AutopilotComponent component, ComponentShutdown args)
    {
        component.Enabled = false;
    }

    private void OnAutopilotServerUnanchored(EntityUid uid, AutopilotServerComponent component, ref AnchorStateChangedEvent args)
    {
        // If the server is being unanchored, disable autopilot on the grid it was on
        if (args.Anchored)
        {
            return;
        }

        var gridUid = args.Transform.GridUid;
        if (gridUid == null)
        {
            return;
        }

        // Check if there's an autopilot component on this grid
        if (!TryComp<AutopilotComponent>(gridUid.Value, out var autopilot) || !autopilot.Enabled)
        {
            return;
        }

        DisableAutopilot(gridUid.Value);
        SendShuttleMessage(gridUid.Value, "Autopilot: Server disconnected - autopilot disabled");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AutopilotComponent, ShuttleComponent, TransformComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var autopilot, out var shuttle, out var xform, out var physics))
        {
            if (!autopilot.Enabled || autopilot.TargetCoordinates == null)
            {
                continue;
            }

            if (!shuttle.Enabled)
            {
                autopilot.Enabled = false;
                continue;
            }

            // Check if autopilot server has power
            if (!HasPoweredAutopilotServer(uid))
            {
                DisableAutopilot(uid);
                SendShuttleMessage(uid, "Autopilot: Server lost power - autopilot disabled");
                continue;
            }

            var currentPos = _transform.GetMapCoordinates(uid, xform).Position;
            var targetPos = autopilot.TargetCoordinates.Value.Position;

            // Check if we've arrived
            var toTarget = targetPos - currentPos;
            var distance = toTarget.Length();

            if (distance <= autopilot.ArrivalDistance)
            {
                var destinationName = autopilot.DestinationName ?? "destination";
                autopilot.Enabled = false;
                SendShuttleMessage(uid, $"Autopilot: {destinationName} reached - Parking");

                // Apply brakes
                ApplyBraking(uid, shuttle, physics, xform, frameTime);

                // Park the shuttle by setting it to Anchor mode (same as UI "Park" button)
                const float AnchorDampingStrength = 2.5f;
                shuttle.BodyModifier = AnchorDampingStrength;
                if (shuttle.DampingModifier != 0)
                    shuttle.DampingModifier = shuttle.BodyModifier;

                // Refresh shuttle consoles so pilots see the mode change to "Park"
                _console.RefreshShuttleConsoles(uid);

                continue;
            }

            // Calculate steering force using Reynolds steering behaviors
            var maxSpeed = CalculateMaxSpeed(shuttle, physics.LinearVelocity) * autopilot.SpeedMultiplier;
            var currentVelocity = physics.LinearVelocity;

            // Obstacle avoidance: check for obstacles first (highest priority)
            var (avoidanceForce, threatLevel) = CalculateObstacleAvoidance(uid, xform, physics, autopilot, uid, maxSpeed);

            // Reduce max speed based on threat level (slow down when obstacles are near)
            var effectiveMaxSpeed = maxSpeed * (1f - threatLevel * 0.7f);

            // Arrival behavior: seek with slowdown near target
            var arrivalForce = CalculateArrivalSteering(currentPos, targetPos, currentVelocity, effectiveMaxSpeed, autopilot.SlowdownDistance);

            // Combine steering forces with priority-based blending
            Vector2 steeringForce;
            if (threatLevel > 0.8f)
            {
                // Critical threat: pure avoidance, ignore destination
                steeringForce = avoidanceForce;
            }
            else if (threatLevel > 0.01f)
            {
                // Blend based on threat level - higher threat = more avoidance
                var avoidanceWeight = threatLevel;
                var arrivalWeight = 1f - threatLevel;

                // Project arrival force to remove component toward obstacle
                // This prevents arrival from fighting against avoidance
                if (avoidanceForce.LengthSquared() > 0.01f)
                {
                    var avoidDir = Vector2.Normalize(avoidanceForce);
                    var arrivalDotAvoid = Vector2.Dot(arrivalForce, avoidDir);
                    if (arrivalDotAvoid < 0)
                    {
                        // Arrival is pushing toward obstacle - remove that component
                        arrivalForce -= avoidDir * arrivalDotAvoid;
                    }
                }

                steeringForce = arrivalForce * arrivalWeight + avoidanceForce * avoidanceWeight;
            }
            else
            {
                // No threat: pure arrival
                steeringForce = arrivalForce;
            }

            // Truncate steering force to max force
            var maxForce = maxSpeed * 0.5f;
            if (steeringForce.LengthSquared() > maxForce * maxForce)
            {
                steeringForce = Vector2.Normalize(steeringForce) * maxForce;
            }

            // Convert steering force to local space and apply thrust
            var localSteering = (-xform.LocalRotation).RotateVec(steeringForce);
            ApplyAutopilotThrust(uid, shuttle, localSteering, xform, physics, threatLevel, frameTime);

            // Handle rotation to face direction of travel (or target if moving slowly)
            var facingDirection = currentVelocity.LengthSquared() > 1f ? Vector2.Normalize(currentVelocity) : (distance > 0.01f ? toTarget / distance : Vector2.UnitY);
            RotateTowardsTarget(uid, shuttle, xform, physics, facingDirection, frameTime);
        }
    }

    /// <summary>
    /// Calculate the maximum speed for the shuttle considering thruster upgrades.
    /// Uses the same formula as MoverController.ObtainMaxVel to account for upgraded thrusters.
    /// </summary>
    private float CalculateMaxSpeed(ShuttleComponent shuttle, Vector2 velocity)
    {
        if (velocity.LengthSquared() < 0.01f)
            return shuttle.BaseMaxLinearVelocity;

        var vel = Vector2.Normalize(velocity);

        var horizIndex = vel.X > 0 ? 1 : 3; // east else west
        var vertIndex = vel.Y > 0 ? 2 : 0; // north else south

        // Calculate the velocity scaling based on thrust ratios
        // This accounts for upgraded thrusters having more thrust than base
        var horizComp = vel.X != 0 && shuttle.LinearThrust[horizIndex] > 0
            ? MathF.Pow(Vector2.Dot(vel, new Vector2(shuttle.BaseLinearThrust[horizIndex] / shuttle.LinearThrust[horizIndex], 0f)), 2)
            : 0;
        var vertComp = vel.Y != 0 && shuttle.LinearThrust[vertIndex] > 0
            ? MathF.Pow(Vector2.Dot(vel, new Vector2(0f, shuttle.BaseLinearThrust[vertIndex] / shuttle.LinearThrust[vertIndex])), 2)
            : 0;

        if (horizComp + vertComp < 0.0001f)
            return shuttle.BaseMaxLinearVelocity;

        return shuttle.BaseMaxLinearVelocity * vel.Length() * MathF.ReciprocalSqrtEstimate(horizComp + vertComp);
    }

    /// <summary>
    /// Calculate arrival steering: seek behavior with slowdown as we approach target.
    /// Reference: Reynolds "Arrival" behavior
    /// </summary>
    private Vector2 CalculateArrivalSteering(Vector2 position, Vector2 target, Vector2 currentVelocity, float maxSpeed, float slowingDistance)
    {
        var targetOffset = target - position;
        var distance = targetOffset.Length();

        if (distance < 0.01f)
            return -currentVelocity; // Just brake if we're at the target

        // Ramped speed: full speed when far, slowing down when near
        var rampedSpeed = maxSpeed * (distance / slowingDistance);
        var clippedSpeed = MathF.Min(rampedSpeed, maxSpeed);

        // Desired velocity points toward target at the calculated speed
        var desiredVelocity = (targetOffset / distance) * clippedSpeed;

        // Steering = desired velocity - current velocity
        return desiredVelocity - currentVelocity;
    }

    /// <summary>
    /// Calculate obstacle avoidance steering using Reynolds cylinder-based approach.
    /// Projects a cylinder ahead based on current velocity and steers away from obstacles.
    /// Takes into account actual ship sizes from their bounding boxes.
    /// Returns both the avoidance force and a threat level (0-1) indicating urgency.
    /// </summary>
    private (Vector2 avoidanceForce, float threatLevel) CalculateObstacleAvoidance(EntityUid uid, TransformComponent xform, PhysicsComponent physics, AutopilotComponent autopilot, EntityUid shuttleUid, float maxSpeed)
    {
        var pos = _transform.GetMapCoordinates(uid, xform);
        var velocity = physics.LinearVelocity;
        var speed = velocity.Length();

        // If not moving much, no obstacle avoidance needed
        if (speed < 0.5f)
            return (Vector2.Zero, 0f);

        var forward = velocity / speed;

        // Look-ahead distance scales with speed: full scan range at max speed, shorter when slower
        // This way slow-moving ships don't overreact to distant obstacles
        var speedRatio = Math.Clamp(speed / maxSpeed, 0.1f, 1f);
        var lookAheadDistance = autopilot.ScanRange * speedRatio;

        // Get our ship's bounding radius from the grid's AABB
        var ourRadius = 10f; // Default fallback
        if (TryComp<MapGridComponent>(uid, out var ourGrid))
        {
            var ourAABB = ourGrid.LocalAABB;
            // Use half the diagonal as the bounding radius (conservative estimate)
            ourRadius = MathF.Max(ourAABB.Width, ourAABB.Height) / 2f;
        }

        // Find all grids in scan range
        var grids = new HashSet<Entity<MapGridComponent>>();
        _lookup.GetEntitiesInRange(pos.MapId, pos.Position, autopilot.ScanRange, grids);

        Vector2? mostThreateningAvoidance = null;
        var nearestIntersection = float.MaxValue;
        var highestThreatLevel = 0f;

        // Debug: report scan status periodically
        if (autopilot.DebugObstacles && !autopilot.ReportedObstacles.Contains(EntityUid.Invalid))
        {
            autopilot.ReportedObstacles.Add(EntityUid.Invalid); // Use Invalid as a "we've reported scan status" marker
            SendShuttleMessage(shuttleUid, $"Autopilot: Scanning... speed={speed:F1}, lookAhead={lookAheadDistance:F0}m, ourRadius={ourRadius:F0}m, gridsInRange={grids.Count}");
        }

        foreach (var grid in grids)
        {
            var gridUid = grid.Owner;

            if (gridUid == uid) // Don't avoid ourselves
                continue;

            if (!TryComp<PhysicsComponent>(gridUid, out _))
                continue;

            var obstacleGrid = grid.Comp;
            var gridXform = Transform(gridUid);
            var gridPos = _transform.GetMapCoordinates(gridUid, gridXform);

            // Get obstacle's bounding radius from its AABB
            var obstacleAABB = obstacleGrid.LocalAABB;
            var obstacleRadius = MathF.Max(obstacleAABB.Width, obstacleAABB.Height) / 2f;

            // Vector from us to obstacle
            var toObstacle = gridPos.Position - pos.Position;
            var distanceToObstacle = toObstacle.Length();

            if (distanceToObstacle < 0.01f)
                continue;

            // Combined radii: our bounding radius + obstacle's bounding radius + safety margin
            var safetyMargin = 10f;
            var combinedRadius = ourRadius + obstacleRadius + safetyMargin;

            // Localize obstacle position: project onto forward axis
            var forwardComponent = Vector2.Dot(toObstacle, forward);

            // Obstacle behind us? Skip it (but allow slightly behind for safety)
            if (forwardComponent < -combinedRadius * 0.5f)
                continue;

            // Obstacle too far ahead? Skip it
            if (forwardComponent > lookAheadDistance + combinedRadius)
                continue;

            // Calculate lateral (perpendicular) distance from our path to obstacle center
            var lateralOffset = toObstacle - forward * forwardComponent;
            var lateralDistance = lateralOffset.Length();

            // Debug: Report ANY grid that passes the basic forward/behind checks, before lateral filter
            if (autopilot.DebugObstacles && !autopilot.ReportedObstacles.Contains(gridUid))
            {
                autopilot.ReportedObstacles.Add(gridUid);
                var obstacleName = MetaData(gridUid).EntityName;
                var direction = GetCardinalDirection(toObstacle, forward);
                var inCylinder = lateralDistance < combinedRadius * 1.5f;
                SendShuttleMessage(shuttleUid, $"Autopilot: Grid detected {direction} - {obstacleName} (size: {obstacleRadius * 2:F0}m, dist: {distanceToObstacle:F0}m, lateral: {lateralDistance:F0}m, combined: {combinedRadius:F0}m, inPath: {inCylinder})");
            }

            // Check if obstacle intersects our forward cylinder (with wider detection)
            if (lateralDistance >= combinedRadius * 1.5f)
                continue;

            // Calculate intersection distance (when we'd hit)
            var intersectionDistance = forwardComponent - combinedRadius;

            // Calculate threat level based on how close we are to collision
            // Use exponential scaling for more aggressive response when close
            var normalizedDistance = Math.Clamp(intersectionDistance / lookAheadDistance, 0f, 1f);
            var threatLevel = MathF.Pow(1f - normalizedDistance, 2f); // Exponential urgency

            // Increase threat if we're very close
            if (intersectionDistance < combinedRadius)
            {
                threatLevel = MathF.Min(1f, threatLevel + 0.3f);
            }

            // Track highest threat for speed reduction
            highestThreatLevel = MathF.Max(highestThreatLevel, threatLevel);

            if (intersectionDistance < nearestIntersection)
            {
                nearestIntersection = intersectionDistance;

                // Steering to avoid: push away from obstacle
                // If lateral offset is small (head-on), pick a perpendicular direction
                Vector2 avoidanceDirection;
                if (lateralDistance < combinedRadius * 0.3f)
                {
                    // Near head-on: choose a strong perpendicular direction
                    avoidanceDirection = new Vector2(-forward.Y, forward.X);
                }
                else
                {
                    // Steer away from obstacle center
                    avoidanceDirection = Vector2.Normalize(-lateralOffset);
                }

                // Also add braking component when very close - steer partially backward
                if (intersectionDistance < combinedRadius * 2f && intersectionDistance > 0)
                {
                    var brakeComponent = -forward * (1f - intersectionDistance / (combinedRadius * 2f));
                    avoidanceDirection = Vector2.Normalize(avoidanceDirection + brakeComponent * 0.5f);
                }

                // Strong avoidance force that scales with threat
                var forceMagnitude = threatLevel * speed * 2f;
                mostThreateningAvoidance = avoidanceDirection * forceMagnitude;
            }
        }

        return (mostThreateningAvoidance ?? Vector2.Zero, highestThreatLevel);
    }

    /// <summary>
    /// Gets a human-readable direction string for an obstacle relative to forward direction.
    /// </summary>
    private string GetCardinalDirection(Vector2 toObstacle, Vector2 forward)
    {
        if (toObstacle.LengthSquared() < 0.01f)
            return "nearby";

        var normalized = Vector2.Normalize(toObstacle);
        var forwardDot = Vector2.Dot(normalized, forward);
        var right = new Vector2(forward.Y, -forward.X);
        var rightDot = Vector2.Dot(normalized, right);

        // Determine direction based on dot products
        if (forwardDot > 0.7f)
            return "ahead";
        if (forwardDot < -0.7f)
            return "behind";
        if (rightDot > 0.7f)
            return "to starboard";
        if (rightDot < -0.7f)
            return "to port";
        if (forwardDot > 0 && rightDot > 0)
            return "ahead to starboard";
        if (forwardDot > 0 && rightDot < 0)
            return "ahead to port";
        if (forwardDot < 0 && rightDot > 0)
            return "behind to starboard";
        return "behind to port";
    }

    private void ApplyAutopilotThrust(EntityUid uid, ShuttleComponent shuttle, Vector2 localSteering, TransformComponent xform, PhysicsComponent physics, float threatLevel, float frameTime)
    {
        // Get current velocity in local space
        var currentLocalVelocity = (-xform.LocalRotation).RotateVec(physics.LinearVelocity);
        var currentSpeed = currentLocalVelocity.Length();

        // Calculate target velocity based on threat level
        // Higher threat = lower allowed speed
        var baseMaxVelocity = CalculateMaxSpeed(shuttle, physics.LinearVelocity) * 0.6f;
        var threatSpeedMultiplier = 1f - threatLevel * 0.9f; // At max threat, only 10% speed allowed
        var targetMaxVelocity = baseMaxVelocity * threatSpeedMultiplier;

        var force = Vector2.Zero;
        DirectionFlag directions = DirectionFlag.None;

        // When threat is significant, prioritize braking if we're going too fast
        if (threatLevel > 0.3f && currentSpeed > targetMaxVelocity)
        {
            // We need to slow down! Apply braking thrust opposite to current velocity
            if (currentLocalVelocity.X > 0.5f)
            {
                directions |= DirectionFlag.West;
                var index = (int)Math.Log2((int)DirectionFlag.West);
                force.X -= shuttle.LinearThrust[index];
            }
            else if (currentLocalVelocity.X < -0.5f)
            {
                directions |= DirectionFlag.East;
                var index = (int)Math.Log2((int)DirectionFlag.East);
                force.X += shuttle.LinearThrust[index];
            }

            if (currentLocalVelocity.Y > 0.5f)
            {
                directions |= DirectionFlag.South;
                var index = (int)Math.Log2((int)DirectionFlag.South);
                force.Y -= shuttle.LinearThrust[index];
            }
            else if (currentLocalVelocity.Y < -0.5f)
            {
                directions |= DirectionFlag.North;
                var index = (int)Math.Log2((int)DirectionFlag.North);
                force.Y += shuttle.LinearThrust[index];
            }
        }
        else
        {
            // Normal steering - apply thrust in direction of steering force
            // X-axis (East/West) thrust based on steering demand
            if (localSteering.X > 0.1f && currentLocalVelocity.X < targetMaxVelocity)
            {
                directions |= DirectionFlag.East;
                var index = (int)Math.Log2((int)DirectionFlag.East);
                force.X += shuttle.LinearThrust[index];
            }
            else if (localSteering.X < -0.1f && currentLocalVelocity.X > -targetMaxVelocity)
            {
                directions |= DirectionFlag.West;
                var index = (int)Math.Log2((int)DirectionFlag.West);
                force.X -= shuttle.LinearThrust[index];
            }

            // Y-axis (North/South) thrust based on steering demand
            if (localSteering.Y > 0.1f && currentLocalVelocity.Y < targetMaxVelocity)
            {
                directions |= DirectionFlag.North;
                var index = (int)Math.Log2((int)DirectionFlag.North);
                force.Y += shuttle.LinearThrust[index];
            }
            else if (localSteering.Y < -0.1f && currentLocalVelocity.Y > -targetMaxVelocity)
            {
                directions |= DirectionFlag.South;
                var index = (int)Math.Log2((int)DirectionFlag.South);
                force.Y -= shuttle.LinearThrust[index];
            }
        }

        // Additionally, apply lateral thrust for avoidance even while braking
        if (threatLevel > 0.3f)
        {
            // Add lateral steering component to dodge while braking
            if (localSteering.X > 0.1f && (directions & DirectionFlag.East) == 0 && (directions & DirectionFlag.West) == 0)
            {
                directions |= DirectionFlag.East;
                var index = (int)Math.Log2((int)DirectionFlag.East);
                force.X += shuttle.LinearThrust[index] * 0.5f; // Half thrust for lateral dodge
            }
            else if (localSteering.X < -0.1f && (directions & DirectionFlag.East) == 0 && (directions & DirectionFlag.West) == 0)
            {
                directions |= DirectionFlag.West;
                var index = (int)Math.Log2((int)DirectionFlag.West);
                force.X -= shuttle.LinearThrust[index] * 0.5f;
            }
        }

        // Enable thrusters visually and apply force
        if (directions != DirectionFlag.None)
        {
            _thruster.EnableLinearThrustDirection(shuttle, directions);

            // Apply the force in world coordinates
            var worldForce = xform.LocalRotation.RotateVec(force);
            _physics.ApplyForce(uid, worldForce, body: physics);
        }
        else
        {
            _thruster.DisableLinearThrusters(shuttle);
        }
    }

    private void RotateTowardsTarget(EntityUid uid, ShuttleComponent shuttle, TransformComponent xform, PhysicsComponent physics, Vector2 targetDirection, float frameTime)
    {
        // Calculate desired angle - subtract PI/2 to point front of ship (north) instead of right side (east)
        var currentAngle = xform.LocalRotation.Theta;
        var desiredAngle = MathF.Atan2(targetDirection.Y, targetDirection.X) - MathF.PI / 2f;
        var angleDiff = (float)Angle.ShortestDistance(currentAngle, desiredAngle).Theta;

        var maxAngularVelocity = ShuttleComponent.MaxAngularVelocity;
        var currentAngularVelocity = physics.AngularVelocity;

        // Apply angular damping to reduce oscillation
        if (MathF.Abs(currentAngularVelocity) > 0.01f)
        {
            var dampingTorque = -currentAngularVelocity * shuttle.AngularThrust * 0.6f;
            _physics.ApplyAngularImpulse(uid, dampingTorque * frameTime, body: physics);
        }

        // Dead zone - don't rotate if we're close enough
        if (MathF.Abs(angleDiff) < 0.15f)
        {
            _thruster.SetAngularThrust(shuttle, false);
            return;
        }

        // Proportional control based on angle difference
        var direction = angleDiff > 0 ? 1f : -1f;
        var angleDiffAbs = MathF.Abs(angleDiff);
        var proportionalMultiplier = Math.Clamp(angleDiffAbs / 1.0f, 0.1f, 1.0f);

        // Reduced torque application with proportional scaling
        var torqueMultiplier = 0.25f * proportionalMultiplier;

        // Only apply torque if under max angular velocity
        var shouldApplyTorque = (direction > 0 && currentAngularVelocity < maxAngularVelocity * 0.7f) ||
                               (direction < 0 && currentAngularVelocity > -maxAngularVelocity * 0.7f);

        if (shouldApplyTorque)
        {
            var torque = shuttle.AngularThrust * direction * torqueMultiplier;
            _thruster.SetAngularThrust(shuttle, true);
            _physics.ApplyAngularImpulse(uid, torque * frameTime, body: physics);
        }
        else
        {
            _thruster.SetAngularThrust(shuttle, false);
        }
    }

    private void ApplyBraking(EntityUid uid, ShuttleComponent shuttle, PhysicsComponent physics, TransformComponent xform, float frameTime)
    {
        // Apply braking forces to linear velocity
        var velocity = physics.LinearVelocity;
        if (velocity.LengthSquared() > 0.01f)
        {
            var shuttleVelocity = (-xform.LocalRotation).RotateVec(velocity);
            var force = Vector2.Zero;
            DirectionFlag brakeDirections = DirectionFlag.None;

            if (shuttleVelocity.X < -0.1f)
            {
                brakeDirections |= DirectionFlag.East;
                var index = (int)Math.Log2((int)DirectionFlag.East);
                force.X += shuttle.LinearThrust[index];
            }
            else if (shuttleVelocity.X > 0.1f)
            {
                brakeDirections |= DirectionFlag.West;
                var index = (int)Math.Log2((int)DirectionFlag.West);
                force.X -= shuttle.LinearThrust[index];
            }

            if (shuttleVelocity.Y < -0.1f)
            {
                brakeDirections |= DirectionFlag.North;
                var index = (int)Math.Log2((int)DirectionFlag.North);
                force.Y += shuttle.LinearThrust[index];
            }
            else if (shuttleVelocity.Y > 0.1f)
            {
                brakeDirections |= DirectionFlag.South;
                var index = (int)Math.Log2((int)DirectionFlag.South);
                force.Y -= shuttle.LinearThrust[index];
            }

            if (brakeDirections != DirectionFlag.None)
            {
                _thruster.EnableLinearThrustDirection(shuttle, brakeDirections);

                // Apply braking force
                var impulse = force * ShuttleComponent.BrakeCoefficient;
                impulse = xform.LocalRotation.RotateVec(impulse);
                var forceMul = frameTime * physics.InvMass;
                var maxVelocity = (-velocity).Length() / forceMul;

                // Don't overshoot
                if (impulse.Length() > maxVelocity)
                    impulse = impulse.Normalized() * maxVelocity;

                _physics.ApplyForce(uid, impulse, body: physics);
            }
        }
        else
        {
            _thruster.DisableLinearThrusters(shuttle);
        }

        // Brake angular velocity
        if (MathF.Abs(physics.AngularVelocity) > 0.01f)
        {
            var torque = shuttle.AngularThrust * (physics.AngularVelocity > 0f ? -1f : 1f) * ShuttleComponent.BrakeCoefficient;
            _thruster.SetAngularThrust(shuttle, true);
            _physics.ApplyAngularImpulse(uid, torque * frameTime, body: physics);
        }
        else
        {
            _thruster.SetAngularThrust(shuttle, false);
        }
    }

    /// <summary>
    /// Sends a message to all players on the shuttle.
    /// </summary>
    public void SendShuttleMessage(EntityUid shuttleUid, string message)
    {
        var players = new List<ICommonSession>();

        // Find all players on this shuttle
        var query = EntityQueryEnumerator<TransformComponent, ActorComponent>();
        while (query.MoveNext(out _, out var xform, out var actor))
        {
            if (xform.GridUid == shuttleUid)
            {
                players.Add(actor.PlayerSession);
            }
        }

        // Send message to all players on the shuttle
        foreach (var player in players)
        {
            _chatManager.DispatchServerMessage(player, message);
        }
    }

    /// <summary>
    /// Enable autopilot
    /// </summary>
    public void EnableAutopilot(EntityUid shuttleUid, MapCoordinates targetCoordinates, string? destinationName = null)
    {
        var autopilot = EnsureComp<AutopilotComponent>(shuttleUid);

        if (autopilot.Enabled)
        {
            return;
        }

        autopilot.Enabled = true;
        autopilot.TargetCoordinates = targetCoordinates;
        autopilot.DestinationName = destinationName;
        autopilot.ReportedObstacles.Clear(); // Reset obstacle tracking for new journey

        // Switch to "Drive" mode (Dampen) - release any parking brake or anchor
        if (TryComp<ShuttleComponent>(shuttleUid, out var shuttle))
        {
            const float DampenDampingStrength = 0.25f;
            shuttle.BodyModifier = DampenDampingStrength;
            if (shuttle.DampingModifier != 0)
                shuttle.DampingModifier = shuttle.BodyModifier;

            // Refresh shuttle consoles so pilots see the mode change to "Drive"
            _console.RefreshShuttleConsoles(shuttleUid);
        }

        // Clear any held pilot inputs (e.g., brake button) that would interfere with autopilot
        var pilotQuery = EntityQueryEnumerator<PilotComponent, TransformComponent>();
        while (pilotQuery.MoveNext(out _, out var pilot, out var pilotXform))
        {
            // Check if this pilot is on our shuttle
            if (pilotXform.GridUid == shuttleUid)
            {
                pilot.HeldButtons = ShuttleButtons.None;
            }
        }
    }

    /// <summary>
    /// Disable autopilot
    /// </summary>
    public void DisableAutopilot(EntityUid shuttleUid)
    {
        var autopilot = EnsureComp<AutopilotComponent>(shuttleUid);

        if (!autopilot.Enabled)
        {
            return;
        }

        autopilot.Enabled = false;
        autopilot.TargetCoordinates = null;
        SendShuttleMessage(shuttleUid, "Autopilot: Disabled");
    }

    /// <summary>
    /// Checks if there's a powered autopilot server on the shuttle grid.
    /// </summary>
    private bool HasPoweredAutopilotServer(EntityUid shuttleGridUid)
    {
        var query = EntityQueryEnumerator<AutopilotServerComponent, TransformComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out _, out _, out var xform, out var powerReceiver))
        {
            // Check if the server is on the same grid as the shuttle
            if (xform.GridUid != shuttleGridUid || !xform.Anchored)
            {
                continue;
            }

            // Check if the server is powered
            if (powerReceiver.Powered)
            {
                return true;
            }
        }
        return false;
    }
}
