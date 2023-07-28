using Content.Server.Storage.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Server.PneumaticCannon;
using Content.Shared.PneumaticCannon;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Server.SpaceArtillery
{
	public sealed class SpaceArtillerySystem : EntitySystem
	{
		
		public override void Initialize()
		{
			base.Initialize();
			
			SubscribeLocalEvent<SpaceArtilleryComponent, InteractHandEvent>(OnInteractHand);
            //SubscribeLocalEvent<SpaceArtilleryComponent, GetVerbsEvent<Verb>>(OnGetVerb);
		}
		
		//Idea is that when interacted, the artillery will call attempt at firing as if it was held. Will probably need to think of better way to activate it, like with linkables
		private void OnInteractHand(EntityUid uid, SpaceArtilleryComponent component, InteractHandEvent args)
		{
			if (args.Handled)
                return;
			
        var attemptEv = new AttemptShootEvent(user, null);
        RaiseLocalEvent(gunUid, ref attemptEv);
		}
			
		
//        private void Fire(EntityUid uid, EmitterComponent component)
//       {
//            if (!TryComp<GunComponent>(uid, out var gunComponent))
//                return;
//
//            var xform = Transform(uid);
//            var ent = Spawn(component.BoltType, xform.Coordinates);
//            var proj = EnsureComp<ProjectileComponent>(ent);
//            _projectile.SetShooter(proj, uid);
//
//            var targetPos = new EntityCoordinates(uid, new Vector2(0, -1));
//
//            _gun.Shoot(uid, gunComponent, ent, xform.Coordinates, targetPos, out _);
//        }
	}
}
