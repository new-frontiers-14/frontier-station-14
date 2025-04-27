using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Robust.Shared.Prototypes;
using Content.Server.Power.Components;
using Content.Shared.PowerCell.Components;

namespace Content.Server._NF.Power.Components;

public sealed class MixedPowerRecieverSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PowerCellSystem _powerCellSystem = default!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;
    [Dependency] private readonly PowerReceiverSystem _powerSystem = default!;

    BatteryComponent batComp = new();


    public bool IsPowered(EntityUid uid, MixedPowerReceiverComponent? comp)
    {
        // check whoever put this component on the thing didn't do an oopsie
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerComp) ||
            !TryComp<PowerCellSlotComponent>(uid, out var cellSlotComp) ||
            !Resolve<MixedPowerReceiverComponent>(uid, ref comp))
        {
            return false;
        }
        if (_powerSystem.IsPowered(uid, apcPowerComp) || _powerCellSystem.HasCharge(uid, comp.Wattage))
        {
            return true;
        }
        return false;
    }

    public void UsePower(EntityUid uid, MixedPowerReceiverComponent? comp)
    {
        // check whoever put this component on the thing didn't do an oopsie
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerComp) ||
            !TryComp<PowerCellSlotComponent>(uid, out var cellSlotComp) ||
            !Resolve<MixedPowerReceiverComponent>(uid, ref comp))
        {
            return;
        }
        if (_powerSystem.IsPowered(uid, apcPowerComp)) // don't draw power from the cell if we're on APC power
            return;
        _powerCellSystem.TryUseCharge(uid, comp.Wattage, cellSlotComp);
    }

    public bool TryUseCharge(EntityUid uid, float value, BatteryComponent? battery = null)
    {
        if (IsAPCPowered(uid))
        {
            return true;
        }

        return _batterySystem.TryUseCharge(uid, value, battery);
    }

    public bool TryUseCharge(EntityUid uid, float charge, PowerCellSlotComponent? component = null, EntityUid? user = null)
    {
        if (IsAPCPowered(uid))
        {
            return true;
        }
        return _powerCellSystem.TryUseCharge(uid, charge, component, user);
    }
    public bool IsAPCPowered(EntityUid uid)
    {
        return HasComp<MixedPowerReceiverComponent>(uid) &&
                TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerComp) &&
                _powerSystem.IsPowered(uid, apcPowerComp);
    }

}
