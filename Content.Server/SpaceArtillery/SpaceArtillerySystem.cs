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
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Rounding;

namespace Content.Shared.SpaceArtillery;

public sealed partial class SpaceArtillerySystem : EntitySystem
{

	[Dependency] private readonly ProjectileSystem _projectile = default!;
	[Dependency] private readonly GunSystem _gun = default!;
	[Dependency] private readonly SharedCombatModeSystem _combat = default!;
	//[Dependency] private readonly RotateToFaceSystem _rotate = default!;
	[Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
	[Dependency] private readonly SharedBuckleSystem _buckle = default!;
	
	private const float ShootSpeed = 30f;
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
		SubscribeLocalEvent<SpaceArtilleryComponent, PowerChangedEvent>(OnApcChanged);
		SubscribeLocalEvent<SpaceArtilleryComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
	}

	private void OnSignalReceived(EntityUid uid, SpaceArtilleryComponent component, ref SignalReceivedEvent args)
	{
		if(component.IsPowered == true || component.IsPowerRequiredForSignal == false)
		{
			//Sawmill.Info($"Space Artillery has been pinged. Entity: {ToPrettyString(uid)}");
			if (args.Port == component.SpaceArtilleryFirePort && component.IsArmed == true)
			{
				if(TryComp<BatteryComponent>(uid, out var battery))
				{
					if((component.IsPowered == true && battery.Charge >= component.PowerUseActive) || component.IsPowerRequiredToFire == false)
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
							if(component.IsPowerRequiredToFire == true)
							battery.CurrentCharge -= component.PowerUseActive;
						}
					}
				}
			}
			if (args.Port == component.SpaceArtillerySafetyPort)
			{
				if (TryComp<CombatModeComponent>(uid, out var combat))
				{
					if(combat.IsInCombatMode == false && combat.IsInCombatMode != null)
					{
						_combat.SetInCombatMode(uid, true, combat);
						component.IsArmed = true;
					}
					else
					{
						_combat.SetInCombatMode(uid, false, combat);
						component.IsArmed = false;
					}
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
                _actionsSystem.AddAction(args.BuckledEntity, ref component.FireActionEntity, component.FireAction, uid, actions);
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
		if((component.IsPowered == true || component.IsPowerRequiredForMount == false) && component.IsArmed == true)
		{
			if(TryComp<BatteryComponent>(uid, out var battery))
			{
				if((component.IsPowered == true && battery.Charge >= component.PowerUseActive) || component.IsPowerRequiredToFire == false)
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
						if(component.IsPowerRequiredToFire == true)
							battery.CurrentCharge -= component.PowerUseActive;
					}

					
					args.Handled = true;
				}
			}
		}
    }


	private void OnApcChanged(EntityUid uid, SpaceArtilleryComponent component, ref PowerChangedEvent args){
		
		if(TryComp<BatterySelfRechargerComponent>(uid, out var batteryCharger)){
		
			if (args.Powered)
			{
				component.IsCharging = true;
				batteryCharger.AutoRecharge = true;
				batteryCharger.AutoRechargeRate = component.PowerChargeRate;
			}
			else
			{
				component.IsCharging = false;
				batteryCharger.AutoRecharge = true;
				batteryCharger.AutoRechargeRate = component.PowerUsePassive * -1;
				
				if(TryComp<BatteryComponent>(uid, out var battery))
					battery.CurrentCharge -= 1; //It is done so that BatterySelfRecharger will get start operating instead of being blocked by fully charged battery
			}
		}
	}
	
	
	private void OnBatteryChargeChanged(EntityUid uid, SpaceArtilleryComponent component, ref ChargeChangedEvent args){
		
		if(args.Charge > 0)
		{
			component.IsPowered = true;
		}
		else
		{
			component.IsPowered = false;
		}
		
		if(TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerReceiver) && TryComp<BatteryComponent>(uid, out var battery))
		{
			if(battery.IsFullyCharged == false)
			{
				apcPowerReceiver.Load = component.PowerUsePassive + component.PowerChargeRate;
			}
			else
			{
				apcPowerReceiver.Load = component.PowerUsePassive;
			}
		}
	}
}