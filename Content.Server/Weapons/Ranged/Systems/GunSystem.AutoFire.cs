using Content.Shared.Weapons.Ranged.Components;
using Content.Server.Power.Components; // Frontier
using Content.Server.Power.EntitySystems; // Frontier
using Content.Shared.Interaction; // Frontier

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        /*
         * On server because client doesn't want to predict other's guns.
         */

        // Automatic firing without stopping if the AutoShootGunComponent component is exist and enabled
        var query = EntityQueryEnumerator<AutoShootGunComponent, GunComponent>();

        while (query.MoveNext(out var uid, out var autoShoot, out var gun))
        {
            if (!autoShoot.Enabled)
                continue;

            if (gun.NextFire > Timing.CurTime)
                continue;

            AttemptShoot(uid, gun);
        }
    }

    private void OnGunExamine(EntityUid uid, AutoShootGunComponent component, ExaminedEvent args)
    {
        // Powered is already handled by other power components
        var enabled = Loc.GetString(component.On ? "thruster-comp-enabled" : "thruster-comp-disabled");

        using (args.PushGroup(nameof(ThrusterComponent)))
        {
            args.PushMarkup(enabled);

            if (component.Type == ThrusterType.Linear &&
                EntityManager.TryGetComponent(uid, out TransformComponent? xform) &&
                xform.Anchored)
            {
                var nozzleDir = Loc.GetString("thruster-comp-nozzle-direction",
                    ("direction", xform.LocalRotation.Opposite().ToWorldVec().GetDir().ToString().ToLowerInvariant()));

                args.PushMarkup(nozzleDir);

                var exposed = NozzleExposed(xform);

                var nozzleText =
                    Loc.GetString(exposed ? "thruster-comp-nozzle-exposed" : "thruster-comp-nozzle-not-exposed");

                args.PushMarkup(nozzleText);
            }
        }
    }

    private void OnActivateGun(EntityUid uid, AutoShootGunComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        component.On ^= true;

        if (!component.On)
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && component.OriginalLoad != 0) // Frontier
                apcPower.Load = 1; // Frontier

            DisableGun(uid, component);
            args.Handled = true;
        }
        else if (CanEnable(uid, component))
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && component.OriginalLoad != apcPower.Load) // Frontier
                apcPower.Load = component.OriginalLoad; // Frontier

            EnableGun(uid, component);
            args.Handled = true;
        }
    }

    public void DisableGun(EntityUid uid, AutoShootGunComponent component, TransformComponent? xform = null, Angle? angle = null)
    {
        if (!Resolve(uid, ref xform)) return;
        DisableGun(uid, component, xform.GridUid, xform);
    }

    /// <summary>
    /// Tries to disable the AutoShootGun.
    /// </summary>
    public void DisableGun(EntityUid uid, AutoShootGunComponent component, EntityUid? gridId, TransformComponent? xform = null, Angle? angle = null)
    {
        if (!component.IsOn ||
            !Resolve(uid, ref xform))
        {
            return;
        }

        component.IsOn = false;
    }

    public bool CanEnable(EntityUid uid, AutoShootGunComponent component)
    {
        if (!component.On)
            return false;

        if (component.LifeStage > ComponentLifeStage.Running)
            return false;

        var xform = Transform(uid);

        if (!xform.Anchored || !this.IsPowered(uid, EntityManager))
        {
            return false;
        }

        return true;
    }

    public void EnableGun(EntityUid uid, AutoShootGunComponent component, TransformComponent? xform = null)
    {
        if (component.IsOn ||
            !Resolve(uid, ref xform))
        {
            return;
        }

        component.IsOn = true;
    }

    private void OnGunInit(EntityUid uid, AutoShootGunComponent component, ComponentInit args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && component.OriginalLoad == 0) { component.OriginalLoad = apcPower.Load; } // Frontier

        if (!component.On)
        {
            return;
        }

        if (CanEnable(uid, component))
        {
            EnableGun(uid, component);
        }
    }
}
