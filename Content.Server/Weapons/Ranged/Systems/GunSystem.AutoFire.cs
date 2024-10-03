using Content.Shared.Weapons.Ranged.Components;
using Content.Server.Power.Components; // Frontier
using Content.Server.Power.EntitySystems; // Frontier
using Content.Shared.Interaction; // Frontier
using Content.Shared.Examine; // Frontier
using Content.Server.Popups;
using Content.Shared.Power; // Frontier

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] public PopupSystem _popup = default!; // Frontier
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

    // New Frontiers - Shuttle Gun Power Draw - makes shuttle guns require power if they
    // have an ApcPowerReceiverComponent
    // This code is licensed under AGPLv3. See AGPLv3.txt
    private void OnGunExamine(EntityUid uid, AutoShootGunComponent component, ExaminedEvent args)
    {
        // Powered is already handled by other power components
        var enabled = Loc.GetString(component.On ? "gun-comp-enabled" : "gun-comp-disabled");

        args.PushMarkup(enabled);
    }

    private void OnActivateGun(EntityUid uid, AutoShootGunComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        component.On ^= true;

        if (!component.On)
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && component.OriginalLoad != 0)
                apcPower.Load = 1;

            DisableGun(uid, component);
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("auto-fire-disabled"), uid, args.User);
        }
        else if (CanEnable(uid, component))
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && component.OriginalLoad != apcPower.Load)
                apcPower.Load = component.OriginalLoad;

            EnableGun(uid, component);
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("auto-fire-enabled"), uid, args.User);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("auto-fire-enabled-no-power"), uid, args.User);
        }
    }

    /// <summary>
    /// Tries to disable the AutoShootGun.
    /// </summary>
    public void DisableGun(EntityUid uid, AutoShootGunComponent component)
    {
        if (component.CanFire)
            component.CanFire = false;
    }

    public bool CanEnable(EntityUid uid, AutoShootGunComponent component)
    {
        var xform = Transform(uid);

        // Must be anchored to fire.
        if (!xform.Anchored)
            return false;

        // No power needed? Always works.
        if (!HasComp<ApcPowerReceiverComponent>(uid))
            return true;

        // Not switched on? Won't work.
        if (!component.On)
            return false;

        return this.IsPowered(uid, EntityManager);
    }

    public void EnableGun(EntityUid uid, AutoShootGunComponent component, TransformComponent? xform = null)
    {
        if (!component.CanFire)
            component.CanFire = true;
    }

    private void OnAnchorChange(EntityUid uid, AutoShootGunComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored && CanEnable(uid, component))
            EnableGun(uid, component);
        else
            DisableGun(uid, component);
    }

    private void OnGunInit(EntityUid uid, AutoShootGunComponent component, ComponentInit args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && component.OriginalLoad == 0)
            component.OriginalLoad = apcPower.Load;

        if (!component.On)
            return;

        if (CanEnable(uid, component))
            EnableGun(uid, component);
    }

    private void OnGunShutdown(EntityUid uid, AutoShootGunComponent component, ComponentShutdown args)
    {
        DisableGun(uid, component);
    }

    private void OnPowerChange(EntityUid uid, AutoShootGunComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered && CanEnable(uid, component))
            EnableGun(uid, component);
        else
            DisableGun(uid, component);
    }
    // End of modified code
}
