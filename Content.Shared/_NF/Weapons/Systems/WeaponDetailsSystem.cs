using Content.Shared._NF.Weapons.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._NF.Weapons.Systems;

public sealed class WeaponDetailsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NFWeaponDetailsComponent, GunExamineEvent>(OnGunExamined);
    }

    private void OnGunExamined(Entity<NFWeaponDetailsComponent> ent, ref GunExamineEvent args)
    {
        args.Msg.PushNewline();

        if (ent.Comp.Manufacturer != null)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupPermissive(Loc.GetString("gun-examine-nf-manufacturer",
                ("color", SharedGunSystem.FireRateExamineColor),
                ("manufacturercolor", ent.Comp.ManufacturerColor),
                ("value", Loc.GetString(ent.Comp.Manufacturer))));
        }

        if (ent.Comp.Class != null)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupPermissive(Loc.GetString("gun-examine-nf-class",
                ("color", SharedGunSystem.FireRateExamineColor),
                ("value", Loc.GetString(ent.Comp.Class))));
        }
    }
}
