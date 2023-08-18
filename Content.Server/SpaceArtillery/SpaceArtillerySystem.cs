using Content.Shared.SpaceArtillery;
using Content.Server.DeviceLinking.Events;
using Content.Server.Projectiles;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Interaction.Events;
using System.Numerics;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Shared.SpaceArtillery;

public sealed class SpaceArtillerySystem : EntitySystem
{

	[Dependency] private readonly ProjectileSystem _projectile = default!;
	[Dependency] private readonly GunSystem _gun = default!;
	[Dependency] private readonly SharedCombatModeSystem _combat = default!;
	[Dependency] private readonly RotateToFaceSystem _rotate = default!;
	
	private const float ShootSpeed = 25f;
	
	protected ISawmill Sawmill = default!;
	
	public override void Initialize()
	{
		base.Initialize();
		Sawmill = Logger.GetSawmill("SpaceArtillery");
		SubscribeLocalEvent<SpaceArtilleryComponent, SignalReceivedEvent>(OnSignalReceived);
	}

	private void OnSignalReceived(EntityUid uid, SpaceArtilleryComponent component, ref SignalReceivedEvent args)
	{
		Sawmill.Info($"Space Artillery has been pinged. Entity: {ToPrettyString(uid)}");
		if (args.Port == component.SpaceArtilleryFirePort)
		{
			Sawmill.Info($"Space Artillery has received FIRE signal. Entity: {ToPrettyString(uid)}");
			//var attemptEv = new AttemptShootEvent(uid, "text", false);
			//RaiseLocalEvent(uid, ref attemptEv);
			//var attemptEv = new UseInHandEvent(uid);
			//RaiseLocalEvent(uid, attemptEv);
			var bodyQuery = GetEntityQuery<PhysicsComponent>();
			var xformQuery = GetEntityQuery<TransformComponent>();
			var combatQuery = GetEntityQuery<CombatModeComponent>();
			var query = EntityQueryEnumerator<SpaceArtilleryComponent, TransformComponent>();
			while (query.MoveNext(out uid, out var comp, out var xform))
			{
				if (!_gun.TryGetGun(uid, out var gunUid, out var gun))
				{
					continue;
				}
				
			//var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery);
            //var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetXform, xformQuery);
			//var distance = (targetPos - worldPos).Length();
			//var mapVelocity = targetBody.LinearVelocity;
				
			//var targetSpot = targetPos + mapVelocity * distance / ShootSpeed;
				
			EntityCoordinates targetCordinates;
			targetCordinates = new EntityCoordinates(xform.MapUid!.Value, new Vector2(250, 75));
				
			_gun.AttemptShoot(uid, gunUid, gun, targetCordinates);
			}
            
		}
	}
}