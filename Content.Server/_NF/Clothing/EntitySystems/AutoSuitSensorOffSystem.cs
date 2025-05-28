using Content.Server._NF.Medical.SuitSensors;
using Content.Server.Medical.SuitSensors;
using Content.Shared.Access.Components;
using Content.Shared.Clothing._NF.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Roles;

namespace Content.Server._NF.Medical.SuitSensors;

public sealed class AutoSuitSensorOffSystem : EntitySystem
{
    [Dependency] private readonly SuitSensorSystem _suitSensor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisableSuitSensorComponent, StartingGearEquippedEvent>(OnStartingGear);
    }

    private void OnStartingGear(EntityUid uid, DisableSuitSensorComponent component, ref StartingGearEquippedEvent args)
    {
        if (component.StartingGear)
            _suitSensor.SetAllSensors(uid, SuitSensorMode.SensorOff);
    }
}
