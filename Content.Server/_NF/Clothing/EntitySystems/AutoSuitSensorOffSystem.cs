using Content.Server.Medical.SuitSensors;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Roles;

namespace Content.Server._NF.Medical.SuitSensors;

public sealed class AutoSuitSensorOffSystem : EntitySystem
{
    [Dependency] private readonly SuitSensorSystem _suitSensor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisableSuitSensorsComponent, StartingGearEquippedEvent>(OnStartingGear);
    }

    private void OnStartingGear(EntityUid uid, DisableSuitSensorsComponent component, ref StartingGearEquippedEvent args)
    {
        if (component.StartingGear)
            _suitSensor.SetAllSensors(uid, SuitSensorMode.SensorOff);
    }
}
