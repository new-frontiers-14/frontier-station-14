using Content.Server.Atmos.EntitySystems;

namespace Content.Server._Starlight.Atmos;

public sealed partial class ToggleableAtmosDeviceSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;

    /// <summary>
    /// Sets the device to the boolean value
    /// </summary>
    public void Set(Entity<ToggleableAtmosDeviceComponent?> entity, bool value)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        RaiseLocalEvent(entity, new SetToggleSignalReceivedEvent(value));
    }

    /// <summary>
    /// Toggles the device
    /// </summary>
    public void Toggle(Entity<ToggleableAtmosDeviceComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        RaiseLocalEvent(entity, new ToggleSignalReceivedEvent());
    }
}

public record struct SetToggleSignalReceivedEvent(bool Value);
public record struct ToggleSignalReceivedEvent();
