using Content.Shared._DV.Abilities;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._DV.Abilities.Chitinid;

public sealed partial class ChitinidSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ItemCougherSystem _cougher = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChitinidComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChitinidComponent, ItemCoughedUpEvent>(OnItemCoughedUp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ChitinidComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var comp, out var damageable))
        {
            if (_timing.CurTime < comp.NextUpdate)
                continue;

            comp.NextUpdate += comp.UpdateInterval;

            if (comp.AmountAbsorbed >= comp.MaximumAbsorbed || _mobState.IsDead(uid))
                continue;

            if (_damageable.TryChangeDamage(uid, comp.Healing, damageable: damageable) is not {} delta)
                continue;

            // damage healed is subtracted, so the delta is negative.
            comp.AmountAbsorbed -= delta.GetTotal();
            if (comp.AmountAbsorbed >= comp.MaximumAbsorbed)
                _cougher.EnableAction(uid);
        }
    }

    private void OnMapInit(Entity<ChitinidComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnItemCoughedUp(Entity<ChitinidComponent> ent, ref ItemCoughedUpEvent args)
    {
        // start healing radiation again
        ent.Comp.AmountAbsorbed = 0f;
    }
}
