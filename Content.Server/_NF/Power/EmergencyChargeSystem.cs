using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Examine;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Power;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Color = Robust.Shared.Maths.Color;
using Content.Shared._NF.Power; // Frontier
using Content.Shared._NF.Power.Components; // Frontier
using Content.Server._NF.Power.Components; // Frontier

namespace Content.Server._NF.Power.EntitySystems;

public sealed class EmergencyChargeSystem : SharedEmergencyChargeSystem
{
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmergencyChargeComponent, EmergencyChargeEvent>(OnEmergencyChargeEvent);
        SubscribeLocalEvent<EmergencyChargeComponent, ExaminedEvent>(OnEmergencyExamine);
        SubscribeLocalEvent<EmergencyChargeComponent, PowerChangedEvent>(OnEmergencyPower);
    }

    private void OnEmergencyPower(Entity<EmergencyChargeComponent> entity, ref PowerChangedEvent args)
    {
        var meta = MetaData(entity.Owner);

        // TODO: PowerChangedEvent shouldn't be issued for paused ents but this is the world we live in.
        if (meta.EntityLifeStage >= EntityLifeStage.Terminating ||
            meta.EntityPaused)
        {
            return;
        }

        UpdateState(entity);
    }

    private void OnEmergencyExamine(EntityUid uid, EmergencyChargeComponent component, ExaminedEvent args)
    {
        using (args.PushGroup(nameof(EmergencyChargeComponent)))
        {
            args.PushMarkup(
                Loc.GetString("emergency-light-component-on-examine",
                    ("batteryStateText",
                        Loc.GetString(component.BatteryStateText[component.State]))));
        }
    }

    private void OnEmergencyChargeEvent(EntityUid uid, EmergencyChargeComponent component, EmergencyChargeEvent args)
    {
        switch (args.State)
        {
            case EmergencyChargeState.On:
            case EmergencyChargeState.Charging:
                EnsureComp<ActiveEmergencyChargeComponent>(uid);
                break;
            case EmergencyChargeState.Full:
            case EmergencyChargeState.Empty:
                RemComp<ActiveEmergencyChargeComponent>(uid);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetState(EntityUid uid, EmergencyChargeComponent component, EmergencyChargeState state)
    {
        if (component.State == state) return;

        component.State = state;
        RaiseLocalEvent(uid, new EmergencyChargeEvent(state));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveEmergencyChargeComponent, EmergencyChargeComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out _, out var emergencyLight, out var battery))
        {
            Update((uid, emergencyLight), battery, frameTime);
        }
    }

    private void Update(Entity<EmergencyChargeComponent> entity, BatteryComponent battery, float frameTime)
    {
        if (entity.Comp.State == EmergencyChargeState.On)
        {
            if (!_battery.TryUseCharge(entity.Owner, entity.Comp.Wattage * frameTime, battery))
            {
                SetState(entity.Owner, entity.Comp, EmergencyChargeState.Empty);
                TurnOff(entity);
            }
        }
        else
        {
            _battery.SetCharge(entity.Owner, battery.CurrentCharge + entity.Comp.ChargingWattage * frameTime * entity.Comp.ChargingEfficiency, battery);
            if (battery.IsFullyCharged)
            {
                if (TryComp<ApcPowerReceiverComponent>(entity.Owner, out var receiver))
                {
                    receiver.Load = 1;
                }

                SetState(entity.Owner, entity.Comp, EmergencyChargeState.Full);
            }
        }
    }

    /// <summary>
    ///     Updates the light's power drain, battery drain, sprite and actual light state.
    /// </summary>
    public void UpdateState(Entity<EmergencyChargeComponent> entity)
    {
        if (!TryComp<ApcPowerReceiverComponent>(entity.Owner, out var receiver))
            return;

        if (receiver.Powered && !entity.Comp.ForciblyEnabled) // Green alert
        {
            receiver.Load = (int) Math.Abs(entity.Comp.Wattage);
            TurnOff(entity, receiver);
            SetState(entity.Owner, entity.Comp, EmergencyChargeState.Charging);
        }
        else if (!receiver.Powered) // If internal battery runs out it will end in off red state
        {
            // Set off
            SetState(entity.Owner, entity.Comp, EmergencyChargeState.On);
        }
        else // Powered and enabled
        {
            TurnOn(entity, receiver);
            SetState(entity.Owner, entity.Comp, EmergencyChargeState.On);
        }
    }

    private void TurnOff(Entity<EmergencyChargeComponent> entity)
    {
        _appearance.SetData(entity.Owner, EmergencyChargeVisuals.On, false);
        _ambient.SetAmbience(entity.Owner, false);

    }

    private void TurnOff(Entity<EmergencyChargeComponent> entity, ApcPowerReceiverComponent receiver)
    {
        _appearance.SetData(entity.Owner, EmergencyChargeVisuals.On, false);
        _ambient.SetAmbience(entity.Owner, false);

        receiver.Powered = false;
    }

    private void TurnOn(Entity<EmergencyChargeComponent> entity, ApcPowerReceiverComponent receiver)
    {
        _appearance.SetData(entity.Owner, EmergencyChargeVisuals.On, true);
        _ambient.SetAmbience(entity.Owner, true);

        receiver.Powered = true;
    }
}
