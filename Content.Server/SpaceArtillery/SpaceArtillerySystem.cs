using Content.Shared.SpaceArtillery;
using Content.Server.DeviceLinking.Events;
using Content.Server.Projectiles;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.SpaceArtillery;

public sealed class SpaceArtillerySystem : EntitySystem
{

	[Dependency] private readonly ProjectileSystem _projectile = default!;
	[Dependency] private readonly GunSystem _gun = default!;
	
	public override void Initialize()
	{
		base.Initialize();
		
		SubscribeLocalEvent<SpaceArtilleryComponent, SignalReceivedEvent>(OnSignalReceived);
	}

	private void OnSignalReceived(EntityUid uid, SpaceArtilleryComponent component, ref SignalReceivedEvent args)
	{
		if (args.Port == component.SpaceArtilleryFirePort)
		{
			//var attemptEv = new AttemptShootEvent(uid, null);
			//RaiseLocalEvent(uid, ref attemptEv);
		}
	}
}