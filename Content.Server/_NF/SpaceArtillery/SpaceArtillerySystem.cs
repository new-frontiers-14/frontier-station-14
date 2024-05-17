using Content.Shared.SpaceArtillery;
using Content.Server.DeviceLinking.Events;
using Content.Server.Projectiles;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Server.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Interaction.Events;
using System.Numerics;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Rounding;
using Robust.Shared.Containers;
using Content.Shared.Stacks;
using Content.Server.Stack;
using Robust.Shared.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Random;
using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork;
using Robust.Shared.Timing;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Shuttles.Components;
using Content.Server.Shuttles.Components;

namespace Content.Shared.SpaceArtillery;

public sealed partial class SpaceArtillerySystem : EntitySystem
{
	[Dependency] private readonly IPrototypeManager _prototypeManager = default!;
	[Dependency] private readonly ProjectileSystem _projectile = default!;
	[Dependency] private readonly GunSystem _gun = default!;
	[Dependency] private readonly SharedCombatModeSystem _combat = default!;
	//[Dependency] private readonly RotateToFaceSystem _rotate = default!;
	[Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
	[Dependency] private readonly SharedBuckleSystem _buckle = default!;
	[Dependency] private readonly SharedContainerSystem _containerSystem = default!;
	[Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
	[Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
	[Dependency] private readonly IRobustRandom _random = default!;
	[Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
	[Dependency] private readonly IGameTiming _gameTiming = default!; //var variable = _gameTiming.CurTime; - to set with current time
	[Dependency] private readonly SharedShuttleSystem _shuttleSystem = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

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
		SubscribeLocalEvent<SpaceArtilleryComponent, AmmoShotEvent>(OnShotEvent);
		SubscribeLocalEvent<SpaceArtilleryComponent, OnEmptyGunShotEvent>(OnEmptyShotEvent);
		SubscribeLocalEvent<SpaceArtilleryComponent, PowerChangedEvent>(OnApcChanged);
		SubscribeLocalEvent<SpaceArtilleryComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
		SubscribeLocalEvent<SpaceArtilleryComponent, EntInsertedIntoContainerMessage>(OnCoolantSlotChanged);
        SubscribeLocalEvent<SpaceArtilleryComponent, EntRemovedFromContainerMessage>(OnCoolantSlotChanged);
		SubscribeLocalEvent<SpaceArtilleryComponent, ExaminedEvent>(OnExamine);
		SubscribeLocalEvent<SpaceArtilleryComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SpaceArtilleryComponent, ComponentRemove>(OnComponentRemove);

		///TODO Integrate vessel armament deactivation event
		SubscribeLocalEvent<SpaceArtilleryGridComponent, MapInitEvent>(OnMapInit);
		SubscribeLocalEvent<SpaceArtilleryGridComponent, SpaceArtilleryGridActivationEvent>(OnActivationEvent);

		//This is to ensure proper operation of armed vessel
		SubscribeLocalEvent<IFFConsoleComponent, ComponentInit>(OnIFFInit);
	}

	private void OnMapInit(EntityUid uid, SpaceArtilleryGridComponent componentGrid, MapInitEvent args)
	{
		componentGrid.LastActivationTime = _gameTiming.CurTime;
		componentGrid.CooldownEndTime = componentGrid.LastActivationTime + componentGrid.CooldownDuration;
	}


    private void OnComponentInit(EntityUid uid, SpaceArtilleryComponent component, ComponentInit args)
    {
		if(component.IsCoolantRequiredToFire == true)
        _itemSlotsSystem.AddItemSlot(uid, SpaceArtilleryComponent.CoolantSlotSlotId, component.CoolantSlot);
    }

    private void OnComponentRemove(EntityUid uid, SpaceArtilleryComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.CoolantSlot);
    }

	private void OnExamine(EntityUid uid, SpaceArtilleryComponent component, ExaminedEvent args)
	{
		if (!args.IsInDetailsRange)
            return;


		if(component.IsArmed == true)
		{
			args.PushMarkup(Loc.GetString("space-artillery-on-examine-safe"));
		}
		else
		{
			args.PushMarkup(Loc.GetString("space-artillery-on-examine-armed"));
		}

		if(component.IsCoolantRequiredToFire == true)
		{
			args.PushMarkup(Loc.GetString("space-artillery-on-examine-coolant-consumed",
            ("consumed_coolant", component.CoolantConsumed)));

			args.PushMarkup(Loc.GetString("space-artillery-on-examine-coolant-count",
            ("current_coolant", component.CoolantStored), ("max_coolant", component.MaxCoolantStored)));
		}

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
					if((component.IsPowered == true && battery.CurrentCharge >= component.PowerUseActive) || component.IsPowerRequiredToFire == false)
					{
						if((component.IsCoolantRequiredToFire == true && component.CoolantStored >= component.CoolantConsumed) || component.IsCoolantRequiredToFire == false)
						{
							TryFireArtillery(uid, component, battery);
						} else
							OnMalfunction(uid,component);
					} else
						OnMalfunction(uid,component);
				}
			}
			if (args.Port == component.SpaceArtilleryToggleSafetyPort)
			{
				///WIP TEST DEBUG-----------------------------------------------------------------------------------
				if(TryComp<TransformComponent>(uid, out var transformComponent))
				{
					var _gridUid = transformComponent.GridUid;

					if(_gridUid is {Valid :true} gridUid)
					{
					var activationEvent = new SpaceArtilleryGridActivationEvent();
					RaiseLocalEvent(gridUid, ref activationEvent);
					}
				}

				///TEST DEBUG--------------------------------------------------------------------------------

				if (TryComp<CombatModeComponent>(uid, out var combat))
				{
					if(combat.IsInCombatMode == false && combat.IsInCombatMode != null)
					{
						_combat.SetInCombatMode(uid, true, combat);
						component.IsArmed = true;

						if(component.IsCapableOfSendingSignal == true)
							_deviceLink.SendSignal(uid, component.SpaceArtilleryDetectedSafetyChangePort, true);
					}
					else
					{
						_combat.SetInCombatMode(uid, false, combat);
						component.IsArmed = false;

						if(component.IsCapableOfSendingSignal == true)
							_deviceLink.SendSignal(uid, component.SpaceArtilleryDetectedSafetyChangePort, true);
					}
				}
			}
			if (args.Port == component.SpaceArtilleryOnSafetyPort)
			{
				if (TryComp<CombatModeComponent>(uid, out var combat) && combat.IsInCombatMode != null)
				{
					if(combat.IsInCombatMode == true && component.IsCapableOfSendingSignal == true)
						_deviceLink.SendSignal(uid, component.SpaceArtilleryDetectedSafetyChangePort, true);

					_combat.SetInCombatMode(uid, false, combat);
					component.IsArmed = false;

				}
			}
			if (args.Port == component.SpaceArtilleryOffSafetyPort)
			{
				if (TryComp<CombatModeComponent>(uid, out var combat) && combat.IsInCombatMode != null)
				{
					if(combat.IsInCombatMode == false && component.IsCapableOfSendingSignal == true)
						_deviceLink.SendSignal(uid, component.SpaceArtilleryDetectedSafetyChangePort, true);

					_combat.SetInCombatMode(uid, true, combat);
					component.IsArmed = true;
				}
			}
		} else
			OnMalfunction(uid,component);
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
				if((component.IsPowered == true && battery.CurrentCharge >= component.PowerUseActive) || component.IsPowerRequiredToFire == false)
				{
					if((component.IsCoolantRequiredToFire == true && component.CoolantStored >= component.CoolantConsumed) || component.IsCoolantRequiredToFire == false)
					{
						if (args.Handled)
							return;

						TryFireArtillery(uid, component, battery);

						args.Handled = true;
					} else
						OnMalfunction(uid,component);
				} else
					OnMalfunction(uid,component);
			}
		} else
			OnMalfunction(uid,component);
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
					_battery.UseCharge(uid, component.PowerUsePassive, battery); //It is done so that BatterySelfRecharger will get start operating instead of being blocked by fully charged battery
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

	private void OnCoolantSlotChanged(EntityUid uid, SpaceArtilleryComponent component, ContainerModifiedMessage args)
    {
		GetInsertedCoolantAmount(component, out var storage);

		// validating the coolant slot was setup correctly in the yaml
        if (component.CoolantSlot.ContainerSlot is not BaseContainer coolantSlot)
        {
            return;
        }

		// validate stack prototypes
        if (!TryComp<StackComponent>(component.CoolantSlot.ContainerSlot.ContainedEntity, out var stackComponent) ||
            stackComponent.StackTypeId == null)
        {
            return;
        }

        // and then check them against the Armament's CoolantType
        if (_prototypeManager.Index<StackPrototype>(component.CoolantType) != _prototypeManager.Index<StackPrototype>(stackComponent.StackTypeId))
        {
            return;
        }

		var currentCoolant = component.CoolantStored;
		var maxCoolant = component.MaxCoolantStored;
		var totalCoolantPresent = currentCoolant + storage;
		if(totalCoolantPresent > maxCoolant)
		{
			var remainingCoolant = totalCoolantPresent - maxCoolant;
			stackComponent.Count = remainingCoolant;
			stackComponent.UiUpdateNeeded = true;
			component.CoolantStored = maxCoolant;
		}
		else
		{
			component.CoolantStored = totalCoolantPresent;
			 _containerSystem.CleanContainer(coolantSlot);
		}
	}

	private void TryFireArtillery(EntityUid uid, SpaceArtilleryComponent component, BatteryComponent battery)
	{
		var xform = Transform(uid);

		if (!_gun.TryGetGun(uid, out var gunUid, out var gun))
		{
			OnMalfunction(uid,component);
			return;
		}

		if(TryComp<TransformComponent>(uid, out var transformComponent)){

			var worldPosX = transformComponent.WorldPosition.X;
			var worldPosY = transformComponent.WorldPosition.Y;
			var worldRot = transformComponent.WorldRotation+rotOffset;
			var targetSpot = new Vector2(worldPosX - distance * (float) Math.Sin(worldRot), worldPosY + distance * (float) Math.Cos(worldRot));

			var _gridUid = transformComponent.GridUid;

			EntityCoordinates targetCordinates;
			targetCordinates = new EntityCoordinates(xform.MapUid!.Value, targetSpot);

            _gun.AttemptShoot(uid, gunUid, gun, targetCordinates);

            // Unused grid system to enable/disable multiple armaments connected to a grid at the same time

            ///Checks if armaments on grid are activated, or its a harmless armament
            //if(TryComp<SpaceArtilleryGridComponent>(_gridUid, out var componentGrid))
            //{
            //	if(componentGrid.IsActive == true || component.IsDestructive == false)
            //	{
            //		///Fires the armament, sending signal to gun system
            //		_gun.AttemptShoot(uid, gunUid, gun, targetCordinates);
            //	}
            //	else
            //	{
            //		OnMalfunction(uid,component);
            //	}
            //}
            //else if(component.IsDestructive == false)
            //{
            //	///Fires the armament, sending signal to gun system
            //	_gun.AttemptShoot(uid, gunUid, gun, targetCordinates);
            //}
        }
    }

    private void GetInsertedCoolantAmount(SpaceArtilleryComponent component, out int amount)
    {
        amount = 0;
        var coolantEntity = component.CoolantSlot.ContainerSlot?.ContainedEntity;

        if (!TryComp<StackComponent>(coolantEntity, out var coolantStack) ||
            coolantStack.StackTypeId != component.CoolantType)
        {
            return;
        }

        amount = coolantStack.Count;
        return;
    }

	///TODO Fix empty cartridge allowing recoil to be activated
	///TOD add check for args.FiredProjectiles
	private void OnShotEvent(EntityUid uid, SpaceArtilleryComponent component, AmmoShotEvent args)
	{
		if(args.FiredProjectiles.Count == 0)
		{
			OnMalfunction(uid,component);
			return;
		}

		if(TryComp<TransformComponent>(uid, out var transformComponent) && TryComp<BatteryComponent>(uid, out var battery)){
			var worldPosX = transformComponent.WorldPosition.X;
			var worldPosY = transformComponent.WorldPosition.Y;
			var worldRot = transformComponent.WorldRotation+rotOffset;
			var targetSpot = new Vector2(worldPosX - distance * (float) Math.Sin(worldRot), worldPosY + distance * (float) Math.Cos(worldRot));

			var _gridUid = transformComponent.GridUid;

		if(component.IsCapableOfSendingSignal == true)
			_deviceLink.SendSignal(uid, component.SpaceArtilleryDetectedFiringPort, true);

		if(component.IsPowerRequiredToFire == true)
		{
			_battery.UseCharge(uid, component.PowerUseActive, battery);
		}
		if(component.IsCoolantRequiredToFire == true)
		{
			component.CoolantStored -= component.CoolantConsumed;
		}

		///Space Recoil is handled here
		if(transformComponent.Anchored == true)
		{
			if(TryComp<PhysicsComponent>(_gridUid, out var gridPhysicsComponent) && _gridUid is {Valid :true} gridUid)
			{
					var gridMass = gridPhysicsComponent.FixturesMass;
					var linearVelocityLimitGrid = component.VelocityLimitRecoilGrid;
					var oldLinearVelocity = gridPhysicsComponent.LinearVelocity;
					var oldAngularVelocity = gridPhysicsComponent.AngularVelocity;

					var oldLinearRelativeVelocity = (float) Math.Sqrt((oldLinearVelocity.X * oldLinearVelocity.X) + (oldLinearVelocity.Y * oldLinearVelocity.Y));

					//Check if grid isn't flying faster already than the velocity limit
					if(oldLinearRelativeVelocity >= linearVelocityLimitGrid)
						return;

					var targetSpotRecoil = new Vector2(worldPosX - component.LinearRecoilGrid * (float) Math.Sin(worldRot), worldPosY + component.LinearRecoilGrid * (float) Math.Cos(worldRot));
					var recoilX = (worldPosX - targetSpotRecoil.X);
					var recoilY = (worldPosY - targetSpotRecoil.Y);

					var newLinearVelocity = new Vector2(oldLinearVelocity.X + (recoilX/gridMass), oldLinearVelocity.Y + (recoilY/gridMass));


					var newLinearRelativeVelocity = (float) Math.Sqrt((newLinearVelocity.X * newLinearVelocity.X) + (newLinearVelocity.Y * newLinearVelocity.Y));

					//Check if new velocity isn't faster than the limit
					if(newLinearRelativeVelocity > linearVelocityLimitGrid)
					{
						//Decrease X and Y velocity so that relative velocity matches the limit
						newLinearVelocity.X = newLinearVelocity.X * linearVelocityLimitGrid / newLinearRelativeVelocity;
						newLinearVelocity.Y = newLinearVelocity.Y * linearVelocityLimitGrid / newLinearRelativeVelocity;
					}

					var randomAngularInstability = _random.Next((int) -component.AngularInstabilityGrid, (int) component.AngularInstabilityGrid);
					var newAngularVelocity = oldAngularVelocity + (randomAngularInstability/gridMass);


					_physicsSystem.SetLinearVelocity(gridUid, newLinearVelocity);
					_physicsSystem.SetAngularVelocity(gridUid, newAngularVelocity);

					Sawmill.Info($"Space Artillery recoil. RecoilX: {recoilX}  RecoilY: {recoilY}  Instability: {randomAngularInstability}");
					Sawmill.Info($"Space Artillery recoil. LinearVelocityX: {newLinearVelocity.X}/{oldLinearVelocity.X}  LinearVelocityY: {newLinearVelocity.Y}/{oldLinearVelocity.Y}  AngularInstability: {newAngularVelocity}/{oldAngularVelocity}");

					//(float) Math.Sqrt(GetSeverityModifier());
				}
			}
			else
			{
				if(TryComp<PhysicsComponent>(uid, out var weaponPhysicsComponent) && uid is {Valid :true} weaponUid)
				{
					var weaponMass = weaponPhysicsComponent.FixturesMass;
					var linearVelocityLimitWeapon = component.VelocityLimitRecoilWeapon;
					var oldLinearVelocity = weaponPhysicsComponent.LinearVelocity;
					var oldAngularVelocity = weaponPhysicsComponent.AngularVelocity;

					var oldLinearRelativeVelocity = (float) Math.Sqrt((oldLinearVelocity.X * oldLinearVelocity.X) + (oldLinearVelocity.Y * oldLinearVelocity.Y));

					//Check if weapon isn't flying faster already than the velocity limit
					if(oldLinearRelativeVelocity >= linearVelocityLimitWeapon)
						return;

					var targetSpotRecoil = new Vector2(worldPosX - component.LinearRecoilWeapon * (float) Math.Sin(worldRot), worldPosY + component.LinearRecoilWeapon * (float) Math.Cos(worldRot));
					var recoilX = (worldPosX - targetSpotRecoil.X);
					var recoilY = (worldPosY - targetSpotRecoil.Y);

					var newLinearVelocity = new Vector2(oldLinearVelocity.X + (recoilX/weaponMass), oldLinearVelocity.Y + (recoilY/weaponMass));


					var newLinearRelativeVelocity = (float) Math.Sqrt((newLinearVelocity.X * newLinearVelocity.X) + (newLinearVelocity.Y * newLinearVelocity.Y));

					//Check if new velocity isn't faster than the limit
					if(newLinearRelativeVelocity > linearVelocityLimitWeapon)
					{
						//Decrease X and Y velocity so that relative velocity matches the limit
						newLinearVelocity.X = newLinearVelocity.X * linearVelocityLimitWeapon / newLinearRelativeVelocity;
						newLinearVelocity.Y = newLinearVelocity.Y * linearVelocityLimitWeapon / newLinearRelativeVelocity;
					}

					var randomAngularInstability = _random.Next((int) -component.AngularInstabilityWeapon, (int) component.AngularInstabilityWeapon);
					var newAngularVelocity = oldAngularVelocity + (randomAngularInstability/weaponMass);


					_physicsSystem.SetLinearVelocity(uid, newLinearVelocity);
					_physicsSystem.SetAngularVelocity(uid, newAngularVelocity);

					//(float) Math.Sqrt(GetSeverityModifier());
				}
			}
		}
	}

	private void OnEmptyShotEvent(EntityUid uid, SpaceArtilleryComponent component, OnEmptyGunShotEvent args)
	{
		OnMalfunction(uid,component);
	}

	private void OnMalfunction(EntityUid uid, SpaceArtilleryComponent component)
	{
		if(component.IsCapableOfSendingSignal == true)
			_deviceLink.SendSignal(uid, component.SpaceArtilleryDetectedMalfunctionPort, true);
	}



///Armed vessels handling
//TODO Code it much much better

	///Prevents built IFF console from being capable of changing armed vessel's IFF settings
    private void OnIFFInit(EntityUid uid, IFFConsoleComponent iffComponent, ComponentInit args)
    {
		if(TryComp<TransformComponent>(uid, out var transformComponent))
		{
			var _gridUid = transformComponent.GridUid;

			if(_gridUid is {Valid :true} gridUid)
			{
				if(TryComp<SpaceArtilleryGridComponent>(gridUid, out var SpaceArtilleryGridComponent))
				{
					if(SpaceArtilleryGridComponent.IsActive == true || SpaceArtilleryGridComponent.IsCharging == true)
					{
							var _oldFlags = iffComponent.AllowedFlags;
							var _newFlags = iffComponent.AccessableAllowedFlags;

							iffComponent.AllowedFlags = _newFlags;
							iffComponent.AccessableAllowedFlags = _oldFlags;

							iffComponent.IsDisabled = true;

							var ev = new AnchorStateChangedEvent(transformComponent);
							RaiseLocalEvent(uid, ref ev, false);
					}
				}
			}
		}
    }


	private void OnActivationEvent(EntityUid GridUid, SpaceArtilleryGridComponent componentGrid, ref SpaceArtilleryGridActivationEvent args)
	{

		if(componentGrid.IsActive == true)
		{
			if(_gameTiming.CurTime >= componentGrid.CooldownEndTime)
			{
				componentGrid.IsActive = false;

				if(TryComp<IFFComponent>(GridUid, out var IffComponent))
				{
					//IffComponent.Color = componentGrid.Color;
					_shuttleSystem.SetIFFColor(GridUid, componentGrid.Color, IffComponent);
					//_shuttleSystem.AddIFFFlag(GridUid, IFFFlags.Hide);
					//_shuttleSystem.AddIFFFlag(GridUid, IFFFlags.HideLabel);

					var query = EntityQueryEnumerator<IFFConsoleComponent>();
					while (query.MoveNext(out var uid, out var comp))
					{
						if(TryComp<TransformComponent>(uid, out var transformComponent))
						{
							var _gridUid = transformComponent.GridUid;

							if(_gridUid is {Valid :true} gridUid && gridUid == GridUid && comp.IsDisabled == true)
							{
								var _oldFlags = comp.AllowedFlags;
								var _newFlags = comp.AccessableAllowedFlags;

								comp.AllowedFlags = _newFlags;
								comp.AccessableAllowedFlags = _oldFlags;

								comp.IsDisabled = false;

								var ev = new AnchorStateChangedEvent(transformComponent);
								RaiseLocalEvent(uid, ref ev, false);
							}
						}
					}
				}
			}
		}
		else if(componentGrid.IsCharging == true)
		{
			if(_gameTiming.CurTime >= componentGrid.ChargeUpEndTime)
			{
				componentGrid.IsCharging = false;
				componentGrid.IsActive = true;
			}
		}
		else
		{
			componentGrid.IsCharging = true;

			componentGrid.LastActivationTime = _gameTiming.CurTime;
			componentGrid.ChargeUpEndTime = componentGrid.LastActivationTime + componentGrid.ChargeUpDuration;
			componentGrid.CooldownEndTime = componentGrid.LastActivationTime + componentGrid.CooldownDuration;

			if(TryComp<IFFComponent>(GridUid, out var IffComponent))
			{
				//IffComponent.Color = componentGrid.ArmedColor;
				//IffComponent.Flags = componentGrid.Flags;
				///TODO have it affect IFF consoles and disable their ability
				_shuttleSystem.SetIFFColor(GridUid, componentGrid.ArmedColor, IffComponent);
				_shuttleSystem.RemoveIFFFlag(GridUid, IFFFlags.Hide);
				_shuttleSystem.RemoveIFFFlag(GridUid, IFFFlags.HideLabel);

				var query = EntityQueryEnumerator<IFFConsoleComponent>();
				while (query.MoveNext(out var uid, out var comp))
				{
					if(TryComp<TransformComponent>(uid, out var transformComponent))
					{
						var _gridUid = transformComponent.GridUid;

						if(_gridUid is {Valid :true} gridUid && gridUid == GridUid && comp.IsDisabled == false)
						{
							var _oldFlags = comp.AllowedFlags;
							var _newFlags = comp.AccessableAllowedFlags;

							comp.AllowedFlags = _newFlags;
							comp.AccessableAllowedFlags = _oldFlags;

							comp.IsDisabled = true;

							var ev = new AnchorStateChangedEvent(transformComponent);
							RaiseLocalEvent(uid, ref ev, false);
						}
					}
				}
			}
		}
	}
}




// Raise event to activate armaments for the grid
 /*       var activationEvent = new SpaceArtilleryGridActivationEvent(true, uid, GridUid);
        RaiseLocalEvent(GridUid, activationEvent);

		if (activationEvent.Handled)
            return;
		*/
