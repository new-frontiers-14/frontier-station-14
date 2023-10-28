using Content.Server._Park.Species.Shadowkin.Components;
using Content.Shared._Park.Species.Shadowkin.Events;
using Content.Shared._Park.Species.Shadowkin.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Server._Park.Species.Shadowkin.Systems;

public sealed class ShadowkinBlackeyeSystem : EntitySystem
{
    [Dependency] private readonly ShadowkinPowerSystem _power = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowkinBlackeyeAttemptEvent>(OnBlackeyeAttempt);
        SubscribeAllEvent<ShadowkinBlackeyeEvent>(OnBlackeye);
    }


    private void OnBlackeyeAttempt(ShadowkinBlackeyeAttemptEvent ev)
    {
        if (!_entity.TryGetComponent<ShadowkinComponent>(ev.Uid, out var component) ||
            component.Blackeye ||
            !(component.PowerLevel <= ShadowkinComponent.PowerThresholds[ShadowkinPowerThreshold.Min] + 5))
            ev.Cancel();
    }

    private void OnBlackeye(ShadowkinBlackeyeEvent ev)
    {
        // Check if the entity is a shadowkin
        if (!_entity.TryGetComponent<ShadowkinComponent>(ev.Uid, out var component))
            return;

        // Stop gaining power
        component.Blackeye = true;
        component.PowerLevelGainEnabled = false;
        _power.SetPowerLevel(ev.Uid, ShadowkinComponent.PowerThresholds[ShadowkinPowerThreshold.Min]);

        // Update client state
        Dirty(component);

        // Remove powers
        _entity.RemoveComponent<ShadowkinDarkSwapPowerComponent>(ev.Uid);
        _entity.RemoveComponent<ShadowkinDarkSwappedComponent>(ev.Uid);
        _entity.RemoveComponent<ShadowkinRestPowerComponent>(ev.Uid);
        _entity.RemoveComponent<ShadowkinTeleportPowerComponent>(ev.Uid);

        if (!ev.Damage)
            return;

        // Popup
        _popup.PopupEntity(Loc.GetString("shadowkin-blackeye"), ev.Uid, ev.Uid, PopupType.Large);

        // Stamina crit
        if (_entity.TryGetComponent<StaminaComponent>(ev.Uid, out var stamina))
        {
            _stamina.TakeStaminaDamage(ev.Uid, stamina.CritThreshold, null, ev.Uid);
        }

        // Nearly crit with cellular damage
        // If already 5 damage off of crit, don't do anything
        if (!_entity.TryGetComponent<DamageableComponent>(ev.Uid, out var damageable) ||
            !_mobThreshold.TryGetThresholdForState(ev.Uid, MobState.Critical, out var key))
            return;

        var minus = damageable.TotalDamage;

        _damageable.TryChangeDamage(
            ev.Uid,
            new DamageSpecifier(_prototype.Index<DamageTypePrototype>("Cellular"),
                Math.Max((double) (key.Value - minus - 5), 0)),
            true,
            true
            // null,
            // null,
            // false
        );
    }
}
