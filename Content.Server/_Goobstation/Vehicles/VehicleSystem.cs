using Content.Shared._Goobstation.Vehicles; // Frontier: migrate under _Goobstation
using Content.Server._NF.Radar; //Lua mod
using Content.Shared.Buckle.Components; //Lua mod

namespace Content.Server._Goobstation.Vehicles; // Frontier: migrate under _Goobstation

public sealed class VehicleSystem : SharedVehicleSystem
{
    //Lua start
    protected override void OnStrapped(Entity<VehicleComponent> uid, ref StrappedEvent args)
    {
        base.OnStrapped(uid, ref args);

        var blip = EnsureComp<RadarBlipComponent>(uid);
        blip.RadarColor = Color.Cyan;
        blip.Scale = 1f;
        blip.VisibleFromOtherGrids = true;
    }

    protected override void OnUnstrapped(Entity<VehicleComponent> uid, ref UnstrappedEvent args)
    {
        RemComp<RadarBlipComponent>(uid);

        base.OnUnstrapped(uid, ref args);

    }
}
