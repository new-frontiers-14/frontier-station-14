using Content.Shared.CCVar;
using Content.Shared._NF.CCVar;
using Robust.Shared.Configuration;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Damage;

namespace Content.Shared._NF.SoftCrit;

public partial class SoftCritSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoftCritComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SoftCritComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SoftCritComponent, DamageChangedEvent>(OnDamageChange);
    }

    /// <summary>
    /// OnStartup ensures that component stores correct base speeds
    /// </summary>
    private void OnComponentStartup(EntityUid uid, SoftCritComponent component, ComponentStartup args)
    {
        if(TryComp<MovementSpeedModifierComponent>(uid, out var moveComp))
        {
            component.BaseSprintSpeed = moveComp.BaseSprintSpeed;
            component.BaseWalkSpeed = moveComp.BaseWalkSpeed;
        }
    }

    private void OnMobStateChanged(EntityUid uid, SoftCritComponent component, MobStateChangedEvent args)
    {
        if(TryComp<MovementSpeedModifierComponent>(uid, out var moveComp))
        {
            // 
            if (args.NewMobState == MobState.Critical)
            {
                _movementSpeedModifierSystem.ChangeBaseSpeed(uid, component.CrawlWalkSpeed, component.CrawlSprintSpeed, moveComp.Acceleration);
            }

            // 
            if (args.NewMobState == MobState.Alive)
            {
                _movementSpeedModifierSystem.ChangeBaseSpeed(uid, component.BaseWalkSpeed, component.BaseSprintSpeed, moveComp.Acceleration);
            }
        }
    }

    private void OnDamageChange(EntityUid uid, SoftCritComponent component, DamageChangedEvent args)
    {
        if(args.Damageable.TotalDamage > component.DamageThreshold)
        {
            component.UnableToAct = true;
        }
        else
        {
            component.UnableToAct = false;
        }
    }
    
}