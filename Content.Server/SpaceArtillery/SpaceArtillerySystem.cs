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
using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;

namespace Content.Shared.SpaceArtillery;

public sealed class SpaceArtillerySystem : EntitySystem
{

	[Dependency] private readonly ProjectileSystem _projectile = default!;
	[Dependency] private readonly GunSystem _gun = default!;
	[Dependency] private readonly SharedCombatModeSystem _combat = default!;
	//[Dependency] private readonly RotateToFaceSystem _rotate = default!;
	[Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
	[Dependency] private readonly SharedBuckleSystem _buckle = default!;
	
	private const float ShootSpeed = 25f;
	private const float distance = 100;
	private const float rotOffset = 3.14159265f; //Without it the artillery would be shooting backwards.The rotation is in radians and equals to 180 degrees
	//private readonly TransformSystem _transform;
	
	protected ISawmill Sawmill = default!;
	
	public override void Initialize()
	{
		base.Initialize();
		Sawmill = Logger.GetSawmill("SpaceArtillery");
		SubscribeLocalEvent<SpaceArtilleryComponent, SignalReceivedEvent>(OnSignalReceived);
		SubscribeLocalEvent<SpaceArtilleryComponent, BuckleChangeEvent>(OnBuckleChange);
		SubscribeLocalEvent<SpaceArtilleryComponent, FireActionEvent>(OnFireAction);
	}

	private void OnSignalReceived(EntityUid uid, SpaceArtilleryComponent component, ref SignalReceivedEvent args)
	{
		//Sawmill.Info($"Space Artillery has been pinged. Entity: {ToPrettyString(uid)}");
		if (args.Port == component.SpaceArtilleryFirePort)
		{
			var xform = Transform(uid);
			
			if (!_gun.TryGetGun(uid, out var gunUid, out var gun))
			{
				return;
			}
			
			if(TryComp<TransformComponent>(uid, out var transformComponent)){
				
				var worldPosX = transformComponent.WorldPosition.X;
				var worldPosY = transformComponent.WorldPosition.Y;
				var worldRot = transformComponent.WorldRotation+rotOffset;
				var targetSpot = new Vector2(worldPosX - distance * (float) Math.Sin(worldRot), worldPosY + distance * (float) Math.Cos(worldRot));
				
				EntityCoordinates targetCordinates;
				targetCordinates = new EntityCoordinates(xform.MapUid!.Value, targetSpot);
				
				_gun.AttemptShoot(uid, gunUid, gun, targetCordinates);
			}
            
		}
		if (args.Port == component.SpaceArtillerySafetyPort)
		{
			if (TryComp<CombatModeComponent>(uid, out var combat))
			{
				if(combat.IsInCombatMode == false && combat.IsInCombatMode != null)
				{
					_combat.SetInCombatMode(uid, true, combat);
				}
				else
				{
					_combat.SetInCombatMode(uid, false, combat);
				}
			}
		}
	}
	
	private void OnBuckleChange(EntityUid uid, SpaceArtilleryComponent component, ref BuckleChangeEvent args)
    {
        // Once Gunner buckles
        if (args.Buckling)
        {

            // Update actions

            if (TryComp<ActionsComponent>(args.BuckledEntity, out var actions))
            {
                _actionsSystem.AddAction(args.BuckledEntity, component.FireAction, uid, actions);
            }
            return;
        }

        // Once gunner unbuckles
        // Clean up actions 
        _actionsSystem.RemoveProvidedActions(args.BuckledEntity, uid);

    }
	
    /// <summary>
    /// This fires when the gunner presses the fire action
    /// </summary>
	
    private void OnFireAction(EntityUid uid, SpaceArtilleryComponent component, FireActionEvent args)
    {
        if (args.Handled)
            return;

		var xform = Transform(uid);
		
		if (!_gun.TryGetGun(uid, out var gunUid, out var gun))
		{
			return;
		}
		
		if(TryComp<TransformComponent>(uid, out var transformComponent)){
			
			var worldPosX = transformComponent.WorldPosition.X;
			var worldPosY = transformComponent.WorldPosition.Y;
			var worldRot = transformComponent.WorldRotation+rotOffset;
			var targetSpot = new Vector2(worldPosX - distance * (float) Math.Sin(worldRot), worldPosY + distance * (float) Math.Cos(worldRot));
			
			EntityCoordinates targetCordinates;
			targetCordinates = new EntityCoordinates(xform.MapUid!.Value, targetSpot);
			
			_gun.AttemptShoot(uid, gunUid, gun, targetCordinates);
		}

        
        args.Handled = true;
    }
}